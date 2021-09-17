using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using TRM.Web.Constants;

namespace TRM.Web.Business.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class RoutesInitializationModule : IInitializableModule
    {
        private bool _initialized;
        public void Initialize(InitializationEngine context)
        {
            if (_initialized) return;
            RouteTable.Routes.MapRoute(StringConstants.Routing.SignUp, StringConstants.Routing.NewsletterSignUp, new { controller = StringConstants.Routing.FooterSignUpBlock, action = StringConstants.Routing.SignUp });
            RouteTable.Routes.MapRoute(StringConstants.Routing.EditDetails, StringConstants.Routing.MyAccountEditDetails, new { controller = StringConstants.Routing.MyAccountEditDetails, action = StringConstants.Routing.EditDetails });
            RouteTable.Routes.MapRoute(StringConstants.Routing.GetProductListingData, StringConstants.Routing.GetProductListingData, new { controller = StringConstants.Routing.SearchPage, action = StringConstants.Routing.GetProductListingData });
            RouteTable.Routes.MapRoute(StringConstants.Routing.GetData, StringConstants.Routing.SearchPage, new { controller = StringConstants.Routing.SearchPage, action = StringConstants.Routing.GetData });
            //RouteTable.Routes.MapRoute(StringConstants.Routing.Listing, "en-gb/home/search-page/Listing", new { controller = StringConstants.Routing.SearchPage, action = StringConstants.Routing.Listing });
            RouteTable.Routes.MapRoute(StringConstants.Routing.LoadMore, StringConstants.Routing.ArticleListingBlock, new { controller = StringConstants.Routing.ArticleListingBlock, action = StringConstants.Routing.LoadMore });
            RouteTable.Routes.MapRoute(StringConstants.Routing.CertificateSignUp, StringConstants.Routing.CertificateSign, new { controller = StringConstants.Routing.SovereignCertificateSignUpBlock, action = StringConstants.Routing.CertificateSignUp });

            RouteTable.Routes.MapRoute("ImpersonationLogIndex", "ImpersonationLog", new { controller = "ImpersonationLog", action = "Index" });
            RouteTable.Routes.MapRoute("ImpersonationLogPayload", "ImpersonationLog/Payload", new { controller = "ImpersonationLog", action = "Payload" });

            RouteTable.Routes.MapRoute("LogViewerIndex", "LogViewer", new { controller = "LogViewer", action = "Index" });
            RouteTable.Routes.MapRoute("LogViewerLogFileStream", "LogViewer/LogFileStream", new { controller = "LogViewer", action = "LogFileStream" });

            RouteTable.Routes.MapRoute("TestsHelperAdminPanelIndex", "TestsHelperAdminPanel", new { controller = "TestsHelperAdminPanel", action = "Index" });
            RouteTable.Routes.MapRoute("TestsHelperAdminPanelGenerateAdminUsers", "TestsHelperAdminPanel/GenerateAdminUsers", new { controller = "TestsHelperAdminPanel", action = "GenerateAdminUsers" });
            RouteTable.Routes.MapRoute("TestsHelperAdminPanelGenerateTestUsers", "TestsHelperAdminPanel/GenerateTestUsers", new { controller = "TestsHelperAdminPanel", action = "GenerateTestUsers" });
            
            RouteTable.Routes.MapRoute("404", "404", new { controller = "ErrorPage", action = "NotFound" });
            RouteTable.Routes.MapRoute("500", "500", new { controller = "ErrorPage", action = "SystemError" });

            #region API Controllers
            #region GET
            RouteTable.Routes.MapHttpRoute("trmintegrationservicesOrders", "trmintegrationservices/getorders", new { controller = "EpiServerOrders", action = "GetSalesOrders" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicescustomerstocreate", "trmintegrationservices/getcustomerstocreate", new { controller = "EpiServerCustomer", action = "GetCustomersToCreate" });
            RouteTable.Routes.MapHttpRoute("trmintegrationservicescustomerstoupdate", "trmintegrationservices/getcustomerstoupdate", new { controller = "EpiServerCustomer", action = "GetCustomersToUpdate" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicescreditpayments", "trmintegrationservices/getcreditpayments", new { controller = "EpiServerCreditPayments", action = "GetCreditPayments" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicesemailmessages", "trmintegrationservices/getemailmessages", new { controller = "EmailMessages", action = "GetEmailMessages" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservice_exporttransaction", "trmintegrationservices/GetExportTransactions", new { controller = "ExportTransaction", action = "GetExportTransactions" });
            RouteTable.Routes.MapHttpRoute("trmintegrationservice_updateexporttransaction", "trmintegrationservices/UpdateExportTransactions", new { controller = "ExportTransaction", action = "UpdateExportTransactions" });
            #endregion

            #region POST
            RouteTable.Routes.MapHttpRoute("trmintegrationservicesCustomer", "trmintegrationservices/customers", new { controller = "MaginusCustomer", action = "UpsertCustomer" });
            RouteTable.Routes.MapHttpRoute("trmintegrationservicesBalances", "trmintegrationservices/customerbalances", new { controller = "MaginusCustomer", action = "UpsertCustomerBalance" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicesUpdateOrders", "trmintegrationservices/updateorderstatus", new { controller = "EpiServerOrders", action = "UpdateOrderStatus" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicesupdatecustomerswithobsnumber", "trmintegrationservices/updatecustomerwithobsnumber", new { controller = "EpiServerCustomer", action = "UpdateCustomerWithObsNumber" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicesupdatecreditpayments", "trmintegrationservices/updatecreditpaymentstatus", new { controller = "EpiServerCreditPayments", action = "UpdateCreditPaymentStatus" });

            RouteTable.Routes.MapHttpRoute("trmintegrationservicesupdateemailmessages", "trmintegrationservices/updateemailmessagetatus", new { controller = "EmailMessages", action = "UpdateEmailMessageStatus" });
            #endregion
            #endregion

            _initialized = true;

        }

        public void Uninitialize(InitializationEngine context)
        {
            if (!_initialized) return;

            RouteTable.Routes.Remove(RouteTable.Routes[StringConstants.Routing.SignUp]);
            RouteTable.Routes.Remove(RouteTable.Routes[StringConstants.Routing.CertificateSignUp]);
            RouteTable.Routes.Remove(RouteTable.Routes[StringConstants.Routing.EditDetails]);

            _initialized = false;
        }


    }
}