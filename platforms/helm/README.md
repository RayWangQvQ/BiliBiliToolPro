<!--- app-name: bilibili-tool -->

# BiliBili Tool Helm chart

The chart in [`bilibili-tool/`](./bilibili-tool/) defines a Kubernetes Deployment and a ConfigMap for a legacy console-and-cron container. **Compatibility warning:** its default `zai7lou/bilibili_tool_pro:1.0.1` image, cron entry scripts, and Cookie environment settings predate the current Web image (`bili_tool_web`). The chart has not been updated to deploy the current Web panel; do not substitute the new image and assume the templates still work.

[Project overview](https://github.com/RayWangQvQ/BiliBiliToolPro)

## Quick start (legacy chart)

With a Kubernetes cluster and [Helm](https://helm.sh) configured, inspect and adapt the chart's [values.yaml](bilibili-tool/values.yaml) before use:

```bash
git clone https://github.com/RayWangQvQ/BiliBiliToolPro.git
cd BiliBiliToolPro/platforms/helm/bilibili-tool
helm install my-release .
kubectl logs -f <pod_name>
```

The legacy entry script attempts QR sign-in in the pod on startup. If the legacy image supports the Cookie supplied in `values.yaml`, QR sign-in may not be needed. Verify image availability and behavior before relying on this deployment.

## Prerequisites

- A working Kubernetes cluster (a local [kind](https://kind.sigs.k8s.io/docs/user/quick-start/) cluster also works for chart experiments).
- Helm and `kubectl` configured for that cluster.
- An image compatible with the chart's console/cron entry scripts; the current Web image is **not** a drop-in replacement.

## Install and uninstall

This repository provides a **local chart**, not a configured Helm repository. The former example `helm repo add my-repo <my_chart_repo>` was a placeholder for a separately hosted chart, not a repository provided here.

```bash
helm install my-release ./platforms/helm/bilibili-tool -f ./platforms/helm/bilibili-tool/values.yaml
helm list
helm uninstall my-release
```

Run these commands from the repository root. `helm uninstall` removes the chart-managed resources; hostPath data or other external storage must be managed separately.

## Parameters

The following are the values actually present in [`values.yaml`](bilibili-tool/values.yaml). These are **legacy chart settings**, not recommended values for the current Web container.

| Name | Purpose | Current chart default |
| --- | --- | --- |
| `namespace` | Deployment and ConfigMap namespace | `default` |
| `replicaCount` | Declared replica count (not referenced by the Deployment template) | `1` |
| `configmap.name` | Name of the entry-scripts ConfigMap | `entry` |
| `image.repository` | Legacy container repository | `zai7lou/bilibili_tool_pro` |
| `image.tag` | Container tag (falls back to chart `appVersion`) | `1.0.1` |
| `image.pullPolicy` | Pull policy | `IfNotPresent` |
| `imagePullSecrets` | Image pull secret references | `[]` |
| `nameOverride`, `fullnameOverride` | Template naming overrides | `""`, `""` |
| `resources.limits.cpu`, `resources.limits.memory` | Container limits | `100m`, `120Mi` |
| `resources.requests.cpu`, `resources.requests.memory` | Container requests | `100m`, `120Mi` |
| `affinity`, `nodeSelector`, `tolerations` | Pod scheduling settings | `{}`, `{}`, `[]` |
| `env` | Container environment variables | Legacy `Ray_BiliBiliCookies__1` (empty) and `Ray_DailyTaskConfig__Cron` (`10 8 * * *`) |
| `volumes.log.enabled`, `volumes.log.path`, `volumes.log.name` | Legacy hostPath log mount | `true`, `/tmp/logs`, `bili-tool-vol` |
| `volumes.login.enabled`, `volumes.login.name` | Entry-scripts ConfigMap mount | `true`, `entry` |
| `podAnnotations` | Pod annotations | `{}` |

The template does not reference `replicaCount`; setting it does not change the number of pods. The ConfigMap entry script uses legacy `Ray_*` cron variables and runs `Ray.BiliBiliTool.Console.dll`, so inspect the template before changing images. See the [configuration guide](../../docs/configuration.md) for current application settings.

Override values with a file or `--set`:

```bash
helm install my-release ./platforms/helm/bilibili-tool -f ./platforms/helm/bilibili-tool/values.yaml
helm install my-release ./platforms/helm/bilibili-tool --set namespace=default
helm upgrade my-release ./platforms/helm/bilibili-tool -f ./platforms/helm/bilibili-tool/values.yaml
```

## Upgrade

The earlier guide recommended reinstalling the release. Helm supports `helm upgrade` as shown above, but upgrading this legacy chart does **not** make it compatible with the current Web image. Test changes in a non-production cluster first.

## Optional: local kind cluster

For local experimentation, install Go, Docker, and kind. For example, the earlier guide used:

```bash
go install sigs.k8s.io/kind@v0.17.0
kind create cluster
```

For a multi-node cluster, provide a kind config with `kind: Cluster`, `apiVersion: kind.x-k8s.io/v1alpha4`, and `control-plane` / `worker` nodes. The former example's `cat <kind_config_file>` and `kind create cluster <--config ...>` placeholders were not executable commands. A single control-plane cluster may require scheduling tolerations depending on its configuration.
