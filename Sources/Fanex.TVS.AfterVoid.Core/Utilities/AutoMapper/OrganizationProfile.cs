using AutoMapper;
using Fanex.BetList.Core.Entities;
using Fanex.TVS.AfterVoid.Core.SendEmail.Models;

namespace Fanex.TVS.AfterVoid.Core.Utilities.AutoMapper
{
    public class OrganizationProfile : Profile
    {
        public OrganizationProfile()
        {
            CreateMap<TicketEmailModel, Ticket>();
        }
    }
}