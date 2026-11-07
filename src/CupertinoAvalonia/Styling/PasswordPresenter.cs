using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// A text field's text, whose password mask is drawn as NSSecureTextField draws it: round dots, whatever
/// <c>PasswordChar</c> is. Avalonia masks with that character in the field's font, and no character comes close: "•"
/// is half the size, "●" nearly twice. The mask stays text, spaced as the dots and not drawn, so the caret, the
/// selection and hit testing work as for any text; a layer above draws the dots.
/// </summary>
internal sealed class PasswordPresenter : TextPresenter
{
    // Measured on macOS 27, NSSecureTextField in the system font from 9 to 15 pt: the dots' diameter, their pitch
    // (5.83 pt at 9 pt, 8.08 at 13, 9.21 at 15), and their center above the baseline, mid cap height. Each dot is
    // centered in its pitch.
    private const double DiameterEm = 0.432;
    private const double PitchEm = 0.553;
    private const double PitchExtra = 0.86;
    private const double CenterAboveBaselineEm = 0.36;

    // In the system font of every platform, and alone in its cluster.
    private const char Mask = '•';

    private readonly DotLayer _dots;
    private Size _constraint;
    // The number of dots, 0 when the text shows.
    private int _masked;

    public PasswordPresenter()
    {
        _dots = new DotLayer(this) { IsHitTestVisible = false };
        VisualChildren.Add(_dots);
    }

    protected override Type StyleKeyOverride => typeof(TextPresenter);

    // The text, the selection, the colors, the font: the layer can't tell, and drawing it costs little.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        _dots?.InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _constraint = availableSize;
        _dots.Measure(availableSize);
        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // As TextPresenter does with its own constraint, which it keeps private, before it lays the text out again.
        if (Math.Abs(_constraint.Width - finalSize.Width) > 1e-9)
            _constraint = new Size(Math.Ceiling(finalSize.Width), double.PositiveInfinity);
        var size = base.ArrangeOverride(finalSize);
        _dots.Arrange(new Rect(size));
        return size;
    }

    protected override TextLayout CreateTextLayout()
    {
        var length = (Text?.Length ?? 0) + (PreeditText?.Length ?? 0);
        _masked = PasswordChar != default && !RevealPassword ? length : 0;
        if (_masked == 0)
            return base.CreateTextLayout();

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        using var mask = new TextLayout(Mask.ToString(), typeface, FontSize);
        var spacing = Pitch - mask.WidthIncludingTrailingWhitespace;

        return new TextLayout(new string(Mask, length), typeface, FontSize, Brushes.Transparent, TextAlignment,
            TextWrapping, null, null, FlowDirection,
            _constraint.Width == 0 ? double.PositiveInfinity : _constraint.Width,
            _constraint.Height == 0 ? double.PositiveInfinity : _constraint.Height,
            LineHeight, spacing, 0, FontFeatures, null);
    }

    private double Pitch => PitchEm * FontSize + PitchExtra;

    // Over the text and the selection, as the text is; the caret sits between two dots.
    private sealed class DotLayer(PasswordPresenter presenter) : Control
    {
        public override void Render(DrawingContext context)
        {
            // Lays the text out first if it has to, which counts the dots.
            var layout = presenter.TextLayout;
            if (presenter._masked == 0)
                return;

            // As TextPresenter places the text.
            var top = presenter.Bounds.Height < layout.Height
                ? presenter.VerticalAlignment switch
                {
                    VerticalAlignment.Center => (presenter.Bounds.Height - layout.Height) / 2,
                    VerticalAlignment.Bottom => presenter.Bounds.Height - layout.Height,
                    _ => 0,
                }
                : 0;

            var fontSize = presenter.FontSize;
            var radius = DiameterEm * fontSize / 2;
            var half = presenter.Pitch / 2 * (presenter.FlowDirection == FlowDirection.RightToLeft ? -1 : 1);
            var start = Math.Min(presenter.SelectionStart, presenter.SelectionEnd);
            var end = Math.Max(presenter.SelectionStart, presenter.SelectionEnd);
            var selected = presenter.ShowSelectionHighlight ? presenter.SelectionForegroundBrush : null;

            for (var i = 0; i < presenter._masked; i++)
            {
                var line = layout.TextLines[layout.GetLineIndexFromCharacterIndex(i, false)];
                var caret = layout.HitTestTextPosition(i);
                var center = new Point(caret.X + half, top + caret.Y + line.Baseline - CenterAboveBaselineEm * fontSize);
                var brush = i >= start && i < end && selected is not null ? selected : presenter.Foreground;
                context.DrawEllipse(brush, null, center, radius, radius);
            }
        }
    }
}
