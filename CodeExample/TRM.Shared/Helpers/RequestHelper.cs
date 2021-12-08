using System;
using System.Linq;
using System.Net;
using System.Web;
using TRM.Shared.Constants;
using TRM.Shared.Models;

namespace TRM.Shared.Helpers
{
    public static class RequestHelper
    {
        public static string GetClientIpAddress()
        {
            if (HttpContext.Current?.Request?.ServerVariables["HTTP_X_FORWARDED_FOR"] == null ||
                string.IsNullOrWhiteSpace(HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString()) ||
                string.IsNullOrWhiteSpace(HttpContext.Current.Request.UserHostAddress))
            {
                return StringConstants.DefaultIpAddress;
            }

            var userHostAddress = HttpContext.Current.Request.UserHostAddress.ToString();
            var xForwardedFor = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString();

            return GetIpAddress(userHostAddress, xForwardedFor);
        }

        public static string GetClientIpAddress(this HttpRequestBase request)
        {
            if (request == null) return StringConstants.DefaultIpAddress;

            return GetIpAddress(request.UserHostAddress, request.ServerVariables["HTTP_X_FORWARDED_FOR"]);
        }

        public static string GetClientIpAddress(this IpAddressModel model)
        {
            if (model == null || (string.IsNullOrWhiteSpace(model.UserHostAddress) && string.IsNullOrWhiteSpace(model.XForwardedFor))) return StringConstants.DefaultIpAddress;

            return GetIpAddress(model.UserHostAddress, model.XForwardedFor);
        }

        private static string GetIpAddress(string userHostAddress, string xForwardedFor)
        {
            try
            {

                if (string.IsNullOrEmpty(xForwardedFor))
                {
                    IPAddress.Parse(userHostAddress);
                    return userHostAddress;
                }

                // Get a list of public ip addresses in the X_FORWARDED_FOR variable
                var publicForwardingIps = xForwardedFor.Split(',').ToList();

                if (publicForwardingIps.Any())
                {
                    return publicForwardingIps.FirstOrDefault();
                }
                else
                {
                    IPAddress.Parse(userHostAddress);
                    return userHostAddress;
                }
            }
            catch (Exception)
            {
                return StringConstants.DefaultIpAddress;
            }
        }

    }
}
