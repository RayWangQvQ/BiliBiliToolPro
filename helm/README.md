# Chart 使用说明
<!-- TOC depthFrom:2 -->

- [1. 前期工作](#1-前期工作)
    - [1.1. 环境准备](#11-环境准备)
- [2. 方式：Chart 安装](#2-方式-Chart安装)
    - [2.1. 启动](#21-启动)

<!-- /TOC -->
## 1. 前期工作

### 1.1. 环境准备

请确认已安装了Kubernetes所需环境（[Kubernetes](https://kubernetes/kubernetes.io))

请确认已具有了健康运行的Kubernetes集群，或者也可以使用kind([kind](https://kind.sigs.k8s.io/))来本地运行

请确认已安装了Helm工具([Helm](https://helm.sh/))

## 2. 方式：Chart 安装

### 2.1. 启动

```
# 进入目录
cd helm

# 修改Chart values文件

修改values.yaml，填写env下cookie以及定时触发cron表达式，其他的环境变量设置可以按需设置

helm install <your_release_name> .

如果您想先检查一下生成的资源文件以及是否可以部署在集群中

helm install <your_release_name> . --dry-run > workload.yaml

kubectl apply -f workload.yaml --dry-run=server

确认无误后

helm install <your_release_name> .

# 查看启动日志
kubectl logs <your-bilibilitool-pod-name> -f
```

