#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace DockerSqliteBackup.Formatters;

/// <summary>
/// Formats output as XML. Uses XDocument for clean XML generation and handling.
/// Properly escapes special characters and handles nested structures.
/// </summary>
public class XmlOutputFormatter : IOutputFormatter
{
    public string Name => "XML";
    public string FileExtension => ".xml";

    /// <summary>
    /// Formats a single object as XML.
    /// </summary>
    public string Format(object? value)
    {
        if (value  is null)
            return "<null />";

        var doc = new XDocument(ObjectToXElement("item", value));
        return doc.ToString();
    }

    /// <summary>
    /// Formats a collection as XML with items wrapped in a root element.
    /// </summary>
    public string FormatCollection(IEnumerable<object?> values)
    {
        var root = new XElement("items");

        foreach (var value in values)
        {
            if (value  is not null)
                root.Add(ObjectToXElement("item", value));
        }

        var doc = new XDocument(root);
        return doc.ToString();
    }

    /// <summary>
    /// Formats a dictionary as XML.
    /// </summary>
    public string FormatDictionary(Dictionary<string, object?> data)
    {
        var root = new XElement("data");

        foreach (var kvp in data)
        {
            var element = new XElement(SanitizeXmlElementName(kvp.Key));

            if (kvp.Value  is not null)
            {
                if (kvp.Value is IEnumerable<object?> collection && kvp.Value is not string)
                {
                    foreach (var item in collection)
                    {
                        if (item  is not null)
                            element.Add(ObjectToXElement("item", item));
                    }
                }
                else
                {
                    element.Value = kvp.Value.ToString() ?? "";
                }
            }

            root.Add(element);
        }

        var doc = new XDocument(root);
        return doc.ToString();
    }

    /// <summary>
    /// Converts an object to an XElement recursively.
    /// </summary>
    private static XElement ObjectToXElement(string elementName, object? obj)
    {
        var element = new XElement(SanitizeXmlElementName(elementName));

        if (obj  is null)
            return element;

        var type = obj.GetType();

        // Handle simple types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid))
        {
            element.Value = obj.ToString() ?? "";
            return element;
        }

        // Handle DateTime
        if (type == typeof(DateTime))
        {
            element.Value = ((DateTime)obj).ToString("O");
            return element;
        }

        // Handle collections
        if (obj is IEnumerable<object?> collection && type != typeof(string))
        {
            foreach (var item in collection)
            {
                if (item  is not null)
                    element.Add(ObjectToXElement("item", item));
            }

            return element;
        }

        // Handle complex types via reflection
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);

                if (value  is not null)
                {
                    var propElement = ObjectToXElement(SanitizeXmlElementName(property.Name), value);
                    element.Add(propElement);
                }
            }
            catch
            {
                // Skip properties that can't be read
            }
        }

        return element;
    }

    /// <summary>
    /// Sanitizes a string to be a valid XML element name.
    /// </summary>
    private static string SanitizeXmlElementName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "element";

        var sb = new StringBuilder();

        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                sb.Append(c);
            else if (c == ' ')
                sb.Append('_');
        }

        var result = sb.ToString();
        // XML element names can't start with a digit
        if (result.Length > 0 && char.IsDigit(result[0]))
            result = "_" + result;

        return string.IsNullOrEmpty(result) ? "element" : result;
    }
}
