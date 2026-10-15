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
