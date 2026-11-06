using Avalonia;
using Avalonia.Controls.Primitives;

namespace CupertinoAvalonia.Gallery.Controls;

/// <summary>A row's name in a States or Sizes table, with a note ("No class") or the class or property it takes.</summary>
public class RowLabel : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<RowLabel, string?>(nameof(Text));

    public static readonly StyledProperty<string?> NoteProperty =
        AvaloniaProperty.Register<RowLabel, string?>(nameof(Note));

    /// <summary>The classes or properties to type, comma-separated, each in a code chip: "accent,IsDefault".</summary>
    public static readonly StyledProperty<string?> CodeProperty =
        AvaloniaProperty.Register<RowLabel, string?>(nameof(Code));

    public static readonly DirectProperty<RowLabel, IReadOnlyList<string>> CodesProperty =
        AvaloniaProperty.RegisterDirect<RowLabel, IReadOnlyList<string>>(nameof(Codes), label => label.Codes);

    private IReadOnlyList<string> _codes = [];

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Note
    {
        get => GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }

    public string? Code
    {
        get => GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public IReadOnlyList<string> Codes
    {
        get => _codes;
        private set => SetAndRaise(CodesProperty, ref _codes, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CodeProperty)
            Codes = Code?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
