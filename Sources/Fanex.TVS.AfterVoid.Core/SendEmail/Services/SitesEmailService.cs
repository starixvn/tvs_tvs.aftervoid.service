using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Fanex.Data.Repository;
using Fanex.TVS.AfterVoid.Core.Models;
using Fanex.TVS.AfterVoid.Core.SendEmail.Constants;
using Fanex.TVS.AfterVoid.Core.SendEmail.Criteria;
using Fanex.TVS.AfterVoid.Core.Utilities;

namespace Fanex.TVS.AfterVoid.Core.SendEmail.Services
{
    public interface SitesEmailService
    {
        List<SiteEmail> GetSites();

        List<SiteEmail> GetSitesByCategory(bool isAbnormalBet);

        SiteEmail GetSiteByName(List<SiteEmail> sites, string name);
    }

    public class SitesEmailServiceImp : SitesEmailService
    {
        private readonly IDynamicRepository repository;

        public SitesEmailServiceImp(
            IDynamicRepository repository)
        {
            this.repository = repository;
        }

        public List<SiteEmail> GetSites()
        {
            var mailingListCriteria = new MailingListCriteria();
            var sites = repository.Fetch<SiteEmail>(mailingListCriteria).ToList();
            sites = ExcludeTestSites(sites);
            HandleEmailConfiguration(sites);

            return sites;
        }

        public List<SiteEmail> GetSitesByCategory(bool isAbnormalBet)
        {
            var category = isAbnormalBet ? CategoryEmail.AbNormalBet : CategoryEmail.BetDeletionReport;
            var mailingListCriteria = new MailingListCriteria { CategoryId = category };
            var sites = repository.Fetch<SiteEmail>(mailingListCriteria).ToList();
            sites = ExcludeTestSites(sites);
            HandleEmailConfiguration(sites);

            return sites;
        }

        private static List<SiteEmail> ExcludeTestSites(List<SiteEmail> sites)
        {
            var testSiteNames = new[] { "spondemo", "athena000" };
            return sites.Where(s => !testSiteNames.Any(t => t.Equals(s.Site, StringComparison.CurrentCultureIgnoreCase)))
                        .ToList();
        }

        private static void HandleEmailConfiguration(List<SiteEmail> sites)
        {
            var jsonPath = ConfigurationManager.AppSettings["MailingList"];
            var specialSites = Helper.DeserializeObject<List<SiteEmail>>(jsonPath);
            // 9999999 convert to hex (Underscores should be used to make large numbers readable -- code smell)
            const int SiteId = 0x98967F;
            var specialSite = specialSites.Single(x => x.SiteId == SiteId);
            specialSites.Remove(specialSite);

            sites.ForEach(site => site.BccList = specialSite.BccList);
            AddCCList(sites, specialSite);

            sites.AddRange(specialSites);
        }

        private static void AddCCList(List<SiteEmail> sites, SiteEmail specialSite)
        {
            sites.ForEach(site =>
            {
                if (!string.IsNullOrWhiteSpace(specialSite.CcList))
                {
                    site.CcList = string.IsNullOrWhiteSpace(site.CcList) ? specialSite.CcList : $"{site.CcList};{specialSite.CcList}";
                }
            });
        }

        public SiteEmail GetSiteByName(List<SiteEmail> sites, string name)
        {
            var item = sites.Where(site => site.Site.Equals(name)).ToList();
            return item.Count == 0 ? new SiteEmail() : item[0];
        }
    }
}