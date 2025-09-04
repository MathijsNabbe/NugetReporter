using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NugetReport;
using NugetReport.Extensions;
using NugetReport.Factories;
using NugetReport.Parsers.ProjectParsers;
using NugetReport.Parsers.ReportParsers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddNugetReportParser<CentralizedPackagesReportParser>();
        services.AddNugetReportParser<DefaultPackagesReportParser>();
        services.AddScoped<NugetReportParserFactory>();

        services.AddProjectParser<PackageConfigProjectParser>();
        services.AddScoped<ProjectParserFactory>();
        
        services.AddScoped<App>();
    })
    .Build();

// Run your main app
await host.Services.GetRequiredService<App>().RunAsync();