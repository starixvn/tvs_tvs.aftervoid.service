using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Fanex.Logging;
using Newtonsoft.Json;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class StringExtension
{
    public static T XmlToJson<T>(this string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        var jsonText = JsonConvert.SerializeXmlNode(doc);
        return JsonConvert.DeserializeObject<T>(jsonText);
    }

    public static T ParseXmlToObject<T>(this string xml) where T : new()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                return new T();
            }

            using var reader = new StringReader(xml);
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(reader);
        }
        catch (Exception e)
        {
            var message = $"{e.Message}{Environment.NewLine}*xml: {xml}";
            LogService.Error(message, e);

            return new T();
        }
    }
}