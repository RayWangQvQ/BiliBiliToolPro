# 下载程序包到本地或服务器运行

<!-- TOC depthFrom:2 -->

- [1. 下载应用文件](#1-下载应用文件)
- [2. 运行](#2-运行)

<!-- /TOC -->

如果是 DotNet 开发者，直接 Clone 源码，然后 VS 打开解决方案，配置 Cookie 后即可直接本地进行运行和调试。

对于不是开发者的朋友，可以通过下载 Release 包到本地或任意服务器运行，步骤如下。
<details>

## 1. 下载应用文件

点击 [BiliBiliTool/release](https://github.com/RayWangQvQ/BiliBiliToolPro/releases)，下载已发布的最新版本。

* 如果本地已安装 `.NET 6.0` 环境：

请下载 `net-dependent.zip` 文件，本文件依赖本地运行库（runtime-dependent），所以文件包非常小（不到1M）。

P.S.这里的运行环境指的是 `.NET Runtime 6.0.0` ，安装方法可详见 [常见问题](docs/questions.md) 中的 **本地或服务器如何安装.net环境**

* 如果不希望安装或不知如何安装.net运行环境：

请根据操作系统下载对应的 zip 文件，此文件已自包含（self-contained）运行环境，但相较不包含运行时的文件略大（20M 左右，Github 服务器在国外，下载可能比较慢）。

如，Windows系统请下载 `win-x86-x64.zip` ，其他以此类推。

## 2. 运行

下载并解压zip文件。

* Windows 系统

对于已安装.net环境，且使用的是依赖包，可在当前目录下执行命令：`dotnet Ray.BiliBiliTool.Console.dll --runTasks=Login`，或者直接双击运行名称为 start.bat 的批处理文件，均可运行。

对于使用自包含运行环境版本的，可直接双击运行名称为 Ray.BiliBiliTool.Console.exe 的可执行文件。

* Linux 系统

对于已安装.net环境，且使用的是依赖包，同上，可在终端中执行命令：`dotnet Ray.BiliBiliTool.Console.dll --runTasks=Login`

对于使用独立包的，可在终端中执行命令：

```
chmod +x ./Ray.BiliBiliTool.Console
Ray.BiliBiliTool.Console
```

其他系统依此类推，运行结果图示如下：

![运行图示](docs/imgs/run-exe.png)

除了修改配置文件，也可以通过添加环境变量或在启动命令后附加参数来实现配置，详细方法可参考下面的**配置说明**章节。

</details>
