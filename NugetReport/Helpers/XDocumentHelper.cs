using System.Xml;
using System.Xml.Linq;

namespace NugetReport.Helpers;

public static class XDocumentHelper
{
    public static XDocument? LoadXmlSafely(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        
        XDocument? xml;
        try
        {
            xml = XDocument.Load(filePath);
        }
        catch (XmlException e)
        {
            Console.WriteLine($"{fileName} is a malformed xml file or has an unsupported encoding.");
            return null;
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine($"{fileName} does not exist.");
            return null;
        }

        return xml;
    }
}