using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace codecrush.gstreamer.source_generator;

/// <summary>
/// Turns gstreamer-sharp's generated GType registration list into the
/// <c>ObjectManager</c> the wrapper classes call.
/// </summary>
/// <remarks>
/// gstreamer-sharp generates <c>generated/GtkSharp/ObjectManager.cs</c>: one
/// unguarded <c>GLib.GType.Register (X.GType, typeof (X))</c> line per object
/// type, and every wrapper class's static constructor calls
/// <c>ObjectManager.Initialize ()</c>. A runtime that lacks the native module of
/// one of those types therefore breaks every wrapper class at once: a gst-full
/// build without WebRTC and RTSP server could not construct
/// <c>Gst.App.AppSink</c>, because the list died on <c>gstwebrtc</c> first.
/// <para>
/// The upstream file is the source of truth, not a copy of it. It is included as
/// an <c>AdditionalFile</c> and the registrations are read out of it, so a
/// submodule bump that adds an object type needs no edit here. The generated
/// class has upstream's shape, except that each entry goes through
/// <c>GTypeRegistry</c>, which attempts it on its own and ignores a module that
/// is not installed. Only the types of a genuinely missing module stay
/// unregistered.
/// </para>
/// An entry whose shape the parser does not recognise is reported as an error
/// instead of being dropped, so a change in upstream's generated output fails the
/// build rather than silently producing a partial registry.
/// </remarks>
[Generator]
public sealed class ObjectManagerGenerator : IIncrementalGenerator
{
    /// <summary>
    /// File name of the list to rewrite. Only the name is matched - the entry is
    /// the only <c>ObjectManager.cs</c> a project adds as an
    /// <c>AdditionalFile</c>.
    /// </summary>
    private const string SourceFileName = "ObjectManager.cs";

    /// <summary>
    /// Spelled out instead of taken from <see cref="Environment.NewLine"/> so the
    /// generated text is byte-identical whichever host builds it.
    /// </summary>
    private const string NewLine = "\n";

    /// <summary>
    /// Registrations sit two levels deep - class, method - and the template
    /// places <c>{{Registrations}}</c> at its base indentation, so the helper
    /// owns the layout of the lines it joins.
    /// </summary>
    private const string RegistrationIndent = "        ";

    /// <summary>
    /// What the generated file is called. The <c>.g</c> keeps it apart from
    /// upstream's, which no longer takes part in compilation.
    /// </summary>
    private const string HintName = "ObjectManager.g.cs";

    private static readonly Regex TypeNamePattern = new(
        @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$",
        RegexOptions.CultureInvariant);

    private static readonly DiagnosticDescriptor UnrecognisedRegistration = new(
        id: "GSTOM001",
        title: "Unrecognised GType registration",
        messageFormat: "Unrecognised GType registration in '{0}': {1}",
        category: "codecrush.gstreamer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NoRegistrations = new(
        id: "GSTOM002",
        title: "No GType registration found",
        messageFormat: "'{0}' contains no 'GLib.GType.Register (X.GType, typeof (X))' call",
        category: "codecrush.gstreamer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MultipleSources = new(
        id: "GSTOM003",
        title: "More than one GType registration list",
        messageFormat: "More than one ObjectManager.cs was added as an AdditionalFile ({0}); exactly one is expected",
        category: "codecrush.gstreamer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sources = context.AdditionalTextsProvider
            .Select(static (text, _) => (Text: text, IsSource: IsSource(text.Path)))
            .Where(static candidate => candidate.IsSource)
            .Select(static (candidate, ct) => Parse(candidate.Text, ct))
            .Collect();

        context.RegisterSourceOutput(sources, static (spc, files) =>
        {
            if (files.Length == 0)
                return;

            if (files.Length > 1)
            {
                var paths = string.Join(", ", files.Select(static file => file.FilePath));
                spc.ReportDiagnostic(Diagnostic.Create(MultipleSources, Location.None, paths));
                return;
            }

            var file = files[0];

            // Reported, but the entries that did parse are still emitted: the
            // build fails on the diagnostic, and no wrapper class is left
            // without an ObjectManager to call in the meantime.
            foreach (var diagnostic in file.Diagnostics)
                spc.ReportDiagnostic(diagnostic);

            spc.AddSource(HintName, Generate(file.Types));
        });
    }

