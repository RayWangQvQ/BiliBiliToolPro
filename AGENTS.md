## Agent skills

### Issue tracker

Issues and specs live as GitHub issues in this repo; skills use the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-label vocabulary from mattpocock/skills (`needs-triage` / `needs-info` / `ready-for-agent` / `ready-for-human` / `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: one `CONTEXT.md` at the repo root plus `docs/adr/` (ADRs). See `docs/agents/domain.md`.

### Commit messages

Never put the literal string `[skip ci]` (or any CI-skip marker) in a commit message or PR title/body: squash-merging it to `main` silently skips the alpha image build for that push. The changelog bot does not need skip markers — commits made by `GITHUB_TOKEN` never trigger workflows anyway.

### Language conventions

- **Code comments**: keep them concise; write in **English**.
- **Git commits**: use **English** commit messages by default.
- **PR titles/bodies**: use **English** by default.
