using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;

[assembly: AvaloniaTestApplication(typeof(CupertinoAvalonia.Tests.TestApp))]

namespace CupertinoAvalonia.Tests;

public class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

    public override void Initialize()
    {
        var theme = new CupertinoAvaloniaTheme();
        // The tests expect the default sidebar size, whatever the Mac's setting.
        theme.ApplySidebarSize(Media.SidebarSize.Medium);
        Styles.Add(theme);
    }
}
