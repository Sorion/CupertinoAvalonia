using Avalonia;
using Avalonia.Media;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// Where an indeterminate progress bar's segment has slid, which its animation moves. Animated on the segment, a visual,
/// the animation pauses while the bar isn't shown, as Avalonia pauses style animations; animated on its
/// TranslateTransform, it kept the render loop awake behind a hidden bar (about 5 % CPU on macOS).
/// </summary>
internal static class IndeterminateSlide
{
    public static readonly AttachedProperty<double> XProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("X", typeof(IndeterminateSlide));

    public static readonly AttachedProperty<double> YProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("Y", typeof(IndeterminateSlide));

    static IndeterminateSlide()
    {
        XProperty.Changed.AddClassHandler<Visual>((visual, _) => Apply(visual));
        YProperty.Changed.AddClassHandler<Visual>((visual, _) => Apply(visual));
    }

    public static double GetX(Visual visual) => visual.GetValue(XProperty);

    public static void SetX(Visual visual, double value) => visual.SetValue(XProperty, value);

    public static double GetY(Visual visual) => visual.GetValue(YProperty);

    public static void SetY(Visual visual, double value) => visual.SetValue(YProperty, value);

    private static void Apply(Visual visual)
    {
        if (visual.RenderTransform is not TranslateTransform translate)
            visual.RenderTransform = translate = new TranslateTransform();
        translate.X = visual.GetValue(XProperty);
        translate.Y = visual.GetValue(YProperty);
    }
}
