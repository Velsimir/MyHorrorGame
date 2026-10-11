# Changelog

## [0.2.0]

### Added
- Dialogue module: `DialogueAsset` graphs, conditions and flags, `DialogueRunner`, `DialoguePlayer` (with `State` / `Restore` for saves), box, subtitle and world-text presenters, `DialogueTrigger`, `DialogueInputAdvance`, the Dialogues tab with a GraphView board (`TYPECRAFT_NO_DIALOGUE_GRAPH` switches to a list view).
- Markup layer: styling lives next to the string table, translators see plain text; stale markup falls back to plain text.
- Table sync with CSV/TSV, `.xlsx` and Google Sheets; export to xlsx, CSV, TSV and JSON; key usage search.
- Custom styles, material presets and effects from the editor.
- Icons sit on the baseline; image checks and per-icon offset and scale.
- Responsive window layout with resizable panes.
- Welcome window and automatic install of TMP Essential Resources, the bundled TMPEffects 0.3.2 and its resources.
- Showcase sample, Manual and API documentation in Russian and English.

### Changed
- `ITextSource`: `BaseLocale` and `Request(table, key, locale, Action<TextEntry>)`.
- `EXTENDING.md` merged into `Documentation~/API.*.md`.

### Fixed
- `<!wait>` and other TMPEffects commands on localized text.

## [0.1.0]

- First version: markup, style and icon libraries, input prompts, font roles, string tables, the Typecraft window.
