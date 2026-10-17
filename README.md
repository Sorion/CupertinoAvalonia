<p align="center">
<img src="https://github.com/Sorion/CupertinoAvalonia/blob/main/assets/icon.png" width="128" alt="CupertinoAvalonia logo">
</p>


# CupertinoAvalonia


[![Nuget](https://img.shields.io/nuget/vpre/CupertinoAvalonia?color=%236A5ACD&label=CupertinoAvalonia%20%28nuget%29)](https://www.nuget.org/packages/CupertinoAvalonia/)
[![CI](https://github.com/Sorion/CupertinoAvalonia/actions/workflows/ci.yml/badge.svg?branch=main&event=push)](https://github.com/Sorion/CupertinoAvalonia/actions/workflows/ci.yml)
[![Avalonia](https://img.shields.io/badge/Avalonia-v12-%236A5ACD)](https://avaloniaui.net)


Give your [Avalonia](https://avaloniaui.net) application a native macOS look.
Inspired by [FluentAvalonia](https://github.com/amwx/FluentAvalonia).

_Note: The theme is still in an early stage_

## Introduction

CupertinoAvalonia tries to match the macOS 27 design system. It uses Apple's Figma file and screen captures of native apps as references. As such, a gap between Swift-made apps and this kit may appear. Liquid Glass elements are simulated in two ways:
- A custom Skia shader inspired by [LiquidGlassAvaloniaUI](https://github.com/KaranocaVe/LiquidGlassAvaloniaUI) and
  [LiquidAvalonia](https://github.com/A1isandr/LiquidAvalonia).
- Different layers to mimic some effects.


## Getting started

Add the package (`dotnet add package CupertinoAvalonia`), then use the theme instead of `FluentTheme` in
`App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:ca="using:CupertinoAvalonia">
  <Application.Styles>
    <ca:CupertinoAvaloniaTheme />
  </Application.Styles>
</Application>
```

Light and dark follow `RequestedThemeVariant`; the accent follows System Settings (`CustomAccentColor` forces one).
On macOS, the accent, the selection and the focus ring are AppKit's own colors in both modes; elsewhere, and for a
custom accent, their shades are derived from the accent.

## Controls

| Control | Classes |
|---|---|
| `Button`, `RepeatButton`, `ToggleButton` | `accent` (or `IsDefault`), `tinted`, `destructive`, `borderless` |
| `DropDownButton`, `SplitButton`, `ToggleSplitButton` | A pull-down button, with a button's classes (`toolbar` included); an action and, apart, its menu, as AppKit's split combo button |
| `CheckBox`, `RadioButton`, `ToggleSwitch` | |
| `TextBox`, `ComboBox`, `AutoCompleteBox` | The autocomplete box's suggestions open in a menu |
| `NumericUpDown`, `ButtonSpinner` | A field with the stepper inside; the stepper alone |
| `ListBox` | `sidebar`, `alternating`, `segmented` (a segmented control; `SelectionMode="Multiple"` for several) |
| `TreeView` | An outline view; `sidebar` for nested sidebar items |
| `MenuFlyout`, `ContextMenu`, `Flyout`, `ToolTip` | |
| `Slider`, `ProgressBar`, `Expander`, `ScrollBar` | `small` (progress bar), `button` (expander) |
| `SplitView` | Pane on the left: a sidebar (on the window's material in a `CupertinoWindow`). On the right: an inspector, in the sidebar's color. `plain`: the pane takes the content's background and a divider, as in Shortcuts. `PaneBackground` and the resources `SplitView…BackgroundBrush`, `SplitViewInspectorLength`, `SplitViewDividerThickness` change the rest |
| `TabControl` | A segmented control on top of the content (`TabStripPlacement`: `Bottom` below it, `Left` and `Right` beside it). `toolbar`: a settings window's tabs, on glass. `Icon="gearshape"` gives a tab an SF Symbol |

- **Sizes:** `mini`, `small`, `large` (and `xlarge` on buttons, segmented controls and steppers).
- **Toolbar items:** add `toolbar` to a `Button`, `ToggleButton`, `DropDownButton`, `ComboBox`, `TextBox` (a search field) or
  segmented `ListBox` for a Liquid Glass capsule; `accent` marks the primary item. A `Border` with
  `toolbar-group` puts several buttons (in a `StackPanel`, without the class) in one capsule. Items are 36 pt
  tall for a `Unified` toolbar; add `compact` for an `UnifiedCompact` one. Pressed, they swell as in macOS 27:
  briefly after a click, up to 3 s while held, and as long as their flyout or drop-down is open (a group or
  segmented control as a whole; the search field doesn't).
- **Text styles:** `TextBlock` classes `largeTitle`, `title1`…`title3`, `headline`, `body`, `callout`,
  `subheadline`, `footnote`, `caption1`, `caption2`, plus `emphasized`, `secondary`, `tertiary`.
- **Resources** are named after the kit: `AccentBrush`, `LabelSecondaryBrush`, `WindowBackgroundBrush`,
  `ControlHeight`… `AccentMulticolorBrush` is System Settings' multicolor swatch, for an accent picker.
- **Inactive windows** gray out their controls, as on macOS. Your styles can do the same with
  `[(cas|WindowActivity.IsInactive)=True]` (`xmlns:cas="using:CupertinoAvalonia.Styling"`).
- **Other controls** (date and time pickers, `Calendar`…) keep Fluent's templates,
  recolored with Apple's palette.

## CupertinoWindow

```xml
<cac:CupertinoWindow xmlns="https://github.com/avaloniaui"
                     xmlns:cac="using:CupertinoAvalonia.Controls"
                     ExtendsContentIntoTitleBar="True"
                     ToolbarStyle="Unified"
                     Material="Sidebar">
```

On macOS (elsewhere it's a plain `Window`):

- `ExtendsContentIntoTitleBar`: the content runs under the title bar. Keep it out of the window controls' way
  with `WindowControlsInset` (as padding on a toolbar when no sidebar is shown, for one), and mark the areas that
  move the window with `WindowDecorationProperties.ElementRole="TitleBar"`.
- `ToolbarStyle`: `Unified` (52 pt) or `UnifiedCompact` (40 pt) title bar, the window controls centered in it.
- `Material`: a system material behind the window (`Sidebar`, `WindowBackground`, `UnderWindowBackground`,
  `HudWindow`), shown wherever the content is transparent.

The Gallery's window ([MainWindow.axaml](https://github.com/Sorion/CupertinoAvalonia/blob/main/samples/CupertinoAvalonia.Gallery/Views/MainWindow.axaml)) is a
complete example: a sidebar on the sidebar material, a toolbar over the content.

**Liquid Glass chrome:** macOS draws it only for apps whose executable declares a recent SDK (26 or later), which
.NET's app host doesn't. The package's `buildTransitive/CupertinoAvalonia.targets` declares macOS 27's SDK after each build on
macOS; with a project reference, import that file in your app (as the Gallery does). `CupertinoDeclareMacOSSdk=false`
turns it off, `CupertinoMacOSSdkVersion` declares another SDK.

## CupertinoToolbar

```xml
<Panel>
  <ScrollViewer Name="Scroll">
    <ContentControl Margin="20,72,20,20" Content="{Binding Page}" />
  </ScrollViewer>
  <cac:CupertinoToolbar ScrollViewer="{Binding #Scroll}">
    <!-- toolbar items -->
  </cac:CupertinoToolbar>
</Panel>
```

The window's toolbar band, laid over the top of the content as in macOS 27: transparent at rest, its background
(and its 1 pt edge line) fades in when the content scrolls under it or the pointer is over it. In dark mode it is a
third color, lighter than the content; in an inactive window it shows, at rest too. `BackgroundVisibility`:
`Automatic` (default), `Visible` (always), `Hidden` (never). Dragging the band moves the window. Leave room for it at
the top of the scrolled content. The content passing under it isn't blurred yet: the background is nearly opaque.

Its buttons, pop-ups and segmented controls are toolbar items without the `toolbar` class (and `compact` ones in a
`compact` toolbar), except in a `toolbar-group`. Controls in a flyout or in another control's template stay as they are.

## SymbolIcon

```xml
<Button Classes="toolbar">
  <cac:SymbolIcon Symbol="square.and.arrow.up" />
</Button>
```

An SF Symbol by its name in Apple's catalog, at the size, weight and color of the text around it (`FontSize`,
`FontWeight`, `Foreground`); `SymbolScale="Large"` is the scale of toolbar items, which toolbar styles set.

- **On macOS 11 and later**, macOS draws its own symbol: the whole catalog is available. No font installed with
  macOS contains them, so the theme's chevrons, checkmarks and magnifying glass use them too.
- **Elsewhere**, about a hundred common symbols have a Fluent counterpart, from Segoe Fluent Icons on Windows or the
  package's embedded font (FluentAvalonia's, MIT). `FallbackGlyph` gives one for any other symbol
  (`FallbackGlyph="&#xE72D;"`), and `FallbackData` a drawing instead.

## Good to know

- The switch's and the slider's pressed knobs are real glass lenses (`Styling/GlassLens.cs`, a Skia shader
  inspired by [LiquidGlassAvaloniaUI](https://github.com/KaranocaVe/LiquidGlassAvaloniaUI) and
  [LiquidAvalonia](https://github.com/A1isandr/LiquidAvalonia)). Menus, popovers and sidebars aren't glass yet.
- Focus rings and lenses are drawn in the adorner layer so containers don't clip them:
  [docs/avalonia-clipping.md](https://github.com/Sorion/CupertinoAvalonia/blob/main/docs/avalonia-clipping.md) explains how, for any control.
- The theme adds letter spacing to the system font, which Skia renders tighter than macOS does.
- On macOS, the theme tags every window as sRGB. Avalonia leaves the color space unset, so on a wide-gamut display
  (every recent Mac) colors looked more saturated than in native apps. Your own colors and images are affected too.
  Avalonia's [#21847](https://github.com/AvaloniaUI/Avalonia/pull/21847) proposes an option for it, which the theme
  will use once released.

## Design tokens

The theme's colors, metrics and text styles (`Styling/Tokens/*.g.axaml`) are generated from Apple's macOS 27 UI
kit for Figma, and included in the library. Apple's kit itself isn't in the
repository. To regenerate the tokens when Apple updates it:

1. Duplicate Apple's [macOS 27 kit](https://developer.apple.com/design/resources/) from the Figma Community, then
   in the Figma desktop app, **File → Save local copy…** to `design/source/macOS-27.fig` (ignored by git).
2. In `tools/figma` (Node.js): `npm install`, then `npm run generate`. It works offline, without a Figma account.


## License

MIT, see [LICENSE](https://github.com/Sorion/CupertinoAvalonia/blob/main/LICENSE) and [THIRD-PARTY-NOTICES.md](https://github.com/Sorion/CupertinoAvalonia/blob/main/THIRD-PARTY-NOTICES.md).
