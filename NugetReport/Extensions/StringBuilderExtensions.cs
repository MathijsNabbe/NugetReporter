using System.Text;

namespace NugetReport.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder SetReportTitle(this StringBuilder sb)
    {
        sb.AppendLine("# 📦 NuGet Package Report");
        sb.AppendLine();
        
        return sb;
    }
}