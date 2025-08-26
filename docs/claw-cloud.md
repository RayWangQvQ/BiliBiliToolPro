# Claw免费容器部署

## 教程

点击 [https://console.run.claw.cloud/signin](https://console.run.claw.cloud/signin?link=FNTTMHS056E5) 注册账号，选择使用 GitHub 账号注册并登录。

成功后，每个月会赠送 $5 额度，跑 BiliTool 绰绰有余。

左上角可以选择一个区域，然后点击 **App Store**。

![claw-app-store.png](/docs/imgs/claw-app-store.png)

搜索`BiliTool`并点击如下搜索结果。

![claw-search.png](/docs/imgs/claw-search.png)

点击`Deploy App`按钮一键部署。

![claw-deploy.png](/docs/imgs/claw-deploy.png)

等待半分钟左右，进入容器 Detail 页面，点击如下链接即可访问站点：

![claw-addr.png](/docs/imgs/claw-addr.png)

## 消息推送

建议使用环境变量配置：

![claw-notification.png](/docs/imgs/claw-notification.png)

配置值见：[confifuration](/docs/configuration.md)

## 费用

官方模板默认配置为 `0.25C 512M`，每天 $0.06，一个月 `30 * 0.06 = $1.8`，每月赠送是 $5，还很富裕。

## 其他

### 账号

- 默认用户名：admin
- 默认密码：BiliTool@2233

首次登陆后，请立即修改账号和密码！

### 速度

不同区域速度可能有差异，可自己切换尝试。

如果速度慢可能会导致页面短时无响应，可稍作等待，或手动刷新。

### 更新

右上角`Update`进行版本更新，如果更新后启动异常，请尝试`Pause`然后再`Restart`。
