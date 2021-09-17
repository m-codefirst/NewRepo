using EPiServer.Data.Dynamic;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.DataAccess.DDS;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    public class PampMetalRepository : GenericRepository<PampMetal>
    {
        public PampMetalRepository() : base(ServiceLocator.Current.GetInstance<DynamicDataStoreFactory>())
        {
        }
    }
}