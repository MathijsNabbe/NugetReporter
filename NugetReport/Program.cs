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

var packageFile = GetPackageVersionFile(workspace);
var projectFiles = GetProjectFiles(workspace);

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