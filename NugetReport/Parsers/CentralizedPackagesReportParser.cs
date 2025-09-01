using System.Text;
using System.Xml.Linq;
using NugetReport.Interfaces;

namespace NugetReport.Parsers;

public class CentralizedPackagesReportParser : INugetReportParser
{
    public bool CentralizedPackageManagement => true;
    
    public string Process(string packageFile, string[] projectFiles)
    {
        var packageVersions = LoadPropsVersions(packageFile);
        var references = projectFiles.SelectMany(f => LoadProjectReferences(f)).ToList();

        var groupedPackageReferences = references
            .GroupBy(r => r.Package)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.AppendLine("# 📦 NuGet Package Report");
        sb.AppendLine();
        sb.AppendLine("| Package | Version | Projects |");
        sb.AppendLine("|---------|---------|----------|");
        
        foreach (var group in groupedPackageReferences)
        {
            var package = group.Key;
            var versionFound = packageVersions.TryGetValue(package, out var version);
            if (versionFound == false)
            {
                Console.WriteLine($"Package {package} not found in Directory.Packages.props.");
                continue;
            }
            
            var projects = string.Join(", ", group.Select(r => r.Project).Distinct().OrderBy(p => p));

            sb.AppendLine($"| {package} | {version} | {projects} |");
        }
        
        var unusedPackages = packageVersions.Keys.Except(groupedPackageReferences.Select(g => g.Key));
        if (unusedPackages.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## Unused Packages");
            sb.AppendLine();
            sb.AppendLine("| Package |");
            sb.AppendLine("|---------|");

            foreach (var kvp in unusedPackages)
            {
                sb.AppendLine($"| {kvp} |");
            }
        }
        
        return sb.ToString();
    }
    
    private Dictionary<string,string> LoadPropsVersions(string propsFile)
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