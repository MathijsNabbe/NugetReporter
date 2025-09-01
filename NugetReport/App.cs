using System.Text;
using System.Xml.Linq;
using NugetReport.Factories;
using NugetReport.Objects;

namespace NugetReport;

public class App(NugetReportParserFactory factory)
{
    public Task RunAsync()
    {
        var workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");

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
        
        var packageFile = Directory.GetFiles(workspace, "Directory.Packages.props", SearchOption.AllDirectories).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(packageFile)) Console.WriteLine($"Directory.Packages.props detected!");
        
        var projectFiles = Directory.GetFiles(workspace, "*.csproj", SearchOption.AllDirectories);
        Console.WriteLine($"Found {projectFiles.Length} projects.");
        
        var context = new DotnetContext
        {
            CentralizedPackageFile = packageFile,
            ProjectFiles = projectFiles
        };

        var markdown = factory.GetParser(context).Process(context);
        
        var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrWhiteSpace(summaryPath))
        {
            File.AppendAllText(summaryPath, markdown);
            Console.WriteLine($"Report written to GitHub Actions summary: {summaryPath}");
        }
        else
        {
            var localReport = Path.Combine(workspace, "NugetReport.md");
            File.WriteAllText(localReport, markdown);
            Console.WriteLine($"Report written to: {localReport}");
        }

        return Task.CompletedTask;
    }
}