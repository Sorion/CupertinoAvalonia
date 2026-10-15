using Avalonia.Animation;

namespace CupertinoAvalonia.Styling;

/// <summary>
/// A length transition that jumps when either end is automatic (NaN): <see cref="DoubleTransition"/> would keep the
/// length automatic until the transition ends.
/// </summary>
public sealed class LengthTransition : InterpolatingTransitionBase<double>
{
    protected override double Interpolate(double progress, double from, double to) =>
        double.IsNaN(from) || double.IsNaN(to) ? to : from + (to - from) * progress;
}
