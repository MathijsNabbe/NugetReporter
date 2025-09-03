using System.Text;
using System.Xml;
using System.Xml.Linq;
using NugetReport.Extensions;
using NugetReport.Interfaces;
using NugetReport.Objects;

namespace NugetReport.Parsers;

public class DefaultPackagesReportParser : INugetReportParser
{
    public bool CentralizedPackageManagement => false;

    public string Process(DotnetContext context)
    {
        // Load data
        var references = context.ProjectFiles
            .SelectMany(LoadProjectReferences)
            .ToList();

        // --- Precompute groups ---
        var groupedReferences = references
            .GroupBy(r => r.Package, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // --- Generate report ---
        var sb = new StringBuilder();
        sb.SetReportTitle();

        // --- Section 1: Packages References ---
        foreach (var group in groupedReferences)
        {
            sb.AppendLine(group.Key);

            // print each project + version
            foreach (var reference in group.OrderBy(r => r.Project))
            {
                sb.AppendLine($"  {reference.Project,-35} {reference.Version}");
            }

            // check for mismatched versions
            var distinctVersions = group
                .Select(r => r.Version)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctVersions.Count > 1)
            {
                sb.AppendLine("This package has mismatched versions across projects.");
            }

            sb.AppendLine(); // spacing between packages
        }

        return sb.ToString();
    }

    private IEnumerable<(string Project, string Package, string Version)> LoadProjectReferences(string projectFile)
    {
        var projectName = Path.GetFileName(projectFile);

        XDocument? xml;
        try
        {
            xml = XDocument.Load(projectFile);
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