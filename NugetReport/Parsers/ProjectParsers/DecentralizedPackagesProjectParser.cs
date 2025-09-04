using System.Xml;
using System.Xml.Linq;
using NugetReport.Interfaces;

namespace NugetReport.Parsers.ProjectParsers;

public class DecentralizedPackagesProjectParser : IProjectParser
{
    public IReadOnlyCollection<string> SupportedNugetVersions => ["9.0"];

    public IEnumerable<(string Project, string Package, string Version)> ParseNugetReferences(string projectPath)
    {
        var projectName = Path.GetFileName(projectPath);

        XDocument? xml;
        try
        {
            xml = XDocument.Load(projectPath);
        }
        catch (XmlException e)
        {
            Console.WriteLine($"{projectName} is a malformed project file or has an unsupported encoding. Skipping...");
            yield break;
        }

        var packages = xml.Descendants("PackageReference");
        foreach (var package in packages)
        {
            var packageKey = package.Attribute("Include")?.Value ?? "";
            var packageVersion = package.Attribute("Version")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(packageKey) || string.IsNullOrWhiteSpace(packageKey))
            {
                Console.WriteLine($"Malformed package reference found in {projectName}.");
                continue;
            }

            yield return (projectName, packageKey, packageVersion);
        }
    }
}