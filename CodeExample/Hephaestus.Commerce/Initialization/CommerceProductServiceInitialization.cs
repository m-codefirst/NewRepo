using System;
using System.Configuration;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace Hephaestus.Commerce.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class CommerceProductServiceInitialization : IInitializableModule
    {
// ReSharper disable once InconsistentNaming
        public enum eLoggingLevel
        {
            Normal,
            Verbose
        };

        private bool _initialized;

        public const eLoggingLevel DefaultLoggingLevel = eLoggingLevel.Normal;

        public const string LoggingLevelSettingName = "CommerceProductServiceDefaultLoggingLevel";

        public static eLoggingLevel LoggingLevel;

        public void Initialize(InitializationEngine context)
        {
            if (_initialized) return;
            SetLoggingLevelFromConfig();
            _initialized = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
            _initialized = false;
        }

        public void Preload(string[] parameters)
        { }

        private static void SetLoggingLevelFromConfig()
        {
            eLoggingLevel settingValue;
            var setting = ConfigurationManager.AppSettings[LoggingLevelSettingName];
            if (string.IsNullOrWhiteSpace(setting) || !Enum.TryParse(setting, out settingValue))
            {
                LoggingLevel = DefaultLoggingLevel;
                return;
            }
            LoggingLevel = settingValue;
        }
    }
}
