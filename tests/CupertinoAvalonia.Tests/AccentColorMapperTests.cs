using Avalonia.Media;
using CupertinoAvalonia.Media;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class AccentColorMapperTests
{
    [Theory]
    // The colors macOS reports for each accent (pre-26 palette), light and dark.
    [InlineData("#007AFF", "AccentBlueColor")]
    [InlineData("#0A84FF", "AccentBlueColor")]
    [InlineData("#AF52DE", "AccentPurpleColor")]
    [InlineData("#FF2D55", "AccentPinkColor")]
    [InlineData("#FF3B30", "AccentRedColor")]
    [InlineData("#FF9F0A", "AccentOrangeColor")]
    [InlineData("#FFCC00", "AccentYellowColor")]
    [InlineData("#28CD41", "AccentGreenColor")]
    [InlineData("#8E8E93", "GrayColor")]
    // Small color space differences still match.
    [InlineData("#0479FD", "AccentBlueColor")]
    public void Recognizes_the_macOS_accent_colors(string reported, string expectedKey)
    {
        Assert.Equal(expectedKey, AccentColorMapper.FindAccentResourceKey(Color.Parse(reported)));
    }

    [Theory]
    [InlineData("#0078D4")] // Windows default accent
    [InlineData("#6B2FBF")]
    public void Ignores_other_colors(string reported)
    {
        Assert.Null(AccentColorMapper.FindAccentResourceKey(Color.Parse(reported)));
    }

    [Fact]
    public void Linear_burn_and_dodge_shift_every_channel()
    {
        // _Control BG/Bordered - Prominent, Clicked: #0088FF under a linear burn of #E6E6E6.
        Assert.Equal(Color.Parse("#006FE6"), AccentColorMapper.LinearBurn(Color.Parse("#0088FF"), 0xE6));
        // Dark: #0091FF under a linear dodge of #090909.
        Assert.Equal(Color.Parse("#099AFF"), AccentColorMapper.LinearDodge(Color.Parse("#0091FF"), 0x09));
    }

    [Fact]
    public void Darken_reproduces_the_kit_table_selection()
    {
        // "Tables/BG - Selected + Active" is #0165E2 for the #0088FF accent, #0158D2 for #0091FF.
        AssertClose(Color.Parse("#0165E2"), AccentColorMapper.Darken(Color.Parse("#0088FF"), 0.055, hue: 5));
        AssertClose(Color.Parse("#0158D2"), AccentColorMapper.Darken(Color.Parse("#0091FF"), 0.086, hue: 9));
    }

    private static void AssertClose(Color expected, Color actual)
    {
        const int tolerance = 6;
        Assert.True(Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance
                    && Math.Abs(expected.B - actual.B) <= tolerance, $"Expected {expected}, got {actual}");
    }
}
