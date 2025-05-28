using Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos;

namespace DomainServiceTest;

public class CalculateUpgradeTimeTest
{
    public CalculateUpgradeTimeTest()
    {
        Program.CreateHost(new[] { "--ENVIRONMENT=Development" });
    }

    [Fact]
    public void TestCalculateUpgradeTime()
    {
        using var scope = Global.ServiceProviderRoot.CreateScope();
        var accountDomainService =
            scope.ServiceProvider.GetRequiredService<IAccountDomainService>();
        int needDay = accountDomainService.CalculateUpgradeTime(
            new UserInfo
            {
                Money = 7,
                Level_info = new LevelInfo()
                {
                    Current_level = 5,
                    Current_exp = 100,
                    Next_exp = 200,
                },
                Uname = "uname",
                Wallet = new(),
                Wbi_img = new() { img_url = "", sub_url = "" },
            }
        );
        int needDay2 = accountDomainService.CalculateUpgradeTime(
            new UserInfo()
            {
                Money = 7,
                Level_info = new LevelInfo()
                {
                    Current_level = 5,
                    Current_exp = 1000,
                    Next_exp = 2000,
                },
                Uname = "uname",
                Wallet = new(),
                Wbi_img = new() { img_url = "", sub_url = "" },
            }
        );

        Assert.Equal(1, needDay);
        Assert.Equal(37, needDay2);
    }
}
