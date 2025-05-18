using Xunit.Abstractions;

namespace DomainServiceTest;

public class ArticleDomainServiceTest
{
    private readonly ITestOutputHelper _output;

    public ArticleDomainServiceTest(ITestOutputHelper output)
    {
        _output = output;
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }
}
