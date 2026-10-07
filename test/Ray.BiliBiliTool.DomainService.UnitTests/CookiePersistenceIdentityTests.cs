using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ray.BiliBiliTool.Agent;
using Ray.BiliBiliTool.Agent.Baihu;
using Ray.BiliBiliTool.Agent.DaiDai;
using Ray.BiliBiliTool.Agent.QingLong;
using Ray.BiliBiliTool.Config.Options;
using Ray.BiliBiliTool.Domain.Exceptions;
using Ray.BiliBiliTool.DomainService;
using Refit;

namespace Ray.BiliBiliTool.DomainService.UnitTests;

public class CookiePersistenceIdentityTests
{
    [Theory]
    [InlineData("QingLong", "overlap")]
    [InlineData("Baihu", "overlap")]
    [InlineData("DaiDai", "overlap")]
    [InlineData("QingLong", "incidental")]
    [InlineData("Baihu", "incidental")]
    [InlineData("DaiDai", "incidental")]
    [InlineData("QingLong", "exact")]
    [InlineData("Baihu", "exact")]
    [InlineData("DaiDai", "exact")]
    [InlineData("QingLong", "missing-account")]
    [InlineData("Baihu", "missing-account")]
    [InlineData("DaiDai", "missing-account")]
    [InlineData("QingLong", "malformed")]
    [InlineData("Baihu", "malformed")]
    [InlineData("DaiDai", "malformed")]
    public async Task PanelSave_UpdatesOnlyTheExactAccount(string platform, string scenario)
    {
        var handler = new PanelHandler(platform, scenario);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://panel.invalid"),
        };
        var service = CreateService(platform, client);
        Assert.True(await SavePanel(service, platform, Cookie("12")));
        Assert.Equal(1, handler.Writes);
        var shouldUpdate = scenario is "overlap" or "exact" or "malformed";
        Assert.Equal(shouldUpdate ? HttpMethod.Put : HttpMethod.Post, handler.WriteMethod);
        Assert.Equal(shouldUpdate ? 2 : 0, handler.UpdatedId);
        Assert.Equal("Ray_BiliBiliCookies__1", handler.SavedName);
    }

    [Theory]
    [InlineData("QingLong", "")]
    [InlineData("Baihu", "")]
    [InlineData("DaiDai", "")]
    [InlineData("QingLong", "0")]
    [InlineData("Baihu", "0")]
    [InlineData("DaiDai", "0")]
    [InlineData("QingLong", "-1")]
    [InlineData("Baihu", "-1")]
    [InlineData("DaiDai", "-1")]
    [InlineData("QingLong", "invalid")]
    [InlineData("Baihu", "invalid")]
    [InlineData("DaiDai", "invalid")]
    [InlineData("QingLong", "999999999999999999999")]
    [InlineData("Baihu", "999999999999999999999")]
    [InlineData("DaiDai", "999999999999999999999")]
    public async Task InvalidAccount_DoesNotContactOrModifyAnyPanel(string platform, string id)
    {
        var handler = new PanelHandler(platform, "overlap");
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://panel.invalid"),
        };
        Assert.False(await SavePanel(CreateService(platform, client), platform, Cookie(id)));
        Assert.Equal(0, handler.Requests);
        Assert.Equal(0, handler.Writes);
    }

    [Theory]
    [InlineData("overlap")]
    [InlineData("incidental")]
    [InlineData("exact")]
    [InlineData("missing-account")]
    public async Task JsonSave_PreservesOtherAccounts(string scenario)
    {
        var directory = Directory.CreateTempSubdirectory("bili-cookie-identity-");
        try
        {
            var original = OriginalCookies(scenario);
            var file = Path.Combine(directory.FullName, "cookies.json");
            await File.WriteAllTextAsync(
                file,
                JsonSerializer
                    .Serialize(
                        new { BiliBiliCookies = original },
                        new JsonSerializerOptions { WriteIndented = true }
                    )
                    .Replace("\n", Environment.NewLine)
            );
            using var client = new HttpClient(new PanelHandler("QingLong", scenario));
            var cookie = Cookie("12");
            await CreateService("Local", client, directory.FullName)
                .SaveCookieToJsonFileAsync(cookie, CancellationToken.None);
            using var data = JsonDocument.Parse(
                await File.ReadAllTextAsync(file),
                new JsonDocumentOptions { AllowTrailingCommas = true }
            );
            var saved = data
                .RootElement.GetProperty("BiliBiliCookies")
                .EnumerateArray()
                .Select(row => row.GetString()!)
                .ToArray();
            var update = scenario is "overlap" or "exact";
            Assert.Equal(update ? original.Length : original.Length + 1, saved.Length);
            Assert.Equal(cookie.CookieStr, saved[update ? 1 : ^1]);
            Assert.Equal(original[0], saved[0]);
            if (!update)
                Assert.Equal(original, saved[..original.Length]);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("invalid")]
    [InlineData("999999999999999999999")]
    public async Task InvalidAccount_DoesNotModifyJson(string id)
    {
        var directory = Directory.CreateTempSubdirectory("bili-cookie-identity-");
        try
        {
            var file = Path.Combine(directory.FullName, "cookies.json");
            var original = JsonSerializer
                .Serialize(
                    new { BiliBiliCookies = OriginalCookies("overlap") },
                    new JsonSerializerOptions { WriteIndented = true }
                )
                .Replace("\n", Environment.NewLine);
            await File.WriteAllTextAsync(file, original);
            using var client = new HttpClient(new PanelHandler("QingLong", "overlap"));
            await Assert.ThrowsAsync<BiliValidationException>(() =>
                CreateService("Local", client, directory.FullName)
                    .SaveCookieToJsonFileAsync(Cookie(id), CancellationToken.None)
            );
            Assert.Equal(original, await File.ReadAllTextAsync(file));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static BiliCookie Cookie(string id) =>
        new(
            new()
            {
                ["DedeUserID"] = id,
                ["SESSDATA"] = "synthetic-new-session",
                ["bili_jct"] = "synthetic-csrf",
            }
        );

    [Theory]
    [InlineData("single-line")]
    [InlineData("lf")]
    [InlineData("crlf")]
    [InlineData("empty")]
    [InlineData("no-cookie")]
    [InlineData("comments")]
    public async Task JsonSave_PreservesConfigurationAcrossSupportedFormats(string format)
    {
        var directory = Directory.CreateTempSubdirectory("bili-cookie-identity-");
        try
        {
            var file = Path.Combine(directory.FullName, "cookies.json");
            var oldCookie = "DedeUserID=123; SESSDATA=synthetic-first";
            var row = JsonSerializer.Serialize(oldCookie);
            var content =
                format == "no-cookie"
                    ? "{\"Other\":{\"enabled\":true}}"
                    : "{\"Other\":{\"enabled\":true},\"BiliBiliCookies\":["
                        + (format == "empty" ? "" : row)
                        + "]}";
            if (format is "lf" or "crlf" or "comments")
            {
                var newline = format == "crlf" ? "\r\n" : "\n";
                content =
                    "{"
                    + newline
                    + (format == "comments" ? "// synthetic comment" + newline : "")
                    + "  \"Other\": {\"enabled\":true},"
                    + newline
                    + "  \"BiliBiliCookies\": ["
                    + newline
                    + "    "
                    + row
                    + ","
                    + newline
                    + "  ],"
                    + newline
                    + "}";
            }
            await File.WriteAllTextAsync(file, content);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            using var client = new HttpClient(new PanelHandler("QingLong", "exact"));
            var cookie = Cookie("12");
            await CreateService("Local", client, directory.FullName)
                .SaveCookieToJsonFileAsync(cookie, CancellationToken.None);
            using var data = JsonDocument.Parse(
                await File.ReadAllTextAsync(file),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                }
            );
            Assert.True(data.RootElement.GetProperty("Other").GetProperty("enabled").GetBoolean());
            var rows = data
                .RootElement.GetProperty("BiliBiliCookies")
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToArray();
            Assert.Equal(format is "empty" or "no-cookie" ? 1 : 2, rows.Length);
            Assert.Equal(cookie.CookieStr, rows[^1]);
            if (rows.Length == 2)
                Assert.Equal(oldCookie, rows[0]);
            Assert.Single(Directory.GetFiles(directory.FullName));
            if (!OperatingSystem.IsWindows())
                Assert.Equal(
                    UnixFileMode.UserRead | UnixFileMode.UserWrite,
                    File.GetUnixFileMode(file)
                );
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"BiliBiliCookies\":\"invalid\"}")]
    public async Task JsonSave_InvalidStructureLeavesTheOriginalFileIntact(string content)
    {
        var directory = Directory.CreateTempSubdirectory("bili-cookie-identity-");
        try
        {
            var file = Path.Combine(directory.FullName, "cookies.json");
            await File.WriteAllTextAsync(file, content);
            using var client = new HttpClient(new PanelHandler("QingLong", "exact"));
            await Assert.ThrowsAsync<BiliValidationException>(() =>
                CreateService("Local", client, directory.FullName)
                    .SaveCookieToJsonFileAsync(Cookie("12"), CancellationToken.None)
            );
            Assert.Equal(content, await File.ReadAllTextAsync(file));
            Assert.Single(Directory.GetFiles(directory.FullName));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task JsonSave_ConcurrentAccountsAreBothPreserved()
    {
        var directory = Directory.CreateTempSubdirectory("bili-cookie-identity-");
        try
        {
            var file = Path.Combine(directory.FullName, "cookies.json");
            await File.WriteAllTextAsync(file, "{\"BiliBiliCookies\":[]}");
            using var client = new HttpClient(new PanelHandler("QingLong", "exact"));
            var service = CreateService("Local", client, directory.FullName);
            await Task.WhenAll(
                service.SaveCookieToJsonFileAsync(Cookie("12"), CancellationToken.None),
                service.SaveCookieToJsonFileAsync(Cookie("123"), CancellationToken.None)
            );
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(file));
            var rows = document
                .RootElement.GetProperty("BiliBiliCookies")
                .EnumerateArray()
                .Select(x => x.GetString())
                .ToArray();
            Assert.Equal(2, rows.Length);
            Assert.Contains(Cookie("12").CookieStr, rows);
            Assert.Contains(Cookie("123").CookieStr, rows);
            Assert.Single(Directory.GetFiles(directory.FullName));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public async Task JsonSave_PreservesDateStringsAndNumericPrecision()
    {
        var directory = Directory.CreateTempSubdirectory("bili-cookie-identity-");
        try
        {
            var file = Path.Combine(directory.FullName, "cookies.json");
            await File.WriteAllTextAsync(
                file,
                "{\"BiliBiliCookies\":[],\"Other\":{\"timestamp\":\"2026-10-07T01:02:03.000Z\",\"number\":0.12345678901234567890123}}"
            );
            using var client = new HttpClient(new PanelHandler("QingLong", "exact"));
            await CreateService("Local", client, directory.FullName)
                .SaveCookieToJsonFileAsync(Cookie("12"), CancellationToken.None);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(file));
            var other = document.RootElement.GetProperty("Other");
            Assert.Equal("2026-10-07T01:02:03.000Z", other.GetProperty("timestamp").GetString());
            Assert.Equal("0.12345678901234567890123", other.GetProperty("number").GetRawText());
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static string[] OriginalCookies(string scenario) =>
        scenario switch
        {
            "overlap" or "malformed" =>
            [
                "DedeUserID=123; SESSDATA=synthetic-first",
                "DedeUserID = 12; SESSDATA=synthetic-second",
            ],
            "incidental" => ["DedeUserID=456; SESSDATA=synthetic-12"],
            "exact" =>
            [
                "DedeUserID=456; SESSDATA=synthetic-first",
                "DedeUserID=12; SESSDATA=synthetic-second",
            ],
            _ => ["DedeUserID=456; SESSDATA=synthetic-first"],
        };

    private static Task<bool> SavePanel(
        LoginDomainService service,
        string platform,
        BiliCookie cookie
    ) =>
        platform switch
        {
            "QingLong" => service.SaveCookieToQinLongAsync(cookie, CancellationToken.None),
            "Baihu" => service.SaveCookieToBaihuAsync(cookie, CancellationToken.None),
            _ => service.SaveCookieToDaiDaiAsync(cookie, CancellationToken.None),
        };

    private static LoginDomainService CreateService(
        string platform,
        HttpClient client,
        string? path = null
    ) =>
        new(
            NullLogger<LoginDomainService>.Instance,
            null!,
            new SyntheticEnvironment(path ?? Path.GetTempPath()),
            RestService.For<IQingLongApi>(client),
            RestService.For<IBaihuApi>(client),
            RestService.For<IDaiDaiApi>(client),
            null!,
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Ray_PlatformType"] = platform }
                )
                .Build(),
            Options.Create(
                new QingLongOptions { ClientId = "synthetic", ClientSecret = "synthetic" }
            ),
            Options.Create(new BaihuOptions { Token = "synthetic" }),
            Options.Create(new DaiDaiOptions { AppKey = "synthetic", AppSecret = "synthetic" })
        );

    private sealed class SyntheticEnvironment(string path) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "synthetic";
        public string ContentRootPath { get; set; } = path;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class PanelHandler(string platform, string scenario) : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public int Writes { get; private set; }
        public HttpMethod? WriteMethod { get; private set; }
        public int UpdatedId { get; private set; }
        public string? SavedName { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests++;
            object payload;
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("token"))
                payload = new
                {
                    code = 200,
                    data = new
                    {
                        token = "synthetic",
                        access_token = "synthetic",
                        token_type = "Bearer",
                    },
                };
            else if (request.Method == HttpMethod.Get)
            {
                var rows = OriginalCookies(scenario)
                    .Select(
                        (value, index) => Row(index + 1, "Ray_BiliBiliCookies__" + index, value)
                    )
                    .ToList();
                if (scenario == "malformed")
                    rows.Insert(0, Row(7, null, null));
                payload = new { code = 200, data = rows };
            }
            else
            {
                Writes++;
                WriteMethod = request.Method;
                using var document = JsonDocument.Parse(
                    await request.Content!.ReadAsStringAsync(cancellationToken)
                );
                var row =
                    document.RootElement.ValueKind == JsonValueKind.Array
                        ? document.RootElement[0]
                        : document.RootElement;
                SavedName = row.GetProperty("name").GetString();
                if (request.Method == HttpMethod.Put)
                    UpdatedId = int.Parse(
                        path.Split('/').LastOrDefault() is "1" or "2" or "7"
                            ? path.Split('/').Last()
                            : row.GetProperty("id").ToString()
                    );
                var acknowledgement = Row(
                    UpdatedId == 0 ? 3 : UpdatedId,
                    SavedName,
                    row.GetProperty("value").GetString()
                );
                payload = new
                {
                    code = 200,
                    message = "synthetic",
                    data = platform == "QingLong" && request.Method == HttpMethod.Post
                        ? new[] { acknowledgement }
                        : acknowledgement,
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        }

        private object Row(int id, string? name, string? value) =>
            new
            {
                id = platform == "Baihu" ? (object)id.ToString() : id,
                name,
                value,
                timestamp = "synthetic-time",
            };
    }
}
