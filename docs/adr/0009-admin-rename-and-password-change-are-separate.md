# ADR-0009：面板管理员的改名与改密是两个独立操作

- 状态：已接受
- 日期：2026-09-29
- 关联：`src/Ray.BiliBiliTool.Web/Services/AuthService.cs`、`Services/Pages/Admin/`、`Components/Pages/Admin.razor`

## 背景

`/Admin` 页原本只有一张表单，四个字段（新用户名 / 当前密码 / 新密码 / 确认新密码），一次提交同时写 `User.Username` 与密码哈希：

```csharp
public async Task ChangePasswordAsync(string username, string currentPassword, string newPassword)
{
    ...
    user.Salt = salt;
    user.PasswordHash = hash;
    user.Username = username;   // 改名是改密的副作用
    await userRepository.UpdateAsync(user);
}
```

参数名 `username` 是**新**用户名，但签名的读法像调用方身份——名字与语义相反。由此产生三个具体问题：

- 只想改密码的用户，必须在「新用户名」里把当前用户名重填一遍；漏填或写错就在无提示、无确认的情况下把登录名改掉，且下一次登录才发现。
- 页面标题写的是「修改密码」，与它实际做的事不符；侧栏入口叫「管理账户」，与「账号管理」（B 站账号）几乎不可区分。
- 改名后 Cookie 里的 `ClaimTypes.Name` 仍是旧值，无人处理。

页面的 UI 重构（视觉 + 信息架构）无法绕开这一点：要拆出「账户信息」与「修改密码」两个分区，服务层就必须先把这两件事解开。

## 决策

1. **`IAuthService` 拆成两个方法**，各自以**当前密码**为授权凭据，各自只写自己那一列：

   ```csharp
   Task ChangePasswordAsync(string currentPassword, string newPassword);   // 不再写 Username
   Task ChangeUsernameAsync(string newUsername, string currentPassword);   // 不再写密码哈希
   ```

   同时把只读的 `GetAdminUserNameAsync()` 换成 `GetAdminAccountAsync()`，一次返回用户名与角色。

2. **`IAdminPageWorkflow` 同步拆成两个方法**，返回类型统一为 `AdminAccountChangeResult`（原 `AdminPasswordChangeResult` 带 Password 字样却要服务于改名，语义不符）。请求类型拆为 `AdminPasswordChangeRequest` 与 `AdminUsernameChangeRequest`。

3. **改名成功后不重签 Cookie**，页面提示「用户名已更新」并引导跳转 `/auth/logout`，用户以新用户名重新登录。

## 后果

- 改密不再有改名副作用；`username` 参数名不再与语义相反。
- 页面按操作分成两个表单、两个提交按钮，两个方法共用同一套结果类型，UI 只用一套错误通道（表单级内联 + 服务级 Snackbar）。
- 代价：改用户名后必然打断一次会话。这是刻意的——登录名变了，重新认证比留着一个 `Name` claim 陈旧的 Cookie 更一致。
- `AuthController` 只用 `LoginAsync`，不受签名变更影响。

## 备选方案

**A. 只拆 UI 与 workflow，`IAuthService` 签名不动。** 改密时把当前用户名原样传回，行为等价、改动最小。但「`username` 其实是新用户名」这个误导签名留在服务层——正是问题的根源，下次还会有人踩。未采纳。

**B. 移除改名能力，页面只改密码。** 最简，也不会再有两个表单。但管理员改登录名是合理需求，去掉后只能改数据库。未采纳。

**C. 改名后自动重签 Cookie 保持登录。** 体验最顺滑。但 Blazor Server 已建立的电路里无法安全重写响应 Cookie，需要绕 `HttpContextAccessor` 与额外中间件，成本明显高于收益。未采纳。