    /// <summary>
    /// Whether an <c>AdditionalFile</c> is the registration list. Only the file
    /// name is looked at: it is the one <c>ObjectManager.cs</c> a project adds.
    /// </summary>
    private static bool IsSource(string path) =>
        string.Equals(Path.GetFileName(path), SourceFileName, StringComparison.OrdinalIgnoreCase);

    private static ObjectManagerFile Parse(AdditionalText text, CancellationToken ct)
    {
        var content = text.GetText(ct);
        if (content is null)
            return new ObjectManagerFile(text.Path, ImmutableArray<string>.Empty, ImmutableArray<Diagnostic>.Empty);

        var root = CSharpSyntaxTree
            .ParseText(content, path: text.Path, cancellationToken: ct)
            .GetRoot(ct);

        var types = ImmutableArray.CreateBuilder<string>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var found = false;

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!IsGTypeRegister(invocation))
                continue;

            found = true;

            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count != 2
                || arguments[0].Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GType" } native
                || arguments[1].Expression is not TypeOfExpressionSyntax typeOf)
            {
                diagnostics.Add(Diagnostic.Create(
                    UnrecognisedRegistration,
                    At(text.Path, content, invocation),
                    text.Path,
                    "expected 'GLib.GType.Register (Some.Type.GType, typeof (Some.Type))'"));
                continue;
            }

            var nativeType = Normalise(native.Expression.ToString());
            var managedType = Normalise(typeOf.Type.ToString());

            if (!string.Equals(nativeType, managedType, StringComparison.Ordinal))
            {
                diagnostics.Add(Diagnostic.Create(
                    UnrecognisedRegistration,
                    At(text.Path, content, invocation),
                    text.Path,
                    $"'{nativeType}.GType' is registered for '{managedType}'"));
                continue;
            }

            if (!TypeNamePattern.IsMatch(nativeType))
            {
                diagnostics.Add(Diagnostic.Create(
                    UnrecognisedRegistration,
                    At(text.Path, content, invocation),
                    text.Path,
                    $"'{nativeType}' is not a plain type name"));
                continue;
            }

            types.Add(nativeType);
        }

        // Only "there was nothing to read at all": a file whose entries all
        // failed to parse has already been reported per entry.
        if (!found)
            diagnostics.Add(Diagnostic.Create(NoRegistrations, At(text.Path, content, root), text.Path));

        return new ObjectManagerFile(text.Path, types.ToImmutable(), diagnostics.ToImmutable());
    }

    /// <summary>
    /// The location of a node for a diagnostic. An <c>AdditionalFile</c> is not a
    /// syntax tree of the compilation, so a location that names it has to be an
    /// external-file one - the compiler rejects anything else.
    /// </summary>
    private static Location At(string path, SourceText content, SyntaxNode node)
    {
        var span = node.Span;
        return Location.Create(path, span, content.Lines.GetLinePositionSpan(span));
    }

    /// <summary>
    /// Whether this is a call on <c>GLib.GType</c> - spelled either fully
    /// qualified or through a <c>using GLib;</c> at the top of the file.
    /// </summary>
    private static bool IsGTypeRegister(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Register" } member
        && Normalise(member.Expression.ToString()) is "GType" or "GLib.GType";

    private static string Normalise(string name) =>
        name.StartsWith("global::", StringComparison.Ordinal) ? name.Substring("global::".Length) : name;

    private static string Generate(ImmutableArray<string> types) => $$"""
        // <auto-generated />

        namespace GtkSharp.GstreamerSharp;

        [{{GeneratorInfo.GeneratedCodeAttribute}}]
        public static class ObjectManager
        {
            static bool initialized;

            // Call this method from the appropriate module init function.
            public static void Initialize()
            {
                if (initialized)
                    return;

                // Set before the first registration: reading a GType runs that
                // type's static constructor, which calls this method again.
                initialized = true;

        {{Registrations(types)}}
            }
        }
        """ + NewLine;

    private static string Registrations(ImmutableArray<string> types) =>
        string.Join(NewLine, types.Select(static type =>
            $"{RegistrationIndent}GTypeRegistry.Register(static () => global::{type}.GType, typeof(global::{type}));"));

    /// <summary>
    /// One parsed <c>ObjectManager.cs</c>: the types it registers, plus whatever
    /// it could not read.
    /// </summary>
    private sealed record ObjectManagerFile(
        string FilePath,
        ImmutableArray<string> Types,
        ImmutableArray<Diagnostic> Diagnostics);
}
