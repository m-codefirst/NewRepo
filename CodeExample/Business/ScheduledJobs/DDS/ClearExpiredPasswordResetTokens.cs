using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using System;
using TRM.Web.Helpers;
using TRM.Web.Services;

namespace TRM.Web.Business.ScheduledJobs.DDS
{
    [ScheduledPlugIn(
        DisplayName = "Clear Expired Password Reset Tokens", 
        Description = "Removes all expired password reset tokens")]
    public class ClearExpiredPasswordResetTokens : ScheduledJobBase
    {
        private readonly IAmResetTokenHelper _resetTokenHelper;

        protected Lazy<IJobFailedHandler> FailedHandler = new Lazy<IJobFailedHandler>(() =>
        {
            return ServiceLocator.Current.GetInstance<IJobFailedHandler>();
        });

        public ClearExpiredPasswordResetTokens(IAmResetTokenHelper resetTokenHelper)
        {
            this._resetTokenHelper = resetTokenHelper;
        }
       
        public override string Execute()
        {
            try
            {
                var deleted = _resetTokenHelper.DeleteExpiredTokens();

                return $"Deleted: {deleted} Password Reset Tokens";
            }
            catch (Exception ex)
            {
                FailedHandler.Value.Handle(this.GetType().Name, ex);
                throw;
            }
        }
    }
}