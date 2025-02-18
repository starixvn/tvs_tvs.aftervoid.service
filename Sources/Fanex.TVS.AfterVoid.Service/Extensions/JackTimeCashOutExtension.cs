using System;
using System.IO;
using System.Xml;
using Fanex.TVS.AfterVoid.Service.Constants;

namespace Fanex.TVS.AfterVoid.Service.Extensions;

public static class JackTimeCashOutExtension
{
    public static bool IsHasJackTimeCashOut(int sportType, string extraInfo)
    {
        if (!sportType.IsSportTypeOfferJackTimeCashOut() || string.IsNullOrWhiteSpace(extraInfo))
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

    private static bool IsSportTypeOfferJackTimeCashOut(this int sportType)
    {
        return sportType is SportTypeConstant.Soccer or SportTypeConstant.Basketball;
    }
}