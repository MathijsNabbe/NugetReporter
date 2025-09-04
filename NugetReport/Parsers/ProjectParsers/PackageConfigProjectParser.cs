using System.Xml;
using System.Xml.Linq;
using NugetReport.Helpers;
using NugetReport.Interfaces;

namespace NugetReport.Parsers.ProjectParsers;

public class PackageConfigProjectParser : IProjectParser
{
    public IReadOnlyCollection<string> SupportedNugetVersions => ["v4.8"];
    
    public IEnumerable<(string Project, string Package, string Version)> ParseNugetReferences(string projectPath)
    {
        var projectName = Path.GetFileName(projectPath);
        var packageFile = Path.Combine(Path.GetDirectoryName(projectPath) ?? "", "packages.config");
        if (File.Exists(packageFile) == false)
            yield break;

        var xml = XDocumentHelper.LoadXmlSafely(packageFile);
        if (xml == null)
        {
            Console.WriteLine($"Skipping {projectName}.");
            yield break;
        }
        
        var packages = xml.Descendants("package");
        foreach (var package in packages)
        {
            var packageKey = package.Attribute("id")?.Value ?? "";
            var packageVersion = package.Attribute("version")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(packageKey) || string.IsNullOrWhiteSpace(packageKey))
            {
                Console.WriteLine($"Malformed package reference found the packages.config of {projectName}.");
                continue;
            }

            yield return (projectName, packageKey, packageVersion);
        }
    }
}