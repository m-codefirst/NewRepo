using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;
using log4net;

namespace TRM.Web.Helpers
{
    public static class AppSettingsHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AppSettingsHelper));

        public static T GetValue<T>(this NameValueCollection nameValuePairs, string configKey, T defaultValue, string errorLogMessage = null) where T : IConvertible
        {
            T retValue = defaultValue;

            if (nameValuePairs.AllKeys.Contains(configKey))
            {
                string tmpValue = nameValuePairs[configKey];

                retValue = (T)Convert.ChangeType(tmpValue, typeof(T));
            }
            else
            {
                Logger.Error(errorLogMessage ?? ("Cannot find key in AppSettings : " + configKey));
                return retValue;
            }

            return retValue;
        }

        public static int RecommendedItem
        {
            get
            {
                var settingValue = ConfigurationManager.AppSettings["RecommendedItem"];
                int recommendedItem;
                if (int.TryParse(Convert.ToString(settingValue), out recommendedItem))
                    return recommendedItem;

                return -1;
            }
        }
    }
}