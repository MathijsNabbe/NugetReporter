namespace NugetReport.Interfaces;

public interface IProjectParser
{
    IReadOnlyCollection<string> SupportedNugetVersions { get; }

    IEnumerable<(string Project, string Package, string Version)> ParseNugetReferences(string projectPath);
}