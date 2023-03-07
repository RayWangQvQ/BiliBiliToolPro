<!--- app-name: bilibili-tool -->

# BiliBili Tool

BiliBiliTool 是一个自动执行任务的工具，当我们忘记做某项任务时，它会像一个贴心小助手，按照我们预先吩咐它的命令，在指定频率、时间范围内帮助我们完成计划的任务。

[Overview of BiliBili Tool](https://github.com/RayWangQvQ/BiliBiliToolPro)

## TL;DR

### 在集群中通过chart部署

```console
$ git clone https://github.com/RayWangQvQ/BiliBiliToolPro.git
$ cd ${local_code_repo}/helm/bilibili-tool
[optional]$ vim values.yaml # provides your own settings like cookies
$ helm install <my_release_name> .
```

如果没有在values.yaml中提供cookie，那么需要手动扫描日志中的二维码进行登录

```console
$kubectl logs -f <pod_name>
```

如果在values.yaml中提供了cookie，那么可以不扫描也可以扫描进行登录，上面的步骤可以暂时不执行

## Introduction

这个chart通过[Helm](https://helm.sh)在[Kubernetes](https://kubernetes.io)集群上拉起一个[BiliBiliToolPro](https://github.com/RayWangQvQ/BiliBiliToolPro)deployment

## Prerequisites

- Kubernetes
- Helm

或者

- Kind
- Helm

## 安装Chart

安装Chart并命名为 `my-release`:

```console
$helm repo add my-repo <my_chart_repo>
$helm install my-release my-repo/bilibili-tool(:1.0.1)
```

上述命令需要用户在values.yaml里提供cookie等必须信息
[Parameters](#parameters) 部分列出了所有可供自定义的值

> **Tip**: `helm list` 可以列出当前已经列出的所有的release

## 卸载 Chart

卸载 `my-release` deployment:

```console
$helm delete my-release
```

上述命令卸载掉所有的release相关资源

## Parameters

| Name                      | Description                                     | Value | Required |
| ------------------------- | ----------------------------------------------- | ----- | -------- |
| `replicaCount`    | Deployment Relicas Count                   | `1`  | true |
| `namespace`    | Deployment and ConfigMap deployed namespace                   | `default`  | true |
| `configmap.name`    | ConfigMap name contains the entry files                   | `entry`  | true |
| `image.repository` | Global Dockevr registry | `zai7lou/bilibili_tool_pro`  | true |
| `image.tag`     | Image Tag    | `1.0.1`  | true |
| `image.pullPolicy`     | Image Pull Policy    | `IfNotPresent`  | true |
| `imagePullSecrets` | Image Pull Secrets | `[]` | false |
| `nameOverride` | Deployment name in the Chart | `""` | false |
| `fullnameOverride` | Release name when set | `""` | false |
| `resources.limits`      | The resources limits for the BiliBili Tool containers                                 | `{}`            | true |
| `resources.limits.memory`                         | The limited memory for the BiliBili Tool containers                                                                                                                                                 | `180Mi`         | true |
| `resources.limits.cpu`                            | The limited cpu for the BiliBili Tool containers     | `100m` | true |
| `resources.requests`      | The resources requests for the BiliBili Tool containers                                                                                                       | `{}`            | true |
| `resources.requests.memory`                         | The requested memory for the BiliBili Tool containers                                                                                                                                                 | `180Mi`         | true |
| `resources.requests.cpu`                            | The requested cpu for the BiliBili Tool containers     | `100m` | true |
| `affinity`                                          | Affinity for pod assignment                                                                                                                                                                       | `{}`            | false |
| `nodeSelector`                                      | Node labels for pod assignment                                                                                                                                                                    | `{}`            | false |
| `tolerations`                                       | Tolerations for pod assignment                                                                                                                                                                    | `[]`            | false |
| `env` | Environment variables for the BiliBili Tool container, Ray_BiliBiliCookies__1 and Ray_DailyTaskConfig__Cron are required, others vars pls take a look at [supported envvars](https://github.com/RayWangQvQ/BiliBiliToolPro/blob/main/docs/configuration.md) | `[]` | true |
| `volumes.log.enabled` | Enable persistent log volume for BiliBili Tool or not | `"false"` | true |
| `volumes.log.path` | The host path mounted into pod | `"/tmp/Logs"` | false |
| `volumes.log.name` | The volume name | `"bili-tool-vol"` | false |
| `volumes.login.enabled` | Enable persistent log volume contains the entries for BiliBili Tool or not | `"false"` | true |
| `volumes.login.name` | The volume name | `"entry"` | false |
| `podAnnotations` | The annotations for the BiliBili Tool pod | `{}` | false |

可以用指定helm install命令行参数 `--set key=value[,key=value]`， 比如

```console
$ helm install my-release \
  --set  \
    relicas=1
```

也可以通过指定一个YAML格式的values文件来配置以上参数，比如

```console
$helm install my-release -f values.yaml my_chart_repo/bilibili-tool
```

> **Tip**: 你可以使用默认的 [values.yaml](bilibili-tool/values.yaml)进行配置

当想更新一些变量时，可以通过指定参数或者直接修改YAML的values文件进行更新

```console
$helm upgrade my-release my_chart_repo/bilibili-tool <-f values> or <--set-file ...> 
```

## Upgrade

建议重新装release

## [Optional]本地Cluster运行

通过安装[kind](https://kind.sigs.k8s.io/docs/user/quick-start/)工具在本地运行一个Kubernetes Cluster

go 1.17+ and Docker installed

```console
$ go install sigs.k8s.io/kind@v0.17.0 && kind create cluster <--config kind_config_file>
$ cat <kind_config_file>
$ kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
- role: worker 
$ EOF
```

at least one worker node otherwise you have to provides tolerations in the values.yaml to schedule on master node
