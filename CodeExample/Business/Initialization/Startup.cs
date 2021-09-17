using EPiServer.ServiceApi.Owin;
using Owin;

namespace TRM.Web.Business.Initialization
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Enable bearer token authentication using Membership for Service Api
            app.UseServiceApiMembershipTokenAuthorization();
        }
    }
}