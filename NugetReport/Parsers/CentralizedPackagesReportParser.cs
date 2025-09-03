using System.Text;
using System.Xml;
using System.Xml.Linq;
using NugetReport.Extensions;
using NugetReport.Interfaces;
using NugetReport.Objects;

namespace NugetReport.Parsers;

public class CentralizedPackagesReportParser : INugetReportParser
{
    public bool CentralizedPackageManagement => true;

    public string Process(DotnetContext context)
    {
        if (string.IsNullOrWhiteSpace(context.CentralizedPackageFile))
            throw new InvalidOperationException("No centralized package file found.");
        
        // Load data
        var packageVersions = LoadPropsVersions(context.CentralizedPackageFile);
        var references = context.ProjectFiles
            .SelectMany(LoadProjectReferences)
            .ToList();

        // --- Precompute groups ---
        var groupedReferences = references
            .GroupBy(r => r.Package, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usedPackages = groupedReferences
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unusedPackages = packageVersions
            .Where(kvp => !usedPackages.Contains(kvp.Key))
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unusedReferences = groupedReferences
            .Where(g => !packageVersions.ContainsKey(g.Key))
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // --- Generate report ---
        var sb = new StringBuilder();
        sb.SetReportTitle();
        
        // --- Section 0: Summary ---
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Used packages | {usedPackages.Count} |");
        sb.AppendLine($"| Unused packages | {unusedPackages.Count} |");
        sb.AppendLine($"| Missing references | {unusedReferences.Count} |");
        sb.AppendLine();

        // --- Section 1: Used Packages ---
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
        if (unusedReferences.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Unused References");
            sb.AppendLine();
            sb.AppendLine("| Package | Projects |");
            sb.AppendLine("|---------|----------|");

            foreach (var group in unusedReferences)
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
        
        XDocument? xml;
        try
        {
            xml = XDocument.Load(projectFile);
        }
        catch (XmlException e)
        {
            Console.WriteLine($"{projectName} is a malformed project file or has an unsupported encoding. Skipping.. .");
            yield break;
        }
        
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