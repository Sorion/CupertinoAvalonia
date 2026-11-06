using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class ProgressBarTests
{
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Bar_is_as_NSProgressIndicator(string theme)
    {
        var variant = UseTheme(theme);
        var regular = new ProgressBar { Width = 200, Value = 40 };
        var small = new ProgressBar { Width = 200, Value = 40, Classes = { "small" } };
        var window = Show(regular, small);

        // Measured on macOS 27 at 2x: 8 pt, 6 pt small; the track black or white at 6 % and 8 % (F0F0F0 on white,
        // 303030 on 1E1E1E), lighter than a slider's, under the kit's stroke.
        Assert.Equal(8, regular.Bounds.Height);
        Assert.Equal(6, small.Bounds.Height);
        Assert.Equal(Describe(Resource("ProgressTrackBrush", variant)), Describe(regular.Background));
        Assert.Equal(Describe(Resource("ProgressTrackStrokeBrush", variant)), Describe(regular.BorderBrush));
        window.Close();
    }
}
