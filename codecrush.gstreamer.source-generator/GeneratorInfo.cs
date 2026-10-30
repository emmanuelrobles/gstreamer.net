namespace codecrush.gstreamer.source_generator;

/// <summary>
/// Values that every generated file carries.
/// </summary>
internal static class GeneratorInfo
{
    /// <summary>
    /// The <c>[GeneratedCode]</c> attribute naming this generator assembly and
    /// its version, so a decompiler or a debugger can tell a generated file from
    /// hand-written code.
    /// </summary>
    public static readonly string GeneratedCodeAttribute = BuildGeneratedCodeAttribute();

    private static string BuildGeneratedCodeAttribute()
    {
        var assembly = typeof(GeneratorInfo).Assembly.GetName();
        return $"""global::System.CodeDom.Compiler.GeneratedCode("{assembly.Name}", "{assembly.Version}")""";
    }
}
