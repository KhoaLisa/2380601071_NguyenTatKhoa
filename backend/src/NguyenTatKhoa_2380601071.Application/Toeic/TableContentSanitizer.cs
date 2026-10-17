using Abp.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NguyenTatKhoa_2380601071.Toeic;

/// <summary>The editor stores only table markup; all other reading content is rendered as text.</summary>
public static class TableContentSanitizer
{
    private static readonly HashSet<string> Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "table", "thead", "tbody", "tfoot", "tr", "th", "td", "br" };

    public static string Normalize(string content)
    {
        try
        {
            using var reader = XmlReader.Create(new System.IO.StringReader(content ?? ""), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1000000
            });
            var table = XElement.Load(reader);
            if (table.Name != "table") throw new XmlException();
            foreach (var element in table.DescendantsAndSelf())
            {
                if (element.Name.Namespace != XNamespace.None || !Tags.Contains(element.Name.LocalName)) throw new XmlException();
                foreach (var attribute in element.Attributes().ToArray())
                {
                    if ((attribute.Name == "rowspan" || attribute.Name == "colspan") &&
                        int.TryParse(attribute.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var span) && span >= 1 && span <= 100)
                        continue;
                    throw new XmlException();
                }
            }
            if (table.DescendantNodes().Any(x => x is XProcessingInstruction || x is XComment)) throw new XmlException();
            return table.ToString(SaveOptions.DisableFormatting);
        }
        catch (XmlException)
        {
            throw new UserFriendlyException("Bảng chỉ được chứa table, thead/tbody/tfoot, tr, th/td và br; không cho phép script, liên kết hoặc thuộc tính trình duyệt.");
        }
    }
}
