using System.Text;
using System.Xml.Linq;
using NugetReport.Interfaces;
using NugetReport.Objects;

namespace NugetReport.Parsers;

public class CentralizedPackagesReportParser : INugetReportParser
{
    public bool CentralizedPackageManagement => true;

    public string Process(DotnetContext context)
    {
        // Load data
        var packageVersions = LoadPropsVersions(context.CentralizedPackageFile); // props file dictionary
        var references = context.ProjectFiles
            .SelectMany(f => LoadProjectReferences(f))
            .ToList(); // (Project, Package)

        var sb = new StringBuilder();
        sb.AppendLine("# 📦 NuGet Package Report");
        sb.AppendLine();

        // --- Section 1: Used Packages ---
        var groupedReferences = references
            .GroupBy(r => r.Package, StringComparer.OrdinalIgnoreCase)
            .ToList();

        sb.AppendLine("## Used Packages");
        sb.AppendLine();
        sb.AppendLine("| Package | Version | Projects |");
        sb.AppendLine("|---------|---------|----------|");

        foreach (var group in groupedReferences.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var package = group.Key;
            if (packageVersions.TryGetValue(package, out var version))
            {
                var projects = string.Join(", ", group
                    .Select(r => r.Project)
                    .Distinct()
                    .OrderBy(p => p));

                sb.AppendLine($"| {package} | {version} | {projects} |");
            }
        }

        // --- Section 2: Unused Packages ---
        var usedPackages = groupedReferences
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unusedPackages = packageVersions
            .Where(kvp => !usedPackages.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unusedPackages.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Unused Packages");
            sb.AppendLine();
            sb.AppendLine("| Package | Version |");
            sb.AppendLine("|---------|---------|");

            foreach (var kvp in unusedPackages)
            {
                sb.AppendLine($"| {kvp.Key} | {kvp.Value} |");
            }
        }

        // --- Section 3: Unused References ---
        var missingReferences = groupedReferences
            .Where(g => !packageVersions.ContainsKey(g.Key))
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingReferences.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Unused References");
            sb.AppendLine();
            sb.AppendLine("| Package | Projects |");
            sb.AppendLine("|---------|----------|");

            foreach (var group in missingReferences)
            {
                var projects = string.Join(", ", group
                    .Select(r => r.Project)
                    .Distinct()
                    .OrderBy(p => p));

                sb.AppendLine($"| {group.Key} | {projects} |");
            }
        }

        return sb.ToString();
    }

    private Dictionary<string, string> LoadPropsVersions(string propsFile)
    {
        var xml = XDocument.Load(propsFile);
        var packages = xml.Descendants("PackageVersion");

        var packageVersions = new Dictionary<string, string>();
        foreach (var package in packages)
        {
            var packageKey = package.Attribute("Include")?.Value ?? "";
            var packageVersion = package.Attribute("Version")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(packageKey) || string.IsNullOrWhiteSpace(packageVersion))
            {
                Console.WriteLine("Malformed package reference found in Directory.Packages.props.");
                continue;
            }

            if (packageVersions.ContainsKey(packageKey))
            {
                Console.WriteLine($"Duplicate package {package.Attribute("Include")?.Value ?? ""} found");
                continue;
            }

            packageVersions.Add(packageKey, package.Attribute("Version")?.Value ?? "");
        }

        return packageVersions;
    }

    private IEnumerable<(string Project, string Package)> LoadProjectReferences(string projectFile)
    {
        var projectName = Path.GetFileName(projectFile);
        var xml = XDocument.Load(projectFile);
        var packages = xml.Descendants("PackageReference");
        foreach (var package in packages)
        {
            var packageKey = package.Attribute("Include")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(packageKey))
            {
                Console.WriteLine($"Malformed package reference found in {projectName}.");
                continue;
            }

            yield return (projectName, packageKey);
        }
    }
}