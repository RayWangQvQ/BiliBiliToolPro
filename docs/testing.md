# Testing conventions

The solution contains different kinds of tests; not every project under `test/` is a unit-test project. Keep each test in a project named `Ray.BiliBiliTool.<Area>.<Level>Tests` (for example, `Ray.BiliBiliTool.Web.ComponentTests`), with matching directory, project file, and namespace. Use `UnitTests` for isolated behavior, `IntegrationTests` for real in-process composition or local storage, `ComponentTests` for bUnit-rendered UI, `CharacterizationTests` for documented existing behavior, `ArchitectureTests` for dependency rules, and `FunctionalTests` for opt-in external API/provider checks.

Database-backed configuration and task-record tests live in `Config.IntegrationTests` and `Web.IntegrationTests`; live agent and notification-provider checks live in their respective `FunctionalTests` projects. The remaining agent request-construction and notification-formatting tests run without network access in `UnitTests` projects. Two integration projects were split from the original twelve projects, so the solution now has fourteen test projects.

## Writing tests

- Use xUnit. Name classes `<Subject>Tests` and methods `<Method>_<Condition>_<ExpectedOutcome>`; use `[Theory]` for variants of the same behavior.
- Arrange only the necessary inputs, exercise the public behavior, and assert its observable output or side effects. A test without a meaningful assertion is not a regression test.
- Cover meaningful success, failure, and boundary cases. Avoid assertions against implementation details, real-clock timing, unstable service data, or unspecified response formatting.
- Keep default tests deterministic: use fakes for HTTP and notifications, isolate database state, clean up temporary resources, and restore any process-wide configuration. Do not rely on run order or credentials.
- Mark **every** test that calls a real external service, requires an account cookie/token, or can send a notification with `[Trait("Category", "External")]`. Do not hide these tests behind a missing-credentials early return: either opt in with valid configuration or fail clearly.

## Running tests

The local and PR-safe suite uses the same filter as `.github/workflows/pr-checks.yml`:

```sh
dotnet test Ray.BiliBiliTool.sln --filter "Category!=External"
```

External functional tests are **not** part of the normal regression gate. Run them only when you intend to use a configured real account and understand that they may send notifications or modify Bilibili account state:

```sh
dotnet test Ray.BiliBiliTool.sln --filter "Category=External"
```

When adding tests, first choose the appropriate test level and place the case in that project's directory. A fake-HTTP request-serialization test is a unit test even if its subject is an API client; a test using a real Bilibili cookie is an external functional test even if it only reads data.

## Remaining coverage

This standardization prioritizes important cases in existing test areas. It does not establish an exhaustive source-wide coverage threshold; untested production modules should be assessed separately and prioritized by regression risk.

- Credential-dependent Bilibili and notification-provider paths require explicitly opted-in live checks; the offline suite exercises local request construction and notification formatting, not third-party availability.
- The notification functional project retains live Microsoft Teams and Work Weixin App checks. Other provider-specific smoke scripts without meaningful assertions were removed; adding deterministic provider contract tests is still open work.
- Host startup and Quartz integration cases deserve separate validation when changing host wiring or job scheduling; isolated service tests do not substitute for process-level behavior.
