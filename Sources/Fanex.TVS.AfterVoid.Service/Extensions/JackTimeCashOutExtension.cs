using System;
using System.IO;
using System.Xml;
using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class JackTimeCashOutExtension
{
    public static bool IsHasJackTimeCashOut(int sportType, string extraInfo)
    {
        return sportType is SportTypeConstant.Basketball
               || sportType is SportTypeConstant.Soccer && IsHasJackTimeText(extraInfo);
    }

    private static bool IsHasJackTimeText(string extraInfo)
    {
        if (string.IsNullOrWhiteSpace(extraInfo))
        {
            return false;
        }

        try
        {
            using var reader = XmlReader.Create(new StringReader(extraInfo),
                new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment });

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "p25")
                {
                    return reader.MoveToAttribute("jtseqid");
                }
            }
        }
        catch (Exception e)
        {
            return false;
        }

        return false;
    }
}