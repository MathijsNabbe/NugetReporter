using NugetReport.Interfaces;
using NugetReport.Objects;

namespace NugetReport.Factories;

public class NugetReportParserFactory
{
    private readonly IEnumerable<INugetReportParser> _parsers;
    
    public NugetReportParserFactory(IEnumerable<INugetReportParser> parsers)
    {
        _parsers = parsers;
    }
    
    public INugetReportParser GetParser(DotnetContext context)
    {
        var isCentralized = context.CentralizedPackageFile != null;
        var suitable = _parsers.FirstOrDefault(p => p.CentralizedPackageManagement == isCentralized);

        if (suitable == null)
            throw new InvalidOperationException("No suitable processor found for the context.");

        return suitable;
    }
}