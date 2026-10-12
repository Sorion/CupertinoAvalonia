# CupertinoAvalonia

A macOS 27 theme for [Avalonia](https://avaloniaui.net)'s built-in controls, plus `CupertinoWindow`, a window
with macOS's chrome. Inspired by [FluentAvalonia](https://github.com/amwx/FluentAvalonia). Status: early.

> **Apple's UI kit isn't included.** The theme's colors, metrics and text styles are generated from **your own
> copy** of Apple's macOS 27 UI kit, by the extractor in `tools/figma`, which you run yourself. The kit's license
> (on its *License* page) forbids redistributing its content: never commit or share the generated files.

## Getting started

**1. Generate the design tokens** (once, and again when Apple updates the kit):

1. Duplicate Apple's [macOS 27 kit](https://developer.apple.com/design/resources/) from the Figma Community, then
   in the Figma desktop app, **File → Save local copy…** to `design/source/macOS-27.fig`.
2. In `tools/figma` (Node.js): `npm install`, then `npm run generate`. It works offline, without a Figma account.

The build stops with a message until the tokens exist. Git ignores them, and `design/`.

**2. Use the theme** instead of `FluentTheme` in `App.axaml`:

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

The Gallery's window ([MainWindow.axaml](samples/CupertinoAvalonia.Gallery/Views/MainWindow.axaml)) is a
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
  [docs/avalonia-clipping.md](docs/avalonia-clipping.md) explains how, for any control.
- The theme adds letter spacing to the system font, which Skia renders tighter than macOS does.

## Contributing

```
src/CupertinoAvalonia/
  Styling/ControlThemes/   one file per control
  Styling/Tokens/          generated tokens (local only, don't edit)
  Controls/                CupertinoWindow
samples/CupertinoAvalonia.Gallery/
tests/CupertinoAvalonia.Tests/
tools/figma/               the extractor
```

- Values come from the generated tokens. A color copied from the kit into a theme is a rare exception: name its
  kit component in a comment.
- `npm run inspect -- "Buttons" "State=Idle"` (in `tools/figma`) prints a kit component's layers and colors.
- `dotnet test` runs the headless tests. To render the Gallery's pages to PNG:
  `GALLERY_SCREENSHOTS=/tmp/gallery dotnet test tests/CupertinoAvalonia.Tests`.

## License

MIT, see [LICENSE](LICENSE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
