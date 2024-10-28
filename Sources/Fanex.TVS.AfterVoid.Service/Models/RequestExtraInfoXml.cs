using System.Xml.Serialization;

namespace Fanex.TVS.AfterVoid.Service.Models;

[XmlRoot(ElementName = "r")]
public class RequestExtraInfoXml
{
    // Wrong Offered Name
    [XmlAttribute(AttributeName = "CurrencyIds")]
    public string CurrencyIds { get; set; }

    [XmlAttribute(AttributeName = "LicenseeNames")]
    public string LicenseeNames { get; set; }

    [XmlAttribute(AttributeName = "WrongName")]
    public string WrongName { get; set; }

    [XmlAttribute(AttributeName = "ActualName")]
    public string ActualName { get; set; }

    [XmlAttribute(AttributeName = "WrongNameT")]
    public string WrongNameT { get; set; }

    [XmlAttribute(AttributeName = "ActualNameT")]
    public string ActualNameT { get; set; }

    [XmlAttribute(AttributeName = "WrongNameS")]
    public string WrongNameS { get; set; }

    [XmlAttribute(AttributeName = "ActualNameS")]
    public string ActualNameS { get; set; }
}