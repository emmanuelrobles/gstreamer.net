using System.Text.RegularExpressions;

namespace codecrush.gstreamer.net.tests;

/// <summary>
/// The GType registry is a local override of a generated file, so a submodule
/// bump can add an object type that the override never registers. Nothing would
/// fail - the type would just never be wrapped in its most derived managed
/// class - so the two files are compared instead.
/// </summary>
public class ObjectManagerCoverageTest
{
    [Fact]
    public void RegistersTheSameTypesAsUpstream()
    {
        var upstream = RegisteredTypes("upstream/ObjectManager.cs");
        var overrides = RegisteredTypes("overrides/ObjectManager.cs");

        Assert.Equal(upstream, overrides);
    }

    [Fact]
    public void RegistersEveryGeneratedObjectType()
    {
        // Sanity check on the comparison itself: an empty or truncated file
        // would otherwise make the equality above vacuous.
        Assert.True(RegisteredTypes("overrides/ObjectManager.cs").Count > 80);
    }

    private static SortedSet<string> RegisteredTypes(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, relativePath);
        Assert.True(File.Exists(path), $"missing '{path}'; see the None/CopyToOutputDirectory items in the test project");

        // Both files name the type with typeof(...); only the surrounding call
        // differs, because the override defers the GType lookup into a lambda.
        var types = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(File.ReadAllText(path), @"typeof\s*\(\s*([\w.]+)\s*\)"))
            types.Add(match.Groups[1].Value);

        return types;
    }
}
