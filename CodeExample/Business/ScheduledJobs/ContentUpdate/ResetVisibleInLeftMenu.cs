using System;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using TRM.Web.Models.Pages;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.ContentUpdate
{
    [ScheduledPlugIn(
        DisplayName = "Reset visible in left menu", 
        Description = "Sets all CMS and Commerce content to show in the left navigation.")]
    public class ResetVisibleInLeftMenu : ScheduledJobBase
    {
        private bool _stopSignaled;

        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentRepository _contentRepository;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public ResetVisibleInLeftMenu(IContentLoader contentLoader, ReferenceConverter referenceConverter, IContentRepository contentRepository)
        {
            IsStoppable = true;
            _contentLoader = contentLoader;
            _referenceConverter = referenceConverter;
            _contentRepository = contentRepository;
        }

        /// <summary>
        /// Called when a user clicks on Stop for a manually started job, or when ASP.NET shuts down.
        /// </summary>
        public override void Stop()
        {
            _stopSignaled = true;
        }

        /// <summary>
        /// Called when a scheduled job executes
        /// </summary>
        /// <returns>A status message to be stored in the database log and visible from admin mode</returns>
        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged($"Starting execution of {this.GetType()}");

                //Add implementation
                var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
                var childPageContentReferences = _contentLoader.GetDescendents(startPage.ContentLink);
                var pagesChanged = 0;

                foreach (var childPageReference in childPageContentReferences.Where(c => c.ID > 0))
                {
                    TRMPage content;
                    try
                    {
                        content = _contentLoader.Get<TRMPage>(childPageReference);
                    }
                    catch (TypeMismatchException)
                    {
                        continue;
                    }

                    if (content == null) continue;

                    var clone = content.CreateWritableClone();
                    clone.Property["VisibleInLeftMenu"].Value = true;

                    _contentRepository.Save(clone, SaveAction.Publish, AccessLevel.NoAccess);

                    pagesChanged += 1;

                    //For long running jobs periodically check if stop is signaled and if so stop execution
                    if (_stopSignaled)
                    {
                        return "Stop of job was called";
                    }
                }

                var catalogs = _contentLoader.GetChildren<CatalogContentBase>(_referenceConverter.GetRootLink()).ToList();
                var childCategoryReferences = _contentLoader.GetDescendents(catalogs.First().ContentLink);
                var catalogContentChanged = 0;

                foreach (var childCategoryReference in childCategoryReferences.Where(c => c.ID > 0))
                {
                    CatalogContentBase content;
                    try
                    {
                        content = _contentLoader.Get<CatalogContentBase>(childCategoryReference);
                    }
                    catch (TypeMismatchException)
                    {
                        continue;
                    }

                    if (content == null) continue;

                    var clone = content.CreateWritableClone();
                    clone.Property["VisibleInLeftMenu"].Value = true;

                    _contentRepository.Save(clone, SaveAction.Publish, AccessLevel.NoAccess);

                    catalogContentChanged += 1;

                    //For long running jobs periodically check if stop is signaled and if so stop execution
                    if (_stopSignaled)
                    {
                        return "Stop of job was called";
                    }
                }

                return $"Display in left Navigation has been set to true for the CMS and Catalog content. CMS changes {pagesChanged} Catalog changes {catalogContentChanged}";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}
