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

### Web UI layout (MudBlazor)

The Web panel is MudBlazor-native (`Ray.BiliBiliTool.Web`, see ADR-0006). Two traps cost real debugging time:

- **`h-100` does not exist in MudBlazor's CSS.** MudBlazor 9.10 ships only its own utility set; `height:100%` is `mud-height-full`. An unknown class fails silently, so the element just keeps its content height.
- **A flex child that must scroll needs `min-height:0`.** Flex items default to `min-height:auto` and will not shrink below their content, which pushes anything after them out of the viewport instead of scrolling.

Sidebar structure follows from both: `MainLayout` renders `div.app-drawer-body` (flex column) containing `MudNavMenu.app-drawer-nav` (fills + scrolls) and `AppVersionFooter` (pinned). The rules live in `wwwroot/app.css`.

### Docker publish

The image build restores before copying sources (for layer caching) and publishes with `--no-restore`. That restore state has no `.razor` items, so the SDK decides `RequiresAspNetWebAssets=false` and drops `_framework/blazor.web.js` from the publish output — see ADR-0008. `Ray.BiliBiliTool.Web.csproj` pins the property explicitly and the Dockerfile asserts the file exists after `dotnet publish`. Never remove that assertion.
