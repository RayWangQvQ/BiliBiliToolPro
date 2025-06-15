## Add new migration

```bash
cd ./src/Ray.BiliBiliTool.Web
dotnet ef migrations add AddUser --project ../Ray.BiliBiliTool.Infrastructure.EF
```

## Remove migration

```bash
dotnet ef migrations remove --project ../Ray.BiliBiliTool.Infrastructure.EF
```
