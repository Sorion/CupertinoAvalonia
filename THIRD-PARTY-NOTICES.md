# Third-party notices

## Avalonia

Several control templates are adapted from Avalonia's Fluent theme (`Avalonia.Themes.Fluent`), and the theme
includes `FluentTheme` as a fallback for the controls it doesn't restyle yet.

```
The MIT License (MIT)

Copyright (c) AvaloniaUI OÜ
All Rights Reserved

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Apple

The theme's design tokens (colors, metrics, text styles) are generated on each developer's machine from that
developer's own copy of Apple's macOS 27 UI kit for Figma, which is subject to Apple's "License Agreement for
Apple Design Resources". The kit, and the files extracted or generated from it, are not distributed with this
project. As an exception, a limited number of control colors taken from the kit are written directly in the
control themes; each names the kit component it comes from.

Apple's fonts (SF Pro) and symbols (SF Symbols) are not distributed either: the theme uses the system font
installed with macOS, and draws its glyphs (checkmarks, chevrons) as vector paths.
