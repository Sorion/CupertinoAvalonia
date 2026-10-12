# Drawing beyond a control's bounds in Avalonia

> **Written:** October 11, 2026, for Avalonia 12.0.5. The behaviors described were checked in Avalonia's sources (`src/Avalonia.Controls`) or by this repository's tests; each statement says which.
> **For:** anyone writing an Avalonia control or theme that draws outside its bounds: focus ring, shadow, enlarged press effect, overflowing badge…

## Contents

1. [The problem](#1-the-problem)
2. [What clips?](#2-what-clips)
3. [Why `ClipToBounds="False"` isn't enough](#3-why-cliptoboundsfalse-isnt-enough)
4. [The solutions](#4-the-solutions)
5. [Recipe: an adorner that overflows](#5-recipe-an-adorner-that-overflows)
6. [Related pitfalls](#6-related-pitfalls)
7. [Checking with a test](#7-checking-with-a-test)
8. [In this repository](#8-in-this-repository)

## 1. The problem

A control can only draw within the rectangle layout gives it (its `Bounds`) **as soon as one of its ancestors clips**. Anything outside that rectangle is cut off:

- a shadow (`BoxShadow`) or a focus ring drawn around the control;
- an element enlarged by a `RenderTransform` (`scale(1.2)`): a render transform doesn't change layout bounds;
- a child placed with a negative margin.

The typical symptom: the effect looks right in the middle of a page, but is cut off as soon as the control touches the edge of its container, or cut off all around when the control itself is what clips.

## 2. What clips?

`ClipToBounds` is `false` on `Control`, but several classes change that default (checked in the sources):

| Class | Default `ClipToBounds` | Consequence |
|---|---|---|
| `Control`, `Panel`, `Border`, `Decorator` | `false` | Don't clip. |
| `Canvas` | `false` (set explicitly) | Doesn't clip. |
| **`TemplatedControl`** | **`true`** | **Every templated control clips:** `ContentControl`, `UserControl`, `Button`, `ListBoxItem`, `ItemsControl`, `HeaderedContentControl`, `DataValidationErrors`… |
| `ScrollContentPresenter` | `true` | A `ScrollViewer`'s viewport clips, which is intended. |
| `LayoutTransformControl` | `true` | Clips its transformed child. |

Two other limits exist:

- **Popup**: a `Popup`'s content (menus, tooltips, flyouts) is shown in its own window, sized to that content (or in the window's `OverlayLayer` when popups are overlays). Whatever overflows the content, such as a shadow, is cut off by that window.
- **Window**: nothing gets out of the window.

## 3. Why `ClipToBounds="False"` isn't enough

Setting `ClipToBounds="False"` on your own control only stops **its** clipping. Every ancestor that clips cuts in turn, and in a real app there almost always is one:

- **a `UserControl`** (the view itself) when the control touches its edge;
- **a `ListBoxItem`** or a `ContentControl` around it;
- **a `DataValidationErrors` in the control's own template.** This is the most common trap: Fluent's templates for `TextBox`, `ComboBox`, `Slider`, `NumericUpDown` and `CalendarDatePicker` are wrapped in a `DataValidationErrors`. Since it is a `ContentControl`, it clips to the field's bounds, and a focus ring drawn in that template is cut off all around, whatever the container.

A library doesn't control the containers its controls will be placed in. So it has to draw somewhere other than the control's tree.

## 4. The solutions

From the simplest to the most robust.

### A. Make room

Enlarge the control (a `Padding` or a margin in its template) so the effect fits within its bounds.

- **For:** trivial, no special mechanism.
- **Against:** the control takes more room than it looks. It no longer lines up with its neighbors, and the margin is always there, even when the effect isn't shown.
- **When:** for a control inside a view you own, or a very small effect.

### B. Turn off clipping on the ancestors you own

`ClipToBounds="False"` on the control and on every container of its template that clips (`DataValidationErrors` included).

- **For:** the effect stays in the control's tree: styles, `TemplateBinding` and animations work as usual.
- **Against:** does nothing about the containers the app puts it in.
- **When:** in an app, for its own views. Not in a library.

### C. The adorner layer (`AdornerLayer`)

Every window (and every popup) has an `AdornerLayer`, drawn above the content. An element set as a control's `AdornerLayer.Adorner` is hosted there and follows that control.

How it works, from `AdornerLayer`'s code:

- **Placement:** the adorner is arranged at the size of the adorned element. The layer gives it, as `RenderTransform`, the transform from that element to itself, and moves it again when the element or one of its ancestors moves.
- **`AdornerLayer.IsClipEnabled` (`true` by default):** the adorner is clipped like the adorned element, by the intersection of the clips of **all** its ancestors (`AdornerHelper.CalculateAdornerClip`). That is exactly the problem again.
- **With `IsClipEnabled="False"`:** no clipping at all, not even by a `ScrollViewer`. The adorner can then float over the rest of the UI while scrolling.

This is the robust solution for a library. [Section 5](#5-recipe-an-adorner-that-overflows) gives the recipe and its traps.

### D. The built-in `FocusAdorner`

`Control.FocusAdorner` is Avalonia's mechanism for the keyboard focus indicator. From `Control.OnGotFocus`'s code:
- it only shows for focus gained from the keyboard (`NavigationMethod.Tab` or `Directional`), never for focus from the mouse;
- the adorner surrounds the template element marked `TemplatedControl.IsTemplateFocusTarget`, or the whole control;
- it goes through the same `AdornerLayer`, so the same `IsClipEnabled` rule applies.

It fits an indicator that surrounds the whole control, for the keyboard only. It doesn't fit a ring around part of a control (a `CheckBox`'s box rather than its label), nor a text field that shows its ring for the mouse too.

### E. A popup

For large floating content (menu, palette, preview), a `Popup` can leave the window. Whatever overflows **inside** the popup, such as its shadow, needs a transparent margin around the content, since the popup's window is sized to the content.

### Choosing

| Need | Solution |
|---|---|
| A one-off effect in one of your own views | B (or A) |
| Focus ring, shadow or press effect in a library | C |
| Keyboard focus indicator around the whole control | D, with `IsClipEnabled="False"` |
| Floating content that may leave the window | E |

## 5. Recipe: an adorner that overflows

### The pattern

A style creates the adorner only while it must show (focus, press), and removes it afterwards:

```xml
<Style Selector="^:focus-visible /template/ Border#PART_Background">
  <Setter Property="AdornerLayer.Adorner">
    <Template>
      <!-- The adorner's root: the layer places and transforms it. -->
      <Panel AdornerLayer.IsClipEnabled="False" IsHitTestVisible="False">
        <!-- The effect itself, on a child. -->
        <Border CornerRadius="6" BoxShadow="0 0 0 3.5 #800088FF" />
      </Panel>
    </Template>
  </Setter>
</Style>
```

### The rules

1. **Create the adorner from a style rather than in the template.** An element declared in `<AdornerLayer.Adorner>` inside a `ControlTemplate` is created and hosted in the layer, but its `TemplatedParent` is empty (seen in a test). The theme's `/template/` styles don't reach it, and a `TemplateBinding` has no source. Created by a style, the adorner no longer depends on the template: give it fixed values or `DynamicResource`s.
2. **`<Template>` in the `Setter`.** With `<Template>`, Avalonia builds a new adorner for each targeted control. A direct value would be a single shared instance.
3. **`AdornerLayer.IsClipEnabled="False"` as an attribute, on the root.** From the layer's code, this setting is read when the adorner is added. Setting it in the adorner's XAML guarantees it is already in place.
4. **Nothing of your own on the root.** The layer replaces the root's `RenderTransform`, and resets its `Clip` to `null` on every arrange when clipping is off (seen in `AdornerLayer.ArrangeOverride`). Transforms, clipping and margins go on a child.
5. **Size and position:** the child takes the adorned element's size. To make it larger, either:
   - a negative margin on the child;
   - a centered scale (`RenderTransformOrigin` is the center by default), handy when the size ratio is constant.
6. **`IsHitTestVisible="False"`** on a purely visual adorner, so it doesn't catch the mouse.
7. **Clip to scroll viewports.** A focus ring stays visible for a long time: unclipped, it spills over the UI while scrolling. `OverflowAdorner.FollowAdorned` (see [section 8](#8-in-this-repository)) clips the child to the `ScrollContentPresenter`s around the adorned element only. A brief effect, such as during a press, can do without.
8. **Cost:** every adorner is arranged again with the layer, and listens to property changes on all the adorned element's ancestors. Better to create them on demand, as here, than to keep one per control at all times.

### Animating the appearance

An adorner created by a style has no "previous" value: a `Transition` doesn't run when it is created. Use a style animation instead, which starts when the element appears. Two traps:

- **A keyframe animation can't animate `RenderTransform`.** It fails with "No animator registered for the property RenderTransform" (seen). Animate `ScaleTransform.ScaleX` and `ScaleY` (or `TranslateTransform.X`) instead, and let the animation create the transform. `TransformOperationsTransition`, on the other hand, can animate `RenderTransform`.
- **A bounce easing (`BackEaseOut`) overshoots the final value.** That suits a scale, not an opacity, which would go over 1. So the opacity goes in a second `Animation`, without bounce.

```xml
<Style Selector="^">
  <Style.Animations>
    <Animation Duration="0:0:0.2" Easing="BackEaseOut" FillMode="Forward">
      <KeyFrame Cue="0%"><Setter Property="ScaleTransform.ScaleX" Value="1" /><Setter Property="ScaleTransform.ScaleY" Value="1" /></KeyFrame>
      <KeyFrame Cue="100%"><Setter Property="ScaleTransform.ScaleX" Value="1.55" /><Setter Property="ScaleTransform.ScaleY" Value="1.55" /></KeyFrame>
    </Animation>
    <Animation Duration="0:0:0.12" FillMode="Forward">
      <KeyFrame Cue="0%"><Setter Property="Opacity" Value="0" /></KeyFrame>
      <KeyFrame Cue="100%"><Setter Property="Opacity" Value="1" /></KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

## 6. Related pitfalls

- **Template value versus style.** A property set as an attribute in a template has `Template` priority. It beats an unconditional style (`Style` priority), but not a style triggered by a class or a pseudo-class (`StyleTrigger` priority, seen in `BindingPriority`). Example: `Background="Transparent"` in the template, then `<Style Selector="^ /template/ Border#X">` changing the background. The style is ignored, while the same under `^:checked` works. A value an unconditional style must be able to change is set by a style, not in the template.
- **Popup shadows.** A popup's window takes the size of its content: without a transparent margin around the frame, its shadow is cut off.
- **`BoxShadow` offsets under a transform.** Avalonia applies a shadow's offset (`OffsetX`, `OffsetY`) in the window's space, without visual transforms (`RenderTransform`, `LayoutTransformControl`). Blur and spread do follow the scale. A `0 5 6 0` shadow under a `ScaleTransform` of 2 therefore has twice the blur but an offset of only 5 DIPs. This is the case of the switch's lens, enlarged 1.62 times: its offsets are given in screen DIPs.
- **`TransformToVisual` and `LayoutTransformControl`.** In our tests, the transform computed between an element of the adorner layer and an element under a `LayoutTransformControl` gave a wrong position. Measuring both in the window's space was right. Be careful with position calculations between the two trees.

## 7. Checking with a test

Avalonia's headless tests (`Avalonia.Headless.XUnit`) are enough:

1. Put the control **against the edge** of a container that clips (a `UserControl`), in a window.
2. Trigger the state: `control.Focus(NavigationMethod.Tab)` for keyboard focus, or `window.MouseDown(point, MouseButton.Left)` for a held press.
3. Check that the adorner exists (`AdornerLayer.GetAdorner(element)`) and that its clipping is off (`AdornerLayer.GetIsClipEnabled`).
4. To look at the result, `window.CaptureRenderedFrame()` goes through the real composition engine.

Animations and transitions run on the real clock. To wait for them to finish, loop on `Thread.Sleep(10)` + `AvaloniaHeadlessPlatform.ForceRenderTimerTick()` + `Dispatcher.UIThread.RunJobs()`, with a timeout.

## 8. In this repository

- **[`Styling/OverflowAdorner.cs`](../src/CupertinoAvalonia/Styling/OverflowAdorner.cs)**: the `FollowAdorned` attached property, on a child of the adorner. It clips that child to the adorned element's scroll viewports, and copies its `CornerRadius` if it is a `Border`. It is a self-contained hundred lines or so, and can be reused in another project.
- **Focus rings:** the `AdornerLayer.Adorner` style in [`Button.axaml`](../src/CupertinoAvalonia/Styling/ControlThemes/Button.axaml), [`CheckBox.axaml`](../src/CupertinoAvalonia/Styling/ControlThemes/CheckBox.axaml), [`TextBox.axaml`](../src/CupertinoAvalonia/Styling/ControlThemes/TextBox.axaml)…
- **Animated press effect:** the lenses of [`ToggleSwitch.axaml`](../src/CupertinoAvalonia/Styling/ControlThemes/ToggleSwitch.axaml) (`CupertinoSwitchLens`) and [`Slider.axaml`](../src/CupertinoAvalonia/Styling/ControlThemes/Slider.axaml) (`CupertinoSliderLens`).
- **Tests:**
  - `Focus_ring_is_not_clipped_by_containers` in [`ControlRenderTests.cs`](../tests/CupertinoAvalonia.Tests/ControlRenderTests.cs);
  - [`ToggleSwitchTests.cs`](../tests/CupertinoAvalonia.Tests/ToggleSwitchTests.cs).
