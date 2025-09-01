using Microsoft.Extensions.DependencyInjection;
using NugetReport.Interfaces;

namespace NugetReport.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNugetReportParser<T>(this IServiceCollection services) where T : class, INugetReportParser
    {
        return services.AddScoped<INugetReportParser, T>();
    }
}