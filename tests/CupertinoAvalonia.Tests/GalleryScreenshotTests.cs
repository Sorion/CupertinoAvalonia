using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using CupertinoAvalonia.Gallery.ViewModels;
using CupertinoAvalonia.Gallery.Views;
using CupertinoAvalonia.Styling;
using Xunit;

namespace CupertinoAvalonia.Tests;

/// <summary>
/// Renders every gallery page in light and dark mode, in an active and an inactive window, for visual review
/// against Apple's kit.
/// Opt-in: set GALLERY_SCREENSHOTS to an output folder, and optionally GALLERY_PAGE to a page title.
/// </summary>
public class GalleryScreenshotTests
{
    [AvaloniaTheory]
    [InlineData("Light", false)]
    [InlineData("Dark", false)]
    [InlineData("Light", true)]
    [InlineData("Dark", true)]
    public void Render_gallery_pages(string theme, bool inactive)
    {
        var outDir = Environment.GetEnvironmentVariable("GALLERY_SCREENSHOTS");
        Assert.SkipWhen(string.IsNullOrEmpty(outDir), "Set GALLERY_SCREENSHOTS to render the gallery.");
        var pageFilter = Environment.GetEnvironmentVariable("GALLERY_PAGE");
        Directory.CreateDirectory(outDir!);

        Application.Current!.RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1100, Height = 760 };
        window.Show();
        // The window activates once its jobs run (the first page has no endless animation): only then
        // can it be made to look inactive, until something activates it again.
        Dispatcher.UIThread.RunJobs();
        WindowActivity.SetIsInactive(window, inactive);

        foreach (var page in vm.Pages.Where(p => string.IsNullOrEmpty(pageFilter) || p.Title.Contains(pageFilter, StringComparison.OrdinalIgnoreCase)))
        {
            vm.SelectedPage = page;
            // Don't use Dispatcher.RunJobs: infinite animations (indeterminate progress) never drain it.
            window.UpdateLayout();
            var frame = window.CaptureRenderedFrame()!;
            frame.Save(Path.Combine(outDir!, $"{page.Title.Replace(' ', '-')}-{theme}{(inactive ? "-Inactive" : "")}.png"));
        }

        window.Close();
    }
}
