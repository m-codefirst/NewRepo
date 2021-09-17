using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Hephaestus.CMS.DataAccess;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.Initialization.DDS
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class PampMetalsInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var configList = GetValidatorsFromConfig().ToList();
            if (!configList.Any()) return;

            using (var repository = ServiceLocator.Current.GetInstance<IRepository<PampMetal>>())
            {
                foreach (var metal in configList)
                {
                    if (repository.Find(x => x.Code == metal.Code).Any()) continue;
                    if (metal.Id == Guid.Empty)
                    {
                        metal.Id = Guid.NewGuid();
                    }

                    repository.Save(metal);
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            //Add uninitialization logic
        }

        private static IEnumerable<PampMetal> GetValidatorsFromConfig()
        {
            var pampMetalsJsonPath = CombinePath("Configs/pampMetals.json");
            return File.Exists(pampMetalsJsonPath)
                ? Newtonsoft.Json.JsonConvert.DeserializeObject<IList<PampMetal>>(File.ReadAllText(pampMetalsJsonPath))
                : new List<PampMetal>();
        }

        private static string CombinePath(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}