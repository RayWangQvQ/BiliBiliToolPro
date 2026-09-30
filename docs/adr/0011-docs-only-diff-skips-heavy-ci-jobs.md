# 纯文档变更跳过 CI 门禁的重活（判定放 job 级，不放 paths）

`ci.yml` 与 `codeql-analysis.yml` 各加一个前置 job `Scope`：枚举本次 PR 的全部变更文件，若每一个都命中文档白名单（`**.md`、`docs/**`、`.editorconfig`、`bruno/**`），就输出 `docs_only=true`，`Verify` / `ScriptTests` / `ImageSmoke` / `Analyze` 四个重活 job 随之跳过。被条件跳过的 job 报 Success，PR 直接可合。

> **已被 ADR-0012 部分取代**：`codeql-analysis.yml` 已并入 `ci.yml`，`CodeQLScope` 取消，文档白名单只剩一份。下文凡涉及「两个 workflow 各一份清单」的描述都已成为历史。
> **命名变更**：以下沿用当时的文件名和 job 名；当前 `pr-checks.yml` 的四个 required checks 已重命名为 `Build and test .NET` / `Test release scripts` / `Build Docker image` / `Scan code with CodeQL`，前置 job 为 `Classify changed files`；原 `alpha.yml` 现为 `publish-alpha-docker-images.yml`。

## 症状与代价

只改 `CHANGELOG.md`（+5 行）的 PR #1186 触发了 CI 与 CodeQL 两整套运行：`ScriptTests` 6s、`Verify` 78s、`ImageSmoke` 85s、CodeQL `Analyze` 264s，合计约 5 分钟构建机时间，全部花在一行 markdown 上。

## 为什么判定必须放 job 级

GitHub 只有两种过滤时机，语义正好相反：

- **workflow 级**（`on.pull_request.paths` / `paths-ignore`）：调度前静态求值。被它跳过的 workflow **不产生任何检查**，required checks 就永远停在 `Expected`，PR 从此无法合并。官方文档明确警告：不要用路径过滤去跳过"合并前必须通过"的 workflow。
- **job 级**（job 上的 `if`）：在 runner 上求值。被条件跳过的 job **报 Success**，即使它是 required check 也不阻塞合并。

`main` ruleset 把 `Verify` / `ScriptTests` / `ImageSmoke` / `Analyze` 四项都列为 required，所以只有 job 级这条路走得通。`alpha.yml` 可以用 workflow 级 `paths-ignore`，是因为它挂在 `push` 上、不承担 required check。

## 考虑的方案

- **给 CI / CodeQL 加 workflow 级 `paths-ignore`（否决）**：改动最小，但检查会停在 Pending，只能靠 admin bypass 合并——等于把"文档 PR 免检"做成"文档 PR 走特殊通道"，每个文档 PR 都得有人记得绕。
- **换 PAT / GitHub App token 建 changelog PR（本轮否决）**：能消掉 bot PR 的「需要审批」停顿，但治不了"纯文档 diff 跑满 5 分钟"，还引入长期凭据。单独评估。
- **抽 `scripts/changed-scope.sh` + fixture 测试（否决）**：与仓库既有的脚本 + fixture 风格一致，但只有两个 workflow 需要这份清单，抽出来多一层间接；fixture 也测不到 GitHub 的 `if` 语义。
- **各 workflow 内联几行 bash（采纳）**：清单在两处各存一份，靠注释互指。判定本身只有 `git diff` + `grep -E` 两行。

## 后果

- **这四个 required check 实际上不再是"必需"**：任何只改 markdown 的 PR 都零校验全绿合入。这是有意换取的代价——这些检查本来也验证不了 markdown。
- **fail-open**：`docs_only` 默认 `false`，只有"成功枚举出文件列表且全部命中白名单"才置 `true`。git 报错、拿不到 base commit、空 diff 一律走全量。宁可多花 5 分钟，不可静默零校验。
- **白名单与 `alpha.yml` 的 `paths-ignore` 对齐，只少一项 `.github/workflows/**`**：改 workflow 的 PR 仍要跑 CI 自检——`pull_request` 用的是 PR 分支上的 workflow 文件，所以那确实是在验证新 workflow 本身。两处清单要一起改。
- **`Scope` 不是 required check**，也不要把它加进 ruleset：它一旦 required，fail-open 就没意义了。
- 判定命令是 `git diff --no-renames --name-only "$BASE...$HEAD"`，两个 SHA 取 `github.event.pull_request.base.sha` / `.head.sha`，`Scope` job 需要 `fetch-depth: 0`。
- `Scope` job 会把变更文件清单与结论 echo 到日志里。这不是额外的可观测性机制，只是脚本的正常输出——出问题时回 Actions 页能看到判定依据。

## 没有解决的部分

PR #1186 的 84 分钟墙钟时间不是这次改动治的：bot 用 `GITHUB_TOKEN` 创建的 PR，其 `pull_request` 运行会以**「需要审批」状态**创建（GitHub 防递归规则），必须有写权限的人点 Approve 才开跑。本轮保留该行为——维护者点一次 Approve，之后文档 diff 几乎瞬间全绿。
