# 镜像构建跑在宿主机架构上，不用 QEMU 模拟 arm64

alpha 镜像从 30 分钟降到个位数分钟的关键决定：Dockerfile 的编译阶段用 `FROM --platform=$BUILDPLATFORM` 固定在宿主机架构（CI 上是 amd64 原生）执行，`platforms` 仍然同时产出 amd64/arm64 两个镜像；final 层不保留任何 `RUN`，因此连 `setup-qemu-action` 都不再需要。实测依据：run 36380404256 里 arm64 侧 restore 239s / build 497s / publish 732s，合计 24.6 分钟，同一份代码在 amd64 原生上只要 2.9 分钟，QEMU 惩罚约 10 倍。

## 为什么产物可以这么换

RID-less `dotnet publish` 的产物是架构中立的 IL：NuGet 会把全平台的 native 资源一并打进产物（已验证 `runtimes/linux-arm64/native/libe_sqlite3.so` 与 `runtimes/linux-x64/native/libe_sqlite3.so` 同时存在于本地 `bin/Release/net10.0/`）。也就是说，现在 arm64 镜像里装的并不是「arm64 专门编译出来的东西」，而是一份任何架构都能加载的中立产物。既然产物与编译机架构无关，在哪台机器上编译就只是速度问题。

反直觉但已确认的一点：**不需要给 publish 加 `-r linux-$TARGETARCH`**。加了反而要连带把 restore 层也做成 RID 专属、引入「native 资源选错」这一整类失败面，而收益为零——中立产物本来就把 arm64 的 native 带上了。

## 考虑的方案

- **继续用 QEMU 模拟 arm64**（原状）：被否决。占总时长 80%，且 `platforms` 里每加一个架构就再翻一倍。
- **`-r linux-$TARGETARCH` 交叉发布**：被否决，理由见上。
- **改用 ARM 原生 runner（`ubuntu-*-arm`）分 job 构建再 `imagetools` 合并**：被否决。同样能摆脱模拟，但要把一个 build-push 拆成两个 job + 一次 manifest 合并，workflow 复杂度明显上升；`--platform=$BUILDPLATFORM` 一行就能拿到同样的效果。
- **alpha 只出 amd64，arm64 留给稳定版**：被否决。交叉编译后 arm64 几乎免费，没必要牺牲一个使用中的通道。

## 后果

- **arm64 产物不再有真机验证**：既不模拟编译、CI 也不做 arm64 冒烟（合并后靠 alpha 实跑观察，出问题 revert）。这是本次明确接受的风险——产物内容是架构中立的 IL，风险集中在「运行时能否加载 native」，而非「编译能否通过」。
- **final 层不能再加 `RUN`**：一旦有人加了（哪怕只是 `chmod`），就会掉回目标架构执行。届时要么把它挪到编译阶段，要么把 `setup-qemu-action` 装回来。
- 原先 final 层的 `rm -rf /var/lib/apt/lists/*` 随之删除，仅为换「final 零 RUN」，无功能影响。
- `dotnet build` 步骤一并删除：它和 `dotnet publish` 的输出目录不同（`/app/build` vs `/app/publish`），会让 MSBuild 增量判断失效、整套方案重编两遍。
- Dockerfile 是 alpha 与稳定版共用，本决定对两个通道同时生效。
- 本 ADR 只负责「在哪台机器上编译」。缓存存放地（GHA Actions Cache → GHCR registry）是同一个 run 里的另一处改动，动机是 GHA 缓存导出固定 5 分钟，不在此 ADR 范围内。
