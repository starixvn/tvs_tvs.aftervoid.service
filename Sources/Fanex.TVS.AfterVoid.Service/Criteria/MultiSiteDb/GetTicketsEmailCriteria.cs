using System.Collections.Generic;
using System.Linq;
using Fanex.Data;
using Fanex.Data.Repository;
using TicketVoidingSystem.Constants;
using TicketVoidingSystem.Models;

namespace Fanex.TVS.AfterVoid.Service.Criteria.MultiSiteDb
{
    public class GetTicketsEmailCriteria : CriteriaBase
    {
        public int VoidingHistoryId { get; set; }

        public bool IsITSupport { get; set; }
        public bool IsITSupportCreatedRequest { get; set; }

        [SpParam(Ignored = true)]
        public int SiteDbGroupId { get; set; }

        public override string GetSettingKey() => SpList.First(x => x.Id == SiteDbGroupId).SpName;

        public override bool IsValid() => true;

        private static readonly IEnumerable<SiteDbGroupModel> SpList = new List<SiteDbGroupModel>
        {
            new () {Id = SiteDbGroupConstant.CentralDb, SpName = "DBCTG.TicketVoiding_Email_VoidedTicket_Select"},
            new () {Id = SiteDbGroupConstant.SiteDb1, SpName = "DBSIG.TicketVoiding_Email_VoidedTicket_Select"}
        };
    }
}