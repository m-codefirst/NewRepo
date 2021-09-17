using System.Net;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    public class SecurityInitializationModule : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public void Uninitialize(InitializationEngine context)
        {
            
        }
    }
}