using System;
using EPiServer.Core;
using EPiServer.Find.Cms;
using EPiServer.Find.Cms.Conventions;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using TRM.Web.Helpers.Find;
using TRM.Web.Models.Interfaces;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Find.Cms.Module.IndexingModule))]
    public class EPiServerFindInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            ContentIndexer.Instance.Conventions.ForInstancesOf<IContent>().ShouldIndex(x => true);
        }

        public void Uninitialize(InitializationEngine context)
        {
            throw new NotImplementedException();
        }
    }
}