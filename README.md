# CupertinoAvalonia

A macOS 27 theme for [Avalonia](https://avaloniaui.net)'s built-in controls, plus `CupertinoWindow`, a window
with macOS's chrome. Inspired by [FluentAvalonia](https://github.com/amwx/FluentAvalonia). Status: early.

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

## Controls

| Control | Classes |
|---|---|
| `Button`, `RepeatButton`, `ToggleButton` | `accent` (or `IsDefault`), `tinted`, `destructive`, `borderless` |
| `CheckBox`, `RadioButton`, `ToggleSwitch` | |
| `TextBox`, `ComboBox` | |
| `ListBox` | `sidebar`, `alternating` |
| `MenuFlyout`, `ContextMenu`, `Flyout`, `ToolTip` | |
| `Slider`, `ProgressBar`, `Expander`, `ScrollBar` | `small` (progress bar), `button` (expander) |

- **Sizes:** `mini`, `small`, `large` (and `xlarge` on buttons).
- **Text styles:** `TextBlock` classes `largeTitle`, `title1`…`title3`, `headline`, `body`, `callout`,
  `subheadline`, `footnote`, `caption1`, `caption2`, plus `emphasized`, `secondary`, `tertiary`.
- **Resources** are named after the kit: `AccentBrush`, `LabelSecondaryBrush`, `WindowBackgroundBrush`,
  `ControlHeight`…
- **Inactive windows** gray out their controls, as on macOS. Your styles can do the same with
  `[(cas|WindowActivity.IsInactive)=True]` (`xmlns:cas="using:CupertinoAvalonia.Styling"`).
- **Other controls** (pickers, `NumericUpDown`, `TabControl`, `TreeView`, `SplitView`…) keep Fluent's templates,
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

- `ExtendsContentIntoTitleBar`: the content runs under the title bar. Leave room for the window controls, and
  mark the areas that move the window with `WindowDecorationProperties.ElementRole="TitleBar"`.
- `ToolbarStyle`: `Unified` (52 pt) or `UnifiedCompact` (40 pt) title bar, the window controls centered in it.
- `Material`: a system material behind the window (`Sidebar`, `WindowBackground`, `UnderWindowBackground`,
  `HudWindow`), shown wherever the content is transparent.

The Gallery's window ([MainWindow.axaml](https://github.com/Sorion/CupertinoAvalonia/blob/main/samples/CupertinoAvalonia.Gallery/Views/MainWindow.axaml)) is a
complete example: a sidebar on the sidebar material, a toolbar over the content.

**Liquid Glass chrome:** macOS 26+ draws it only for apps whose executable declares its SDK, which .NET's app
host doesn't. The package's `buildTransitive/CupertinoAvalonia.targets` declares it after each build on macOS;
with a project reference, import that file in your app (as the Gallery does). `CupertinoDeclareMacOSSdk=false`
turns it off.

## Good to know

- The switch's and the slider's pressed knobs are real glass lenses (`Styling/GlassLens.cs`, a Skia shader
  inspired by [LiquidGlassAvaloniaUI](https://github.com/KaranocaVe/LiquidGlassAvaloniaUI) and
  [LiquidAvalonia](https://github.com/A1isandr/LiquidAvalonia)). Menus, popovers and sidebars aren't glass yet.
- Focus rings and lenses are drawn in the adorner layer so containers don't clip them:
  [docs/avalonia-clipping.md](https://github.com/Sorion/CupertinoAvalonia/blob/main/docs/avalonia-clipping.md) explains how, for any control.
- The theme adds letter spacing to the system font, which Skia renders tighter than macOS does.

## Design tokens

The theme's colors, metrics and text styles (`Styling/Tokens/*.g.axaml`, `Media/SystemAccentPalette.g.cs`) are
generated from Apple's macOS 27 UI kit for Figma, and included in the library. Apple's kit itself isn't in the
repository. To regenerate the tokens when Apple updates it:

1. Duplicate Apple's [macOS 27 kit](https://developer.apple.com/design/resources/) from the Figma Community, then
   in the Figma desktop app, **File → Save local copy…** to `design/source/macOS-27.fig` (ignored by git).
2. In `tools/figma` (Node.js): `npm install`, then `npm run generate`. It works offline, without a Figma account.


## License

MIT, see [LICENSE](https://github.com/Sorion/CupertinoAvalonia/blob/main/LICENSE) and [THIRD-PARTY-NOTICES.md](https://github.com/Sorion/CupertinoAvalonia/blob/main/THIRD-PARTY-NOTICES.md).
