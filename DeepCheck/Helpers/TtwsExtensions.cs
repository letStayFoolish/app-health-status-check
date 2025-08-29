using System.Xml.Linq;

namespace DeepCheck.Helpers;

public static class TtwsExtensions
{
    private static readonly XNamespace TtwsNamespace = "urn:schemas-teletrader-com:mb";

    public static XElement? TtwsElement(this XElement element, string name)
    {
        return element.Element(TtwsNamespace + name);
    }

    public static IEnumerable<XElement> TtwsElements(this XElement element, string name)
    {
        return element.Elements(TtwsNamespace + name);
    }
}
