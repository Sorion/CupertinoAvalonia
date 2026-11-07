# Contributing

## Commit messages

Commits follow [Conventional Commits](https://www.conventionalcommits.org), so the changelog can be generated from them:

```
type(scope): summary
```

- **type:** `feat` (a new feature), `fix` (a bug fix), `docs`, `test`, `refactor` (no visible change), `perf`,
  `chore` (maintenance, the Gallery) or `ci`.
- **scope** (optional): the area changed, such as `window`, `toolbar`, `symbols`, `stepper`, `treeview`, `splitview`,
  `sidebar`, `tokens` or `gallery`.
- **summary:** in English, in the imperative, lowercase, without a final period, 72 characters at most.
- A body can list the details, one per line.
- A breaking change adds `!` after the type or scope (`feat(toolbar)!: …`) and describes the change in a
  `BREAKING CHANGE:` footer.

For example: `feat(stepper): style NumericUpDown and ButtonSpinner as steppers`.

## Design tokens

The theme's colors, metrics and text styles (`Styling/Tokens/*.g.axaml`) are generated from Apple's macOS 27 UI kit
for Figma. The kit drifts from native apps, so values measured on macOS 27 replace the kit's where they differ: the
`measured` and `measuredColors` tables in `tools/figma/generate-tokens.js`, each marked in the output with the kit's
value. Apple's kit isn't in the repository.

To regenerate the tokens, after a change to those tables or a new kit:

1. Duplicate Apple's [macOS 27 kit](https://developer.apple.com/design/resources/) from the Figma Community, then
   in the Figma desktop app, **File → Save local copy…** to `design/source/macOS-27.fig` (ignored by git).
2. In `tools/figma` (Node.js): `npm install`, then `npm run generate`. It works offline, without a Figma account.

Controls themselves follow native apps rather than the kit: measure the control in an AppKit or SwiftUI app and say so
in a comment.
