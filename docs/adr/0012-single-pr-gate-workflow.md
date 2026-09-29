# PR 门禁合并成单一 workflow

`codeql-analysis.yml` 删除，其 `Analyze` job 搬进 `ci.yml`，与 `Verify` / `ScriptTests` / `ImageSmoke` 共用同一个 `Scope` 判定；原 `CodeQLScope` 随之取消。`Analyze` 的 **job 名保持不变**——`main` ruleset 的必需检查认的是 job 名，与 workflow 文件名无关，所以本次 ruleset 不用动。

## 为什么可以合

ruleset 只认检查名（`Verify` / `ScriptTests` / `ImageSmoke` / `Analyze`），一个 workflow 里放几个 job 它不关心。当初拆开换来三样东西，合并后各自的去向：

- **权限最小化**：由 job 级 `permissions` 保住——`Analyze` 单独声明 `security-events: write`，其余 job 保持 `contents: read`，写权限不会外溢。
- **上游生态形态**：Security → Code scanning 认的是「有 workflow 使用 `github/codeql-action`」，不要求独立文件，合并后仍是 advanced setup。
- **生命周期独立**：放弃。`concurrency`、`env`、`permissions` 这些 workflow 级设置现在只有一份。

换来的是最大的收益：**文档白名单只剩一份**（ADR-0011 曾要求 `Scope` 与 `CodeQLScope` 两处同步维护）。

## 明确的代价

- **CodeQL 分析类别（category）重置**：`category` 由 workflow 文件名 + **job 名**推导（文档写的是「动作名」，实测是 job 名）。实测值从 `.github/workflows/codeql-analysis.yml:Analyze` 变成 `.github/workflows/ci.yml:Analyze`（合并时在本 PR 的 `code-scanning/analyses` 里可见）。对 code scanning 而言这是另一条分析线：Security 页面的历史告警基线不再连续，已 dismissed 的告警不会被重开，但会作为新告警重新出现。有意接受——不值得为此长期维护一个手写字符串。
- **CodeQL 与 CI 共用一个 concurrency group**：改 PR 时 `Analyze` 会和其它 job 一起被取消（原先两者各有一个 group）。这是想要的行为：一次取消就是整轮取消。
- **手动补跑只需 dispatch `CI` 一个 workflow**（原先要分别补 CI 与 CodeQL）。

## 不做

- 不显式设置 `analyze` 的 `category`。
- 不改 ruleset，不动 `alpha.yml`、`stable.yml`。
- fork PR 上 `security-events: write` 不可用会让 `Analyze` 失败——这是合并前就有的行为，与本决策无关。
