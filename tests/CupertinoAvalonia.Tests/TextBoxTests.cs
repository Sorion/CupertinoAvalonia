using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Xunit;

namespace CupertinoAvalonia.Tests;

public class TextBoxTests
{
    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("search")]
    public void Fields_keep_their_height_next_to_taller_ones(string? kind)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var size in new[] { "mini", "small", null, "large" })
        {
            var field = new TextBox { Width = 120 };
            if (kind is not null)
                field.Classes.Add(kind);
            if (size is not null)
                field.Classes.Add(size);
            row.Children.Add(field);
        }
        var multiLine = new TextBox { AcceptsReturn = true, Width = 120 };
        row.Children.Add(multiLine);
        var window = new Window { Width = 800, Height = 200, Content = new StackPanel { Children = { new Border { Height = 80, Child = row } } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([16d, 20, 24, 28], row.Children.Take(4).Select(c => c.Bounds.Height));
        Assert.Equal(80, multiLine.Bounds.Height);
        window.Close();
    }
}
