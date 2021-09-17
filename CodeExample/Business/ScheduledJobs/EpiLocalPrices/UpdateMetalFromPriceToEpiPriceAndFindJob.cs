using Castle.Core.Internal;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TRM.Web.Business.DataAccess;
using TRM.Web.Helpers;
using TRM.Web.Helpers.Find;
using TRM.Web.Models.Catalog.Bullion;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.EpiLocalPrices
{
    [ScheduledPlugIn(
        DisplayName = "[Epi Pricing] Update Metal Price To Epi Price And Find",
        Description = "Populate the From Price and push it as the Epi Price, trigger the Find to re-index the variant",
        SortIndex = 130)]
    public class UpdateMetalFromPriceToEpiPriceAndFindJob : ScheduledJobBase
    {
        private readonly IAmLocalPriceDataHelper _localBullionPriceDataHelper;
        private readonly IAmMarketHelper _marketHelper;
        private readonly IFindService _findService;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public UpdateMetalFromPriceToEpiPriceAndFindJob(
            IAmLocalPriceDataHelper amLocalBullionPriceDataHelper,
            IAmMarketHelper marketHelper,
            IFindService findService)
        {
            _localBullionPriceDataHelper = amLocalBullionPriceDataHelper;
            _marketHelper = marketHelper;
            _findService = findService;
        }

        public override void Stop()
        {
        }

        public override string Execute()
        {
            try
            {
                //Call OnStatusChanged to periodically notify progress of job for manually started jobs
                OnStatusChanged(String.Format("Starting execution of {0}", this.GetType()));
                var updatedVariantCount = 0;
                IEnumerable<PreciousMetalsVariantBase> updatedBullionVariants = Enumerable.Empty<PreciousMetalsVariantBase>(); ;
                IEnumerable<PreciousMetalsVariantBase> allBullionVariants = Enumerable.Empty<PreciousMetalsVariantBase>();
                string errorMessage = string.Empty;
                try
                {
                    allBullionVariants = _findService.GetAllContents<PreciousMetalsVariantBase>();
                    updatedBullionVariants = _localBullionPriceDataHelper.UpdateEpiPricesForBullionVariants(allBullionVariants);
                    if (!updatedBullionVariants.IsNullOrEmpty())
                    {
                        _findService.ReIndexContents(updatedBullionVariants);
                    }
                    updatedVariantCount = updatedBullionVariants.Count();
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                return BuildTheMessage(allBullionVariants, updatedBullionVariants, errorMessage);
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }

        private string BuildTheMessage(IEnumerable<PreciousMetalsVariantBase> allBullionVariants, IEnumerable<PreciousMetalsVariantBase> updatedBullionVariants, string errorMessage = "")
        {
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                stringBuilder.AppendLine(string.Format("Error when trying to update Epi Price for Bullion Variants: {0}", errorMessage));
            }
            stringBuilder.AppendLine(string.Format("Variants have  been updated successfully: {0}", string.Join(":", updatedBullionVariants.Select(x => x.Name))));
            stringBuilder.AppendLine(string.Format("Variants could not be updated: {0}", string.Join(":", allBullionVariants.Where(x => !updatedBullionVariants.Contains(x)).Select(y => y.Name))));
            return stringBuilder.ToString();
        }
    }
}