using EPiServer.Data.Dynamic;
using Hephaestus.CMS.DataAccess.DDS;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    public class RoyalMintBankAccountDetailRepository : GenericRepository<RoyalMintBankAccountDetail>
    {
        public RoyalMintBankAccountDetailRepository(DynamicDataStoreFactory storeFactory) : base(storeFactory)
        {
        }
    }
}