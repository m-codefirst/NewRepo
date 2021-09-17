using EPiServer.Data.Dynamic;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.DataAccess.DDS;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    public class CountryRepository : GenericRepository<Country>
    {
        public CountryRepository() : base(ServiceLocator.Current.GetInstance<DynamicDataStoreFactory>())
        {
        }
    }
}