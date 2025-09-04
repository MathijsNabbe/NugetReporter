using System.Text;
using NugetReport.Extensions;
using NugetReport.Factories;
using NugetReport.Helpers;
using NugetReport.Interfaces;
using NugetReport.Objects;

namespace NugetReport.Parsers.ReportParsers;

public class DefaultPackagesReportParser(ProjectParserFactory projectParserFactory) : INugetReportParser
{
    public bool CentralizedPackageManagement => false;

    public string Process(DotnetContext context)
    {
        // Load data
        var references = context.ProjectFiles
            .SelectMany(project =>
            {
                var xml = XDocumentHelper.LoadXmlSafely(project);
                if (xml == null)
                {
                    Console.WriteLine($"Skipping {Path.GetFileName(project)}.");
                    return [];
                }

                var framework =
                    xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFramework")?.Value ??
                    xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "TargetFrameworkVersion")?.Value;

                if (string.IsNullOrWhiteSpace(framework))
                {
                    Console.WriteLine($"No Framework detected, skipping {Path.GetFileName(project)}");
                    return [];
                }

                var parser = projectParserFactory.GetParser(framework);
                if (parser == null)
                {
                    Console.WriteLine($"Skipping {Path.GetFileName(project)}.");
                    return [];
                }
                    
                return parser.ParseNugetReferences(project);
            })
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
}