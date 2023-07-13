using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                    var temp = configurationBuilder.Build();
                    var connectionString = temp["ConnectionStrings:DefaultConnection"]
                                           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

                    var builder = new DbContextOptionsBuilder<BiliDbContext>();
                    builder.UseSqlite(connectionString);

                    var dbContext = new BiliDbContext(builder.Options);
                    dbContext.Database.Migrate();

                    configurationBuilder.Add(new DbConfigurationSource
                    {
                        GetDbListFunc = ()=> dbContext.DbConfigs.AsNoTracking().Select(x=>(IDbConfigEntity)x).ToList(),
                        ReloadOnChange = true,
                        ReloadDelay = 0
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((hostBuilderContext, loggingBuilder) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .CreateLogger();
                    SelfLog.Enable(x => System.Console.WriteLine(x ?? ""));
                })
                .UseSerilog()
            ;
    }
}
