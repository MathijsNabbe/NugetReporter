using NugetReport.Interfaces;

namespace NugetReport.Factories;

public class ProjectParserFactory
{
    private readonly IEnumerable<IProjectParser> _parsers;
    
    public ProjectParserFactory(IEnumerable<IProjectParser> parsers)
    {
        _parsers = parsers;
    }
    
    public IProjectParser? GetParser(string targetFramework)
    {
        var suitable = _parsers.FirstOrDefault(p => p.SupportedNugetVersions.Contains(targetFramework));

        if (suitable == null)
            Console.WriteLine($"No suitable parser found for dotnet version: {targetFramework}");

        return suitable;
    }
}