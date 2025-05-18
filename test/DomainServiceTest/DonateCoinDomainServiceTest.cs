namespace DomainServiceTest;

public class DonateCoinDomainServiceTest
{
    public DonateCoinDomainServiceTest()
    {
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }
}
