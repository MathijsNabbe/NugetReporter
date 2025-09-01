namespace NugetReport.Interfaces;

public interface INugetReportParser
{
    bool CentralizedPackageManagement { get; }
    string Process(string packageFile, string[] projectFiles);
}