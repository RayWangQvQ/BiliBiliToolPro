using System.Reflection;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using AgentServiceCollection = Ray.BiliBiliTool.Agent.Extensions.ServiceCollectionExtension;
using ApplicationServiceCollection = Ray.BiliBiliTool.Application.Extensions.ServiceCollectionExtension;
using DomainServiceCollection = Ray.BiliBiliTool.DomainService.Extensions.ServiceCollectionExtensions;
using InfrastructureGlobal = Ray.BiliBiliTool.Infrastructure.Global;
using WebServiceCollection = Ray.BiliBiliTool.Web.Extensions.ServiceCollectionExtension;

namespace Ray.BiliBiliTool.ArchitectureTests;

public class DependencyGuardrailTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(AgentServiceCollection).Assembly,
            typeof(ApplicationServiceCollection).Assembly,
            typeof(Ray.BiliBiliTool.Console.Program).Assembly,
            typeof(Ray.BiliBiliTool.Domain.User).Assembly,
            typeof(DomainServiceCollection).Assembly,
            typeof(Ray.BiliBiliTool.Infrastructure.EF.DbInitializer).Assembly,
            typeof(InfrastructureGlobal).Assembly,
            typeof(WebServiceCollection).Assembly
        )
        .Build();

    private static readonly IObjectProvider<IType> QuartzJobLayer = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Web.Jobs")
        .As("Quartz job layer");

    private static readonly IObjectProvider<IType> HostForbiddenLowerLayers = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Agent")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.DomainService")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.Infrastructure")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.Infrastructure.EF")
        .As("host-forbidden lower layers");

    private static readonly IObjectProvider<IType> ApplicationLayer = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Application")
        .As("application layer");

    private static readonly IObjectProvider<IType> WebAndSchedulerTypes = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Web")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.Web.Client")
        .Or()
        .ResideInNamespace("BlazingQuartz")
        .Or()
        .ResideInNamespace("Quartz")
        .As("web and scheduler types");

    private static readonly IObjectProvider<IType> TransportDtos = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Agent.BiliBiliAgent.Dtos")
        .As("transport DTOs");

    private static readonly IObjectProvider<IType> DomainAndPolicyLayer = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Domain")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.DomainService")
        .As("domain and domain service layer");

    private static readonly IObjectProvider<IType> DomainForbiddenTypes = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Web")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.Web.Client")
        .Or()
        .ResideInNamespace("Ray.BiliBiliTool.Infrastructure.EF")
        .Or()
        .ResideInNamespace("Quartz")
        .Or()
        .ResideInNamespace("BlazingQuartz")
        .As("domain-forbidden types");

    private static readonly IObjectProvider<IType> WebComponentLayer = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Web.Components")
        .As("web component layer");

    private static readonly IObjectProvider<IType> InfrastructureLayers = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Infrastructure")
        .As("infrastructure layers");

    private static readonly IObjectProvider<IType> NotificationAdapters = Types()
        .That()
        .ResideInNamespace("Ray.BiliBiliTool.Infrastructure.Notifications")
        .As("notification adapter layer");

    [Fact]
    public void Quartz_jobs_should_not_reach_directly_into_lower_layers()
    {
        IArchRule rule = Types()
            .That()
            .Are(QuartzJobLayer)
            .Should()
            .NotDependOnAny(HostForbiddenLowerLayers)
            .Because(
                "Phase 1 forbids host and Quartz entry code from reaching directly into lower layers"
            );

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_should_not_depend_on_web_scheduler_or_transport_dto_types()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(WebAndSchedulerTypes)
            .Because(
                "Application stays the orchestration boundary and should not absorb web or scheduler concerns"
            );

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_transport_dto_dependencies_should_stay_within_the_known_legacy_allowlist()
    {
        string applicationProjectDir = GetApplicationProjectDirectory();

        string[] actualFiles = Directory
            .GetFiles(applicationProjectDir, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
                Regex.IsMatch(
                    File.ReadAllText(file),
                    @"Ray\.BiliBiliTool\.Agent\.BiliBiliAgent\.Dtos"
                )
            )
            .Select(file => Path.GetFileName(file)!)
            .OrderBy(file => file)
            .ToArray();

        string[] allowedFiles =
        [
            "ChargeTaskAppService.cs",
            "DailyTaskAppService.cs",
            "MangaPrivilegeTaskAppService.cs",
            "VipBigPointAppService.cs",
            "VipPrivilegeTaskAppService.cs",
        ];

        Assert.Equal(allowedFiles.OrderBy(file => file), actualFiles);
    }

    [Fact]
    public void Domain_and_domain_service_should_not_depend_on_web_ef_or_scheduler_types()
    {
        IArchRule rule = Types()
            .That()
            .Are(DomainAndPolicyLayer)
            .Should()
            .NotDependOnAny(DomainForbiddenTypes)
            .Because(
                "domain logic and policy services should stay free of host, EF, and scheduler concerns in Phase 1"
            );

        rule.Check(Architecture);
    }

    [Fact]
    public void Web_component_code_behind_classes_should_not_directly_depend_on_infrastructure()
    {
        IArchRule rule = Types()
            .That()
            .Are(WebComponentLayer)
            .Should()
            .NotDependOnAny(InfrastructureLayers)
            .Because(
                "Web component code-behind classes must route Domain and Infrastructure access through Web-layer workflow seams (Phases 13-15)"
            );

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_should_not_depend_on_notification_adapters()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(NotificationAdapters)
            .Because(
                "Application-layer code must use INotificationService from Application.Contracts, not Infrastructure notification adapters directly (Phase 20)"
            );

        rule.Check(Architecture);
    }

    private static string GetApplicationProjectDirectory()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "Ray.BiliBiliTool.Application"
            )
        );
    }
}
