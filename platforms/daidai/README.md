# Run with Daidai Panel

[Daidai Panel](https://github.com/linzixuanzz/daidai-panel) is a scheduled-task panel built with Go, Vue, and SQLite. This integration uses its Git subscription to pull the source, register cron tasks, and run them with either .NET or a `bilitool` release binary. It follows the same task-script pattern as the [Qinglong](../qinglong/README.md) and [Baihu](../baihu/README.md) integrations.

## How the scripts are shared

The task-specific `bili_task_*.sh` scripts simply source `bili_task_base.sh` and call `run_task`. Daidai keeps its own [base script](./DefaultTasks/bili_task_base.sh) for installing and locating the runtime. The subscription hook [`copyshfile.sh`](./copyshfile.sh) copies the other scripts from `platforms/qinglong/DefaultTasks` after the pull and before task registration, then removes the Qinglong directory from that synced checkout to prevent duplicate registrations. The base locates the repository by looking upward for `Ray.BiliBiliTool.sln`.

This procedure targets **Linux container-based Daidai installations** (including Docker or Magisk module deployments) with `bash`; it does not cover the Windows standalone version.

## Prerequisites

- A working Daidai Panel with access to GitHub for the repository and runtime downloads.
- Shell-script subscriptions and subscription hooks enabled in the panel. No `RepoFileExtensions` change is needed as in Qinglong.
- For slow downloads, see [GitHub proxies](#github-proxies).

## Setup

### 1. Create an Open API application (optional)

For automatic Cookie updates, go to **System Settings → Open API** and create an application:

```text
Name: bilitool
Scopes: envs (or * for all scopes)
Rate limit: blank or 0
```

Copy the **AppKey** and **AppSecret** shown on creation/reset; keep them private. Skip this step if you plan to add the Cookie manually after sign-in.

### 2. Set environment variables

In **Environment Variables**, add these for automatic Cookie persistence (note the double underscores):

| Name | Value |
| --- | --- |
| `DaiDaiConfig__AppKey` | Your application's AppKey |
| `DaiDaiConfig__AppSecret` | Your application's AppSecret |

Optional settings:

| Name | Purpose |
| --- | --- |
| `DaiDai_URL` | Panel URL reachable from the task container; application default is `http://127.0.0.1:5700`. Set an actual reachable protocol, host, and port if different (for example `http://192.168.1.10:5700`); omit any path. |
| `BILI_MODE` | `dotnet` (default) or `bilitool`; see [Runtime modes](#runtime-modes). |
| `BILI_GITHUB_PROXY` | Optional GitHub proxy prefix for `bilitool` binary downloads, such as `https://gh-proxy.com/`. |

For multiple Bilibili accounts, the login task creates or updates `Ray_BiliBiliCookies__0`, `Ray_BiliBiliCookies__1`, etc. when API access works; otherwise add them manually.

### 3. Add the subscription

Under **Subscription Management**, create:

```text
Name: Bilibili
Type: Git repository (public)
URL: https://github.com/RayWangQvQ/BiliBiliToolPro.git
Branch: main
Schedule type: crontab
Schedule: 2 2 28 * *
Hook script: bash platforms/daidai/copyshfile.sh
Whitelist: bili_task_
File extension: sh
```

Keep unspecified options at their defaults (including automatic synchronization and task creation). Do **not** restrict the subscription to a subdirectory: the hook needs the Qinglong scripts and `dotnet` mode needs `src/`. The hook copies scripts into `platforms/daidai/DefaultTasks` and removes `platforms/qinglong` from the *synced checkout*. Run the subscription once and check its log for hook execution, script scanning, and task creation.

### 4. Check the tasks

Under **Scheduled Tasks**, look for tasks such as **bili每日任务**, **bili扫码登录**, and **bili银瓜子兑换硬币任务**. If absent, inspect the subscription log for pull errors, missing hook execution, or a mismatched whitelist.

### 5. Sign in

Run **bili扫码登录** and scan the log's QR code with the Bilibili app. First execution may install the runtime. With working Open API credentials, the Cookie is written to `Ray_BiliBiliCookies__0` (then `__1`, `__2`, etc.). Without API access, copy the key and value printed in the login log into the panel's environment variables. Subsequent tasks read these values on their cron schedules.

## Runtime modes

| `BILI_MODE` | Behavior | Cookie persistence |
| --- | --- | --- |
| `dotnet` (default) | Install .NET 10 SDK and run the checked-out source using `dotnet run`. | Supported by the current source. |
| `bilitool` | Download and run a prebuilt stable release binary; does not install .NET. | Depends on whether that particular binary includes Daidai support. If not, add the logged Cookie manually. |

The old note that *all* binaries lack Daidai support is release-dependent: check the version you downloaded. For a new integration before the next stable binary, use source-based `dotnet` mode.

## Early builds

The repository pulls from `main`; the former `develop` branch and `dev/bili_dev_task_*.sh` scripts are gone. Unreleased alpha builds publish `zai7lou/bili_tool_web:alpha` as a **Docker image**, not a `bilitool` binary.

## GitHub proxies

For slow pulls, a proxy-prefixed subscription URL may help:

```text
https://gh-proxy.com/https://github.com/RayWangQvQ/BiliBiliToolPro.git
```

Set `BILI_GITHUB_PROXY` for binary downloads in `bilitool` mode. Third-party proxy uptime is not guaranteed.

## Troubleshooting

### `bash` missing or scripts fail

Use a Linux-based Daidai deployment with `bash`. The Windows standalone version is not supported by these scripts.

### .NET installation fails

Check network access and install .NET 10 manually in the container if needed, or use `BILI_MODE=bilitool`. The script supports installing `dotnet10-sdk` from Alpine 3.23 packages; older Alpine images need an upgrade or binary mode.

### No valid ICU package

The base script exports `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` already. If necessary, also set that variable to `1` in the panel environment.

### Cookie is not saved

1. Confirm the Open API application has the `envs` scope.
2. Check `DaiDaiConfig__AppKey` and `DaiDaiConfig__AppSecret` (double underscores).
3. Check whether the task container can reach `DaiDai_URL`; the default is `http://127.0.0.1:5700`.
4. If using `bilitool`, check whether your release binary includes Daidai support.

The login log reports API errors and prints the Cookie for manual entry on failure.

### Tasks are not registered

Check the subscription log for hook execution, scanned scripts with `# cron:` / `# new Env(...)`, and automatic task creation. If the scan finds none, verify `bili_task_` and the `sh` extension; if the hook did not run, verify `bash platforms/daidai/copyshfile.sh`. If nothing was downloaded, check repository connectivity.
