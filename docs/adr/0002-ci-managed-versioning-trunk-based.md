# 单主干 + CI 托管版本号：alpha 自动产出，稳定版手动触发

版本号不再由任何文件手工维护（废弃 common.props 的 VersionPrefix 权威地位与 CHANGELOG 一致性校验），改为 CI 从 git tag 历史推导：PR squash 合入唯一主干 main 即自动产出 `<暂定下一patch>-alpha.N` 镜像；稳定版由维护者手动触发发版 workflow、下拉选择 patch/minor/major 后自动算号、打 tag、出 Release 并把 CHANGELOG 段落回写仓库。本地构建版本恒为 `0.0.0-dev`，CI 构建一律显式注入版本号。

## Considered Options

- **保留 Git Flow 双主干（develop/main）**：被否决。对单人/小团队项目，它只带来 develop 落后 main、发版 PR 噪音、PR 目标纠偏 workflow 等负担；"每个 PR 合并产出 alpha"在单主干下就是 main 的每次 push，发稳定版只是给 main 盖章，无需第二分支。
- **Conventional Commits / PR 标签驱动版本升级**：被否决。维护者明确要求：普通 PR 合并只升 alpha，major/minor/patch 由发版时人工决策（下拉选择），不解析提交信息或标签。
- **alpha 版本也出 GitHub Prerelease / Console zip**：被否决。alpha 的消费者是跑 Docker 的尝鲜场景，只推镜像可最小化失败面。

## Consequences

- alpha 基底是"暂定下一 patch"，若最终发版选 minor/major，alpha tag 与稳定版号会错配（如 `4.0.8-alpha.3` → `4.1.0`）——显式接受的预期行为，alpha 用后即弃。
- PR 标题不做 CI 强制校验，release notes 生成器必须对不合规标题兜底（原样收录）。
- 完整实施规格见 issue #1153。
