using Hephaestus.CMS.DataAccess.DDS;
using EPiServer.Data.Dynamic;
using TRM.Web.Models.DDS;
using EPiServer.ServiceLocation;

namespace TRM.Web.Business.DataAccess
{
    public class CountryPostcodeValidatorRepository: GenericRepository<CountryPostCodeValidator>
    {
        public CountryPostcodeValidatorRepository() : base(ServiceLocator.Current.GetInstance<DynamicDataStoreFactory>())
        {
        }
    }
}