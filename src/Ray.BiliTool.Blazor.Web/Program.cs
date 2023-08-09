using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Ray.BiliBiliTool.Config;
using Ray.BiliTool.Repository;
using Serilog;
using Serilog.Debugging;

namespace Ray.BiliTool.Blazor.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    //Êý¾Ý¿â
                    Directory.CreateDirectory("./db");
                    var temp = configurationBuilder.Build();
                    var connectionString = temp["ConnectionStrings:DefaultConnection"]
                                           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                    var builder = new DbContextOptionsBuilder<BiliDbContext>();
                    builder.UseSqlite(connectionString);

                    var dbContext = new BiliDbContext(builder.Options);
                    dbContext.Database.Migrate();

                    configurationBuilder.Add(new DbConfigurationSource
                    {
                        GetDbListFunc = () => dbContext.DbConfigs
                            .AsNoTracking()
                            .Select(x => (IDbConfigEntity)x)
                            .Where(x=>x.Enable)
                            .ToList(),
                        ReloadOnChange = true,
                        ReloadDelay = 0
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .CreateLogger();
                    SelfLog.Enable(x => Console.WriteLine(x ?? ""));
                })
                .UseSerilog()
            ;
    }
}
