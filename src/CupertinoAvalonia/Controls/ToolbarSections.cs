using Avalonia;
using Avalonia.Controls;

namespace CupertinoAvalonia.Controls;

/// <summary>
/// The content's and the inspector's sections of a <see cref="NavigationSplitView"/>'s toolbar, as NSToolbar lays out
/// SwiftUI's (measured on macOS 27): the inspector's items start at its edge but never run past the window's, so they
/// stay at the window's trailing edge once the inspector is closed; the content's items end before them.
/// </summary>
/// <remarks>Its first child is the content's section, its second the inspector's.</remarks>
internal sealed class ToolbarSections : Panel
{
    private const double EdgeMargin = NavigationSplitView.EdgeMargin;

    private double _contentStart;
    private double _inspectorEdge;

    /// <summary>Where the content's items start, past the sidebar button.</summary>
    public double ContentStart
    {
        get => _contentStart;
        set
        {
            _contentStart = value;
            InvalidateMeasure();
        }
    }

    /// <summary>How much of the inspector shows, from the right.</summary>
    public double InspectorEdge
    {
        get => _inspectorEdge;
        set
        {
            _inspectorEdge = value;
            InvalidateMeasure();
        }
    }

    private Control? ContentSection => Children.Count > 0 ? Children[0] : null;

    private Control? InspectorSection => Children.Count > 1 && Children[1] is { IsVisible: true } section ? section : null;

    // The content's section gets the room the inspector's leaves it, so its title trims as it should.
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var height = 0.0;
        if (InspectorSection is { } inspector)
        {
            inspector.Measure(availableSize);
            height = inspector.DesiredSize.Height;
        }
        if (ContentSection is { } content)
        {
            content.Measure(new Size(Math.Max(0, ContentEnd(width) - ContentStart), availableSize.Height));
            height = Math.Max(height, content.DesiredSize.Height);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (InspectorSection is { } inspector)
            inspector.Arrange(new Rect(InspectorStart(finalSize.Width), 0, inspector.DesiredSize.Width, finalSize.Height));
        ContentSection?.Arrange(new Rect(ContentStart, 0, Math.Max(0, ContentEnd(finalSize.Width) - ContentStart), finalSize.Height));
        return finalSize;
    }

    // From the inspector's edge, but no further than the window's.
    private double InspectorStart(double width) =>
        Math.Min(width - InspectorEdge + EdgeMargin, width - EdgeMargin - InspectorSection!.DesiredSize.Width);

    private double ContentEnd(double width) =>
        InspectorSection is null ? width - EdgeMargin : InspectorStart(width) - 2 * EdgeMargin;
}
