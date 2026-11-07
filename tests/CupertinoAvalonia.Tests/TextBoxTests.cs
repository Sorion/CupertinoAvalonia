using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
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
        var window = Show(new Window { Width = 800, Height = 200, Content = new StackPanel { Children = { new Border { Height = 80, Child = row } } } });

        Assert.Equal([16d, 20, 24, 28], row.Children.Take(4).Select(c => c.Bounds.Height));
        Assert.Equal(80, multiLine.Bounds.Height);
        window.Close();
    }

    // NSSecureTextField on macOS 27, 13 pt: a dot every 8.08 pt (8.05 from the theme's fit across sizes), whatever the
    // mask character.
    [AvaloniaTheory]
    [InlineData('•')]
    [InlineData('*')]
    public void Password_dots_are_spaced_as_macOS_and_keep_the_line(char mask)
    {
        var password = new TextBox { Width = 190, PasswordChar = mask, Text = "password" };
        var text = new TextBox { Width = 190, Text = "password" };
        var window = Show(password, text);

        var dots = Part<TextPresenter>(password, "PART_TextPresenter").TextLayout;
        var plain = Part<TextPresenter>(text, "PART_TextPresenter").TextLayout;
        Assert.Equal(8 * 8.05, dots.WidthIncludingTrailingWhitespace, 0.05);
        Assert.Equal(3 * 8.05, dots.HitTestTextPosition(3).X, 0.05);
        Assert.Equal(8 * 8.05, dots.HitTestTextPosition(8).X, 0.05);
        // A click on the last dot's right half puts the caret after it, as in text.
        var hit = dots.HitTestPoint(new Point(7.7 * 8.05, 5)).CharacterHit;
        Assert.Equal(8 * 8.05, dots.TextLines[0].GetDistanceFromCharacterHit(hit), 0.05);
        Assert.Equal(plain.Height, dots.Height);
        Assert.Equal(plain.TextLines[0].Baseline, dots.TextLines[0].Baseline);
        Assert.Equal(password.Bounds.Height, text.Bounds.Height);
        window.Close();
    }

    // Each dot centered in its pitch, 0.36 em above the baseline; the mask character itself isn't drawn.
    [AvaloniaFact]
    public void Password_dots_are_drawn_round_between_the_carets()
    {
        var password = new TextBox { Width = 190, PasswordChar = '•', Text = "password" };
        var window = Show(password);
        var presenter = Part<TextPresenter>(password, "PART_TextPresenter");
        var origin = Origin(presenter, window);
        var centerY = origin.Y + presenter.TextLayout.TextLines[0].Baseline - 0.36 * 13;

        using var frame = window.CaptureRenderedFrame()!;
        var paper = Pixel(frame, (int)origin.X + 100, (int)centerY);
        int Ink(double x, double y) => Distance(Pixel(frame, (int)(origin.X + x), (int)y), paper);
        Assert.All(new[] { 0, 7 }, i => Assert.InRange(Ink((i + 0.5) * 8.05, centerY), 500, 765));
        Assert.InRange(Ink(8.05, centerY), 0, 60);
        Assert.InRange(Ink(4, centerY - 4), 0, 60);
        window.Close();
    }

    [AvaloniaFact]
    public void A_revealed_password_shows_its_text()
    {
        var password = new TextBox { Width = 190, PasswordChar = '•', Text = "password", RevealPassword = true };
        var text = new TextBox { Width = 190, Text = "password" };
        var window = Show(password, text);

        Assert.Equal(Part<TextPresenter>(text, "PART_TextPresenter").TextLayout.WidthIncludingTrailingWhitespace,
            Part<TextPresenter>(password, "PART_TextPresenter").TextLayout.WidthIncludingTrailingWhitespace);
        window.Close();
    }
}
