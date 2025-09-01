using NugetReport.Objects;

namespace NugetReport.Interfaces;

public interface INugetReportParser
{
    string Process(DotnetContext context);
    bool CentralizedPackageManagement { get; }
}