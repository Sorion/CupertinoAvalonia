using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>
/// Every resource key the theme's XAML refers to must resolve, in light and dark mode. A missing
/// DynamicResource fails silently at runtime (the property just stays unset), hence this test.
/// </summary>
public partial class ResourceKeyTests
{
    // Keys provided at runtime by the theme's code (CupertinoAvaloniaTheme, SystemFontTracking) are
    // checked too: they must resolve once the theme is loaded.
    [GeneratedRegex(@"\{(?:DynamicResource|StaticResource)\s+(?:ResourceKey=)?([A-Za-z0-9_]+)\s*\}")]
    private static partial Regex ResourceReference();

    public static TheoryData<string> Variants => ["Light", "Dark"];

    [AvaloniaTheory]
    [MemberData(nameof(Variants))]
    public void All_referenced_resources_resolve(string variantName)
    {
        var variant = variantName == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var sources = Directory.GetFiles(ThemeSourceDirectory(), "*.axaml", SearchOption.AllDirectories);
        Assert.NotEmpty(sources);

        var missing = new List<string>();
        foreach (var file in sources)
        {
            foreach (Match match in ResourceReference().Matches(File.ReadAllText(file)))
            {
                var key = match.Groups[1].Value;
                if (!Application.Current!.TryGetResource(key, variant, out _))
                    missing.Add($"{Path.GetFileName(file)}: {key}");
            }
        }

        Assert.True(missing.Count == 0, "Missing resources:\n" + string.Join("\n", missing.Distinct()));
    }

    /// <summary>The theme's XAML sources, found by walking up from the test binaries to the repository.</summary>
    private static string ThemeSourceDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CupertinoAvalonia", "Styling");
            if (Directory.Exists(candidate))
                return candidate;
        }

        throw new DirectoryNotFoundException("src/CupertinoAvalonia/Styling not found above " + AppContext.BaseDirectory);
    }
}
