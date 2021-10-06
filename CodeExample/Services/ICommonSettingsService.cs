using TRM.Web.Models.Settings;

namespace TRM.Web.Services
{
    public interface ICommonSettingsService
    {
        GBCHSettings GetGBCHSettings();
    }
}