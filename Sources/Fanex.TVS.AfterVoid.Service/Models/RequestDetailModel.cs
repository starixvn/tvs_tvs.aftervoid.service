using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Fanex.TVS.AfterVoid.Service.Extensions;
using TicketVoidingSystem.Utilities;

namespace Fanex.TVS.AfterVoid.Service.Models;

public class RequestModel
{
    public int IssueTypeId { get; set; }
    public int ActionId { get; set; }
    public string MistakenBy { get; set; }
    public string DecidedBy { get; set; }
    public string OperatorInfo { get; set; }
    public string Provider { get; set; }
    public string Source { get; set; }
    public string Remark { get; set; }
    public bool IsTest { get; set; }
    public bool IsVoided { get; set; }

    public List<OperatorQuery> Operators
    {
        get
        {
            var root = OperatorInfo.ParseXmlToObject<RootOperators>();
            var operators = root.OperatorXmls;
            var result = operators.Count > 0 ? operators.Select(x =>
                new OperatorQuery { OperatorId = x.OperatorId, OperatorType = x.TypeId }
            ).ToList() : new List<OperatorQuery>();
            return result;
        }
    }

    public string RequestedBy { get; set; }
    public string VoidedBy { get; set; }
    public bool IsHasResettledTicket { get; set; }
}

[XmlRoot(ElementName = "r")]
public class OperatorXml
{
    [XmlAttribute(AttributeName = "OperatorId")]
    public int OperatorId { get; set; }

    [XmlAttribute(AttributeName = "TypeId")]
    public byte TypeId { get; set; }
}

[XmlRoot(ElementName = "root")]
public class RootOperators
{
    public RootOperators()
    {
        OperatorXmls = new List<OperatorXml>();
    }

    [XmlElement(ElementName = "r")]
    public List<OperatorXml> OperatorXmls { get; set; }
}

public class RequestEventModel
{
    public int MatchId { get; set; }
    public int SubIssueTypeId { get; set; }
    public DateTime EventDate { get; set; }
    public int ParentId { get; set; }
    public int LeagueId { get; set; }
    public int HomeId { get; set; }
    public int AwayId { get; set; }
    public string TransIds { get; set; }

    [JsonConverter(typeof(Gmt8DateTimeJsonConverter))]
    public DateTime? KickOffTime { get; set; }

    public int? HTHomeScore { get; set; }
    public int? HTAwayScore { get; set; }
    public int? FinalHomeScore { get; set; }
    public int? FinalAwayScore { get; set; }
    public DateTime GlobalShowTime { get; set; }
    public int? FilterBy { get; set; }
    public int SportType { get; set; }
}