using Bunit;
using Xunit;

namespace Ray.BiliBiliTool.Web.ComponentTests;

/// <summary>
/// Base class for component tests.
/// Bridges bUnit 2.x's IAsyncDisposable to xUnit 2.x's IAsyncLifetime so that
/// services implementing IAsyncDisposable (e.g. MudBlazor's KeyInterceptorService)
/// are disposed asynchronously instead of failing the synchronous container dispose.
/// </summary>
public abstract class ComponentTestBase : BunitContext, IAsyncLifetime
{
    public virtual Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsyncCore();
}
