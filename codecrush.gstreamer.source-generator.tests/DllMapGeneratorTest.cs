using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace codecrush.gstreamer.source_generator.tests;

public class DllMapGeneratorTest
{
    [Fact]
    public Task GeneratesDllMap()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("", cancellationToken: TestContext.Current.CancellationToken);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree });

        var generator = new DllMapperGenerator();

        AdditionalText[] additionalFiles =
        [
            new InMemoryAdditionalText(
                "/virtual/path/gstreamer-sharp.dll.config",
                """
                 <configuration>
                  <!-- Linux -->
                  <dllmap dll="gobject-2.0-0.dll" target="gobject-2.0.so.0" os="linux"/>
                  <dllmap dll="gthread-2.0-0.dll" target="libgthread-2.0.so.0" os="linux"/>
                  <!-- Mac OS X -->
                  <dllmap dll="gobject-2.0-0.dll" target="gobject-2.0.dylib" os="osx"/>
                  <dllmap dll="gthread-2.0-0.dll" target="libgthread-2.0.dylib" os="osx"/>
                </configuration>
                """)
        ];

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalFiles);

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);

        return Verifier.Verify(driver);
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }
}