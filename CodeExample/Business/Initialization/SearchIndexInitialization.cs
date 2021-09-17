using EPiServer.Find;
using EPiServer.Find.ClientConventions;
using EPiServer.Find.Framework;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using TRM.Web.Extentions;
using TRM.Web.Models.Catalog;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class SearchIndexInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            try
            {
                SearchClient.Instance.Conventions.ForInstancesOf<TrmVariant>()
                    .IncludeField(x => x.GetDefaultAssetUrl())
                    .IncludeField(x => x.GetAssociatedReferences())
                    .IncludeField(x => x.DisplayPrice())
                    .IncludeField(x => x.StockStatus())
                    .IncludeField(x => x.StockStatusForOrder());
            }
            catch (ServiceException)
            {
                //
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            //Add uninitialization logic
        }
    }
}