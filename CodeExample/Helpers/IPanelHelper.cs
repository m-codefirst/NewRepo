using TRM.Web.Models.Blocks;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface IPanelHelper
    {
        PanelViewModel GetPanelViewModel(PanelBlock panel);
    }
}