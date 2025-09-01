using System.Text;
using System.Xml.Linq;

var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
var isGithubAction = !string.IsNullOrWhiteSpace(workspace);

if (string.IsNullOrWhiteSpace(workspace))
{
    Console.Write("Enter the workspace directory to scan: ");
    workspace = Console.ReadLine();

    while (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
    {
        Console.Write("Invalid directory. Please enter a valid path: ");
        workspace = Console.ReadLine();
    }
}
                
Console.WriteLine($"Scanning workspace: {workspace}");

var packageFile = GetPackageVersionFile(workspace);
var projectFiles = GetProjectFiles(workspace);

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
    var version = packageVersions.TryGetValue(package, out var v) ? v : "(no version found)";
    var projects = string.Join(", ", group.Select(r => r.Project).Distinct().OrderBy(p => p));

    sb.AppendLine($"| {package} | {version} | {projects} |");
}

var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
if (!string.IsNullOrWhiteSpace(summaryPath))
{
    File.AppendAllText(summaryPath, sb.ToString());
    Console.WriteLine($"Report written to GitHub Actions summary: {summaryPath}");
}
else
{
    var localReport = Path.Combine(workspace, "NugetReport.md");
    File.WriteAllText(localReport, sb.ToString());
    Console.WriteLine($"Report written to: {localReport}");
}

return 0;

string GetPackageVersionFile(string workspace)
{
    var packageFile = Directory.GetFiles(workspace, "Directory.Packages.props", SearchOption.AllDirectories).FirstOrDefault();
    if (packageFile == null)
    {
        Console.WriteLine("No Directory.Packages.props found");
    }

    Console.WriteLine("Directory.Packages.props detected!");
    return packageFile;
}

string[] GetProjectFiles(string workspace)
{
    var projectFiles = Directory.GetFiles(workspace, "*.csproj", SearchOption.AllDirectories);
    if (projectFiles.Length == 0)
    {
        Console.WriteLine("No project files found");
    }
    Console.WriteLine($"Detected {projectFiles.Length} project files");
    
    return projectFiles;
}

Dictionary<string,string> LoadPropsVersions(string propsFile)
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

IEnumerable<(string Project, string Package)> LoadProjectReferences(string projectFile)
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