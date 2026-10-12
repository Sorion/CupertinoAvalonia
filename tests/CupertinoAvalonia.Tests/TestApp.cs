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
        Styles.Add(new CupertinoAvaloniaTheme());
    }
}
