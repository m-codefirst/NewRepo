using System.Collections.Generic;
using System.Linq;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.DataAccess;
using TRM.Web.Models.Catalog.DDS;

namespace TRM.Web.Business.Initialization.DDS
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    public class BrandInitializationModule : IInitializableModule
    {
        private readonly List<Brand> _brands = new List<Brand>
        {
            new Brand {DisplayName = "not-set", Value = "" }
        };

        public void Initialize(InitializationEngine context)
        {
            using (var repository = ServiceLocator.Current.GetInstance<IRepository<Brand>>())
            {
                if (repository.FindAll().Any()) return;

                foreach (var brand in _brands)
                {
                    if (!repository.Find(x => x.Value == brand.Value).Any())
                    {
                        repository.Save(brand);
                    }
                }
            }
        }

        public void Preload(string[] parameters) { }

        public void Uninitialize(InitializationEngine context) { }
    }
}