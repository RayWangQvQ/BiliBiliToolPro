# HTTPS 反向代理

Web 面板会读取受信任代理传入的 `X-Forwarded-Proto`，让登录跳转和链接使用外部访问协议。

同机回环代理默认受信任。独立代理请在部署配置中添加实际代理地址，例如：

```json
{
  "ReverseProxy": {
    "KnownProxies": ["203.0.113.10"],
    "KnownNetworks": ["203.0.113.0/24"]
  }
}
```

示例使用文档地址，部署时替换为代理地址。通常只需配置 `KnownProxies`，共享代理网段可使用 `KnownNetworks`。代理同时需要发送 `X-Forwarded-Proto`。

环境变量使用 `ReverseProxy__KnownProxies__0`，数组后续项递增索引。变更代理配置后重启面板。

实现遵循 [ASP.NET Core 转发标头文档](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)。
