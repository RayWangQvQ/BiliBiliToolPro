using Xunit;

namespace Ray.BiliBiliTool.Host.IntegrationTests.Support;

/// <summary>
/// Booting the web host registers the app's 13 jobs into the single SQLite store that every
/// WebHostFactory in this process shares, so boots must not overlap each other.
/// </summary>
[CollectionDefinition("Host boot", DisableParallelization = true)]
public class HostBootCollection;
