# Run with Baihu Panel

Baihu's repository sync pulls the source and registers cron tasks from shell script comments. Its `mise` integration provides the .NET 10 runtime for task execution. Start with a working Baihu panel.

## Set up repository sync

On the **Script Library Sync** page, import one of these task definitions. The hook [`copyshfile.sh`](./copyshfile.sh) copies task scripts from Qinglong and removes that copy of the Qinglong directory from the synced checkout. The old “dev” variant below is retained for reference, but the repository no longer has a `dev` task directory: both commands currently select the same tasks.

```text
Name: Pull Bili repository (include dev; legacy label)
Command: baihu reposync --source-type git --source-url https://github.com/RayWangQvQ/BiliBiliToolPro.git --pre-command "bash platforms/baihu/copyshfile.sh"  --blacklist platforms/qinglong/DefaultTasks|.git --task-timeout 30 --task-langs '[{"name":"dotnet","version":"10.0"}]'
Schedule: 2 3 28 * *
```

```text
Name: Pull Bili repository (exclude dev; legacy label)
Command: baihu reposync --source-type git --source-url https://github.com/RayWangQvQ/BiliBiliToolPro.git --pre-command "bash platforms/baihu/copyshfile.sh"  --blacklist platforms/qinglong/DefaultTasks|.git|platforms/baihu/DefaultTasks/dev --task-timeout 30 --task-langs '[{"name":"dotnet","version":"10.0"}]'
Schedule: 2 3 28 * *
```

Save and run the sync task once. If successful, the panel parses the `.sh` script comments and registers Bilibili scheduled tasks.

## Runtime

Run a task in Baihu. The base script uses `mise exec dotnet@10` and checks for the required SDK. Installation depends on the panel's `mise` setup and network access; if automatic installation fails, check connectivity or install `dotnet@10` in Baihu's **Programming Languages** page.

## Sign in and save Cookies

Run **bili扫码登录** (Bili QR sign-in) from Baihu's scheduled tasks and scan the QR code in the log. For automatic Cookie persistence through the Baihu OpenAPI, set:

- `BaihuConfig__Token`: your Baihu API token, obtained from the panel's OpenAPI settings.
- `BA_URL` (optional): panel address, default `http://localhost:8052` in the application; change it if the panel is not reachable there.

If API access is not configured, copy the Cookie from the login log into the panel's environment variables:

```text
Name: Ray_BiliBiliCookies__0
Value: your Cookie
```

For more accounts, use `Ray_BiliBiliCookies__1`, `Ray_BiliBiliCookies__2`, and so on.

## Slow GitHub downloads

For slow repository pulls, a third-party proxy prefix may help, for example:

```text
https://gh-proxy.com/https://github.com/RayWangQvQ/BiliBiliToolPro.git
```

Proxy availability varies.

## Troubleshooting

- If `mise` cannot install .NET, check connectivity and try installing `dotnet@10` through the panel's **Programming Languages** page.
- For `Couldn't find a valid ICU package installed on the system` in a minimal container, add `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` with value `1` to the panel environment.
