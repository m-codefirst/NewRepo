using EPiServer.Core;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.ViewModels;

namespace TRM.Web.Helpers
{
    public interface IAmNavigationHelper
    {
        void CreateBreadcrumb(ITrmLayoutModel layoutModel, IContent iContent);
        BreadcrumbViewModel CreateBreadcrumbViewModel(ITrmLayoutModel layoutModel, IContent iContent);
        void CreateAutomaticLeftMenu(ITrmLayoutModel layoutModel, IContent iContent);
        void CreateManualLeftMenu(ITrmLayoutModel layoutModel, IControlLeftNav icontrolLeftNav);
        void CreateMegaMenu(ITrmLayoutModel layoutModel);
        void CreateMyAccountHoverMenu(ITrmLayoutModel layoutModel);
        TrmNavigationBlockViewModel GetTrmNavigationBlockViewModel(TrmNavigationBlock currentBlock);
    }
}