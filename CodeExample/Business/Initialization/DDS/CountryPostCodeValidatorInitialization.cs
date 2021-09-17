using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class CountryPostCodeValidatorInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var skipThisMess =
                ConfigurationManager.AppSettings["Maginus:SkipCountryPostCodeValidatorInitialization"] != null &&
                ConfigurationManager.AppSettings["Maginus:SkipCountryPostCodeValidatorInitialization"].ToLower() ==
                "true";
            if (skipThisMess) return;

            var configList = GetValidatorsFromConfig().ToList();
            if (!configList.Any()) return;

            using (var repository = ServiceLocator.Current.GetInstance<IRepository<CountryPostCodeValidator>>())
            {
                foreach (var postCodeValidator in configList)
                {
                    if (repository.Find(x => x.CountryCode == postCodeValidator.CountryCode).Any()) continue;
                    if (postCodeValidator.Id == Guid.Empty)
                    {
                        postCodeValidator.Id = Guid.NewGuid();
                    }

                    repository.Save(postCodeValidator);
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            //Add uninitialization logic
        }

        private static IEnumerable<CountryPostCodeValidator> GetValidatorsFromConfig()
        {
            var postcodeValidatorJsonPath = CombinePath("Configs/postcodeValidators.json");
            return File.Exists(postcodeValidatorJsonPath) ? Newtonsoft.Json.JsonConvert.DeserializeObject<IList<CountryPostCodeValidator>>(File.ReadAllText(postcodeValidatorJsonPath)) : new List<CountryPostCodeValidator>();
        }

        private static string CombinePath(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }
    }
}