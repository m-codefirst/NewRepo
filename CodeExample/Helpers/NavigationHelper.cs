using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Framework.Localization;
using EPiServer.Web.Routing;
using Hephaestus.CMS.Extensions;
using Hephaestus.CMS.Models.Pages;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Customers;
using StackExchange.Profiling;
using TRM.Shared.Extensions;
using TRM.Web.Constants;
using TRM.Web.Models.Blocks;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;
using TRM.Web.Models.Interfaces.EntryProperties;
using TRM.Web.Models.Pages.Bullion;
using TRM.Web.Models.ViewModels;
using static TRM.Shared.Constants.StringConstants;
using static TRM.Web.Constants.StringConstants;

namespace TRM.Web.Helpers
{
    public class NavigationHelper : IAmNavigationHelper
    {
        private readonly IContentLoader _contentLoader;
        private readonly IAmEntryHelper _trmEntryHelper;
        private readonly LocalizationService _localizationService;
        private readonly MiniProfiler _miniProfiler;

        public NavigationHelper(IContentLoader contentLoader, IAmEntryHelper trmEntryHelper, LocalizationService localizationService)
        {
            _contentLoader = contentLoader;
            _trmEntryHelper = trmEntryHelper;
            _localizationService = localizationService;
            _miniProfiler = MiniProfiler.Current;
        }

        public void CreateBreadcrumb(ITrmLayoutModel layoutModel, IContent iContent)
        {
            using (_miniProfiler.Step("Create Breadcrumb"))
            {
                var currentStartPage = layoutModel.StartPageReference;

                var icontrolTrmBreadcrumbDisplay = iContent as IControlTrmBreadcrumbDisplay;
                if ((icontrolTrmBreadcrumbDisplay != null && icontrolTrmBreadcrumbDisplay.HideSiteBreadcrumb) ||
                    currentStartPage == null)
                {
                    return;
                }

                var breadcrumbItems = new List<MenuItemDto>
                {
                    GetAutomaticMenuItemDto(iContent, true)
                };

                var contentReferenceToUseForAncestors = iContent.ContentLink;

                #region do Commerce stuff

                if (iContent is CatalogContentBase)
                {
                    var parentContentReference = _trmEntryHelper.GetCategoryContentReference(iContent.ContentLink);
                    if (parentContentReference != null)
                    {
                        contentReferenceToUseForAncestors = parentContentReference;
                        var parentContentBase = _contentLoader.Get<CatalogContentBase>(parentContentReference);
                        var parentDto = GetAutomaticMenuItemDto(parentContentBase);
                        breadcrumbItems.Add(parentDto);
                    }
                    breadcrumbItems.AddRange(_contentLoader.GetAncestors(contentReferenceToUseForAncestors)
                        .OfType<CatalogContentBase>()
                        .Where(c => c.ContentType != CatalogContentType.Catalog &&
                                    c.ContentType != CatalogContentType.Root &&
                                    (c.Name.ToLower() != "old"))
                        .Select(x => GetAutomaticMenuItemDto(x)));
                }

                #endregion

                #region do Cms stuff

                if (iContent is PageData)
                {
                    breadcrumbItems.AddRange(_contentLoader.GetAncestors(contentReferenceToUseForAncestors)
                        .OfType<PageData>()
                        .Where(c => c.ContentLink != currentStartPage && c.ContentLink.ID != 1 && c.VisibleInMenu)
                        .Select(x => GetAutomaticMenuItemDto(x)));
                }

                #endregion

                var homeBreadCrumbItem = new MenuItemDto
                {
                    MenuItemDisplayName = _localizationService.GetString(StringResources.BreadCrumbHomeText, TranslationFallback.Home),
                    MenuItemExternalUrl = currentStartPage.GetExternalUrl_V2(),
                    IsActiveContent = false,
                };

                breadcrumbItems.Add(homeBreadCrumbItem);
                breadcrumbItems.Reverse();

                layoutModel.Breadcrumb = new BreadcrumbViewModel
                {
                    BreadcrumbMenuDto = new MenuItemDto
                    {
                        ChildNavigationItems = breadcrumbItems
                    }
                };
                var iControlTrmBreadcrumbDisplay = iContent as IControlTrmBreadcrumbDisplay;
                if (iControlTrmBreadcrumbDisplay != null)
                {
                    layoutModel.Breadcrumb.ShowOnXs = iControlTrmBreadcrumbDisplay.BreadcrumbShowOnXs;
                }
            }
        }
        public BreadcrumbViewModel CreateBreadcrumbViewModel(ITrmLayoutModel layoutModel, IContent iContent)
        {
            using (_miniProfiler.Step("Create Breadcrumb"))
            {
                var currentStartPage = layoutModel.StartPageReference;

                var icontrolTrmBreadcrumbDisplay = iContent as IControlTrmBreadcrumbDisplay;
                if ((icontrolTrmBreadcrumbDisplay != null && icontrolTrmBreadcrumbDisplay.HideSiteBreadcrumb) ||
                    currentStartPage == null)
                {
                    return null;
                }

                var breadcrumbItems = new List<MenuItemDto>
                {
                    GetAutomaticMenuItemDto(iContent, true)
                };

                var contentReferenceToUseForAncestors = iContent.ContentLink;

                #region do Commerce stuff

                if (iContent is CatalogContentBase)
                {
                    var parentContentReference = _trmEntryHelper.GetCategoryContentReference(iContent.ContentLink);
                    if (parentContentReference != null)
                    {
                        contentReferenceToUseForAncestors = parentContentReference;
                        var parentContentBase = _contentLoader.Get<CatalogContentBase>(parentContentReference);
                        var parentDto = GetAutomaticMenuItemDto(parentContentBase);
                        breadcrumbItems.Add(parentDto);
                    }
                    breadcrumbItems.AddRange(_contentLoader.GetAncestors(contentReferenceToUseForAncestors)
                        .OfType<CatalogContentBase>()
                        .Where(c => c.ContentType != CatalogContentType.Catalog &&
                                    c.ContentType != CatalogContentType.Root)
                        .Select(x => GetAutomaticMenuItemDto(x)));
                }

                #endregion

                #region do Cms stuff

                if (iContent is PageData)
                {
                    breadcrumbItems.AddRange(_contentLoader.GetAncestors(contentReferenceToUseForAncestors)
                        .OfType<PageData>()
                        .Where(c => c.ContentLink != currentStartPage && c.ContentLink.ID != 1 && c.VisibleInMenu)
                        .Select(x => GetAutomaticMenuItemDto(x)));
                }

                #endregion

                var homeBreadCrumbItem = new MenuItemDto
                {
                    MenuItemDisplayName = _localizationService.GetString(StringResources.BreadCrumbHomeText, TranslationFallback.Home),
                    MenuItemExternalUrl = currentStartPage.GetExternalUrl_V2(),
                    IsActiveContent = false,
                };

                breadcrumbItems.Add(homeBreadCrumbItem);
                breadcrumbItems.Reverse();

                var breadcrumb = new BreadcrumbViewModel
                {
                    BreadcrumbMenuDto = new MenuItemDto
                    {
                        ChildNavigationItems = breadcrumbItems
                    }
                };
                var iControlTrmBreadcrumbDisplay = iContent as IControlTrmBreadcrumbDisplay;
                if (iControlTrmBreadcrumbDisplay != null)
                {
                    breadcrumb.ShowOnXs = iControlTrmBreadcrumbDisplay.BreadcrumbShowOnXs;
                }

                return breadcrumb;
            }
        }
        public void CreateAutomaticLeftMenu(ITrmLayoutModel layoutModel, IContent iContent)
        {
            using (_miniProfiler.Step("Create Automatic Left Menu"))
            {
                var children = _contentLoader.GetChildren<IContent>(iContent.ContentLink);
                var leftMenuItems = new List<MenuItemDto>();
                foreach (var child in children)
                {
                    var pageData = child as PageData;
                    if (pageData != null)
                    {
                        if (!pageData.VisibleInMenu) continue;
                    }

                    var expanded = false;
                    var controlsMenuVisibility = child as IControlMenuVisibility;
                    if (controlsMenuVisibility != null)
                    {
                        if (!controlsMenuVisibility.VisibleInLeftMenu) continue;

                        var controlsLeftNav = child as IControlLeftNav;
                        if (controlsLeftNav != null)
                        {

                            expanded = controlsLeftNav.ShowMeAsExpandedInALeftNav;
                        }

                    }

                    var item = GetAutomaticMenuItemDto(child, false, expanded);
                    var grandChildren = _contentLoader.GetChildren<IContent>(child.ContentLink);

                    foreach (var grandChild in grandChildren)
                    {
                        var grandChildPageData = grandChild as PageData;
                        if (grandChildPageData != null)
                        {
                            if (!grandChildPageData.VisibleInMenu) continue;
                        }

                        var grandChildExpanded = false;

                        var grandChildControlsMenuVisibility = grandChild as IControlMenuVisibility;
                        if (grandChildControlsMenuVisibility != null)
                        {
                            if (!grandChildControlsMenuVisibility.VisibleInLeftMenu) continue;
                            var controlsLeftNav = child as IControlLeftNav;
                            if (controlsLeftNav != null)
                            {

                                grandChildExpanded = controlsLeftNav.ShowMeAsExpandedInALeftNav;
                            }
                        }

                        var grandChildItem = GetAutomaticMenuItemDto(grandChild, false, grandChildExpanded);

                        grandChildItem.ChildNavigationItems = GetAutomaticChildren(grandChild);

                        item.ChildNavigationItems.Add(grandChildItem);
                    }

                    leftMenuItems.Add(item);
                }

                layoutModel.LeftMenu = new LeftMenuViewModel
                {
                    AutomaticLeftMenuDto = new MenuItemDto
                    {
                        ChildNavigationItems = leftMenuItems
                    }
                };
            }
        }

        public void CreateManualLeftMenu(ITrmLayoutModel layoutModel, IControlLeftNav iControlLeftNav)
        {
            using (_miniProfiler.Step("Create Manual Left Menu"))
            {
                if (iControlLeftNav.LeftMenuManualContentArea?.FilteredItems == null || !iControlLeftNav.LeftMenuManualContentArea.FilteredItems.Any()) return;

                var contentAreaItem = iControlLeftNav.LeftMenuManualContentArea.FilteredItems.FirstOrDefault();
                if (contentAreaItem == null) return;

                ContentData contentData;
                if (!_contentLoader.TryGet(contentAreaItem.ContentLink, out contentData)) return;

                layoutModel.LeftMenu = new LeftMenuViewModel()
                {
                    ManualLeftMenuDto = GetManualMenuItemDto(contentData, Enums.eMenuType.PageLeft)
                };
            }
        }

        public void CreateMyAccountHoverMenu(ITrmLayoutModel layoutModel)
        {
            using (_miniProfiler.Step("Create My Account Hover Menu"))
            {
                if (layoutModel.MyAccountNavigation == null || layoutModel.MyAccountNavigation.FilteredItems == null ||
                    !layoutModel.MyAccountNavigation.FilteredItems.Any()) return;

                var contentAreaItem = layoutModel.MyAccountNavigation.FilteredItems.FirstOrDefault();

                if (contentAreaItem == null)
                    return;

                ContentData contentData;
                if (!_contentLoader.TryGet(contentAreaItem.ContentLink, out contentData)) return;

                layoutModel.MyAccountHoverMenu = new MyAccountHoverMenuViewModel()
                {
                    MyAccountMenuItemDto = GetManualMenuItemDto(contentData, Enums.eMenuType.PageLeft)
                };
            }

        }

        public void CreateMegaMenu(ITrmLayoutModel layoutModel)
        {
            using (_miniProfiler.Step("Create Mega Menu"))
            {
                layoutModel.MegaMenu = new MegaMenuViewModel
                {
                    MegaMenuDto = layoutModel.IsPensionContact ? GetPensionMegaMenu(layoutModel) : GetMegaMenu(layoutModel)
                };
            }
        }

        private MenuItemDto GetMegaMenu(ITrmLayoutModel layoutModel)
        {
            if (layoutModel.Navigation == null || layoutModel.Navigation.FilteredItems == null ||
                !layoutModel.Navigation.FilteredItems.Any()) return null;
            
            var contentAreaItem = layoutModel.Navigation.FilteredItems.FirstOrDefault();
            if (contentAreaItem == null) return null;

            ContentData contentData;
            if (!_contentLoader.TryGet(contentAreaItem.ContentLink, out contentData)) return null;

            return GetManualMenuItemDto(contentData, Enums.eMenuType.MegaMenu);
        }

        private MenuItemDto GetPensionMegaMenu(ITrmLayoutModel layoutModel)
        {
            using (_miniProfiler.Step("Create Pension Mega Menu"))
            {
                if (layoutModel.PensionNavigation == null || layoutModel.PensionNavigation.FilteredItems == null ||
                    !layoutModel.PensionNavigation.FilteredItems.Any()) return null;
                var contentAreaItem = layoutModel.PensionNavigation.FilteredItems.FirstOrDefault();
                if (contentAreaItem == null) return null;

                ContentData contentData;
                if (!_contentLoader.TryGet(contentAreaItem.ContentLink, out contentData)) return null;

                return GetManualMenuItemDto(contentData, Enums.eMenuType.MegaMenu);
            }
        }

        private MenuItemDto GetAutomaticMenuItemDto(IContent thisContent, bool isActivePage = false, bool showExpanded = false)
        {
            using (_miniProfiler.Step("Get Automatic Menu Item Dto"))
            {
                var containerPage = thisContent as IContainerPage;
                var isContainer = containerPage != null;
                var inNav = true;
                var inLeftNav = true;

                CheckMenuVisibility(thisContent, ref inLeftNav, ref inNav);
                AddCreditMenuVisibleForBullionAccount(thisContent, ref inNav, ref inLeftNav);
                return new MenuItemDto
                {
                    MenuItemDisplayName = GetAutomaticTrmNavigationBlockDisplayName(thisContent),
                    MenuItemExternalUrl = thisContent.ContentLink.GetExternalUrl_V2(),
                    ShowExpanded = showExpanded,
                    IsActiveContent = isActivePage,
                    IsContainer = isContainer,
                    VisibleInLeftNav = inLeftNav,
                    VisibleInNav = inNav,
                    ChildNavigationItems = new List<MenuItemDto>()
                };
            }
        }

        private void AddCreditMenuVisibleForBullionAccount(IContent thisContent, ref bool inNav, ref bool inLeftNav)
        {
            var currentContact = CustomerContext.Current.CurrentContact;
            if (!(thisContent is BullionAccountAddCreditPage)) return;

            //Hide add credit page for anonymous user.
            if (null == currentContact)
            {
                inLeftNav = inNav = false;
                return;
            }
            var customerType = currentContact.GetStringProperty(CustomFields.CustomerType);
            inLeftNav = inNav = (!string.IsNullOrEmpty(customerType) && (customerType == CustomerType.Bullion || customerType == CustomerType.ConsumerAndBullion));
        }

        private void CheckMenuVisibility(IContent thisContent, ref bool inLeftNav, ref bool inNav)
        {

            var inMenu = thisContent as IControlMenuVisibility;

            if (inMenu != null)
            {
                inNav = inMenu.VisibleInMenu;
                inLeftNav = inMenu.VisibleInLeftMenu;
            }

            //Pages
            var pageData = thisContent as PageData;
            if (pageData != null)
            {
                //Set left nav options for Article and Help Pages
                if (inMenu == null)
                {
                    inNav = pageData.VisibleInMenu;
                    inLeftNav = inNav;
                }

                if (pageData.CheckPublishedStatus(PagePublishedStatus.Published)) return;

                inNav = false;
                inLeftNav = false;

                return;
            }

            //Contents
            var controlMyMerchandising = thisContent as IControlMyMerchandising;
            if (controlMyMerchandising != null)
            {
                inNav = inNav && controlMyMerchandising.PublishOntoSite && !controlMyMerchandising.Restricted;
                inLeftNav = inLeftNav && controlMyMerchandising.PublishOntoSite && !controlMyMerchandising.Restricted;
            }
            else
            {
                var controlPublishedState = thisContent as IControlPublishedState;
                if (controlPublishedState == null) return;
                inNav = inNav && controlPublishedState.PublishOntoSite;
                inLeftNav = inLeftNav && controlPublishedState.PublishOntoSite;
            }

        }

        private string GetAutomaticTrmNavigationBlockDisplayName(IContent iContent)
        {
            var displayName = string.Empty;
            var thisEntryContent = iContent as EntryContentBase;
            if (thisEntryContent != null)
            {
                displayName = _trmEntryHelper.GetDisplayName(thisEntryContent.ContentLink);
            }
            var thisNodeContent = iContent as NodeContent;
            if (thisNodeContent != null)
            {
                displayName = thisNodeContent.DisplayName;
            }
            var thisPageData = iContent as PageData;
            if (thisPageData != null)
            {
                displayName = thisPageData.Name;
            }
            return displayName;
        }

        private MenuItemDto GetManualMenuItemDto(ContentData contentData, Enums.eMenuType menuType, int level = 0, bool isActivePage = false, bool showExpanded = false)
        {
            //contentData might not be a TrmNavigationBlock, for the bottom level of the mega menu, where we put the content item in a dto, but render it as itself in a 
            //content area (CurrentContentAreaItem)
            var trmNavigationBlock = contentData as TrmNavigationBlock;
            var displayName = string.Empty;
            var childNavigationItems = new List<MenuItemDto>();
            var isContainer = false;
            var itemUrl = string.Empty;
            var inNav = true;
            var inLeftNav = true;
            var openInANewWindow = false;
            var showViewAll = false;
            var viewAllText = string.Empty;
            var menuIcon = Enums.eMenuIcon.Basket;
            var description = string.Empty;

            if (trmNavigationBlock != null)
            {
                var iContent = trmNavigationBlock.RootLink != null && trmNavigationBlock.RootLink != null ? UrlResolver.Current.Route(new UrlBuilder(trmNavigationBlock.RootLink)) : null;
                if (!string.IsNullOrEmpty(trmNavigationBlock.RootLabel))
                {
                    displayName = trmNavigationBlock.RootLabel;
                }
                else
                {
                    displayName = GetAutomaticTrmNavigationBlockDisplayName(iContent);
                }
                if (trmNavigationBlock.RootLink != null)
                {
                    itemUrl = trmNavigationBlock.RootLink.Uri.ToString();
                }

                if (iContent != null)
                {
                    itemUrl = iContent.ContentLink.GetExternalUrl_V2();
                }

                openInANewWindow = trmNavigationBlock.OpenInNewWindow;
                showViewAll = trmNavigationBlock.ShowViewAll;
                viewAllText = trmNavigationBlock.ViewAllText;
                menuIcon = trmNavigationBlock.MenuIcon;
                description = trmNavigationBlock.Description;

                CheckMenuVisibility(iContent, ref inLeftNav, ref inNav);
                AddCreditMenuVisibleForBullionAccount(iContent, ref inNav, ref inLeftNav);
                if (level <= (menuType == Enums.eMenuType.MegaMenu ? 2 : 3))
                {
                    switch (menuType)
                    {
                        case Enums.eMenuType.PageLeft:
                            showExpanded = trmNavigationBlock.ShowExpandedOnPageLoad;
                            if (iContent != null && trmNavigationBlock.ShowChildren)
                            {
                                childNavigationItems = GetAutomaticChildren(iContent);
                            }

                            childNavigationItems.AddRange(GetMenuItemChildren(trmNavigationBlock.LeftMenuContent, menuType, level + 1));

                            break;
                        case Enums.eMenuType.MegaMenu:

                            childNavigationItems = GetMenuItemChildren(trmNavigationBlock.MegaMenuContent, menuType, level + 1);

                            break;
                        case Enums.eMenuType.Default:
                            break;
                        default:
                            if (trmNavigationBlock.RootLink != null && iContent != null && trmNavigationBlock.ShowChildren)
                            {
                                childNavigationItems = GetAutomaticChildren(iContent);
                            }
                            break;
                    }
                }

                isContainer = iContent is IContainerPage;
            }

            var thisDto = new MenuItemDto
            {
                MenuItemDisplayName = displayName,
                MenuItemExternalUrl = itemUrl,
                ShowExpanded = showExpanded,
                IsActiveContent = isActivePage,
                IsContainer = isContainer,
                VisibleInNav = inNav,
                VisibleInLeftNav = inLeftNav,
                CurrentBlock = trmNavigationBlock,
                ChildNavigationItems = childNavigationItems,
                OpenInANewWindow = openInANewWindow,
                ShowViewAll = showViewAll,
                ViewAllText = viewAllText,
                MenuIcon = menuIcon,
                Description = description

            };
            return thisDto;
        }

        private List<MenuItemDto> GetAutomaticChildren(IContent iContent)
        {
            var myChildren = _contentLoader.GetChildren<IContent>(iContent.ContentLink);

            return myChildren.Select(child => GetAutomaticMenuItemDto(child)).ToList();
        }

        private List<MenuItemDto> GetMenuItemChildren(ContentArea contentAreaToLookIn, Enums.eMenuType menuType, int counter = 0)
        {
            var childDtos = new List<MenuItemDto>();
            if (contentAreaToLookIn != null && contentAreaToLookIn.FilteredItems != null)
            {
                foreach (var thisChild in contentAreaToLookIn.FilteredItems)
                {
                    if (thisChild != null && !ContentReference.IsNullOrEmpty(thisChild.ContentLink))
                    {
                        ContentData contentData;
                        if(!_contentLoader.TryGet(thisChild.ContentLink, out contentData)) continue;

                        var thisDto = GetManualMenuItemDto(contentData, menuType, counter);
                        childDtos.Add(thisDto);
                    }
                }
            }
            return childDtos;
        }

        public TrmNavigationBlockViewModel GetTrmNavigationBlockViewModel(TrmNavigationBlock currentBlock)
        {
            var iContent = currentBlock.RootLink != null
                ? UrlResolver.Current.Route(new UrlBuilder(currentBlock.RootLink))
                : null;

            var inNav = true;
            var inLeftNav = true;


            CheckMenuVisibility(iContent, ref inLeftNav, ref inNav);

            var model = new TrmNavigationBlockViewModel
            {
                NavigationDto = new MenuItemDto
                {
                    ChildNavigationItems = iContent != null && currentBlock.ShowChildren
                        ? GetAutomaticChildren(iContent)
                        : new List<MenuItemDto>(),
                    CurrentBlock = currentBlock,
                    OpenInANewWindow = currentBlock.OpenInNewWindow,
                    ShowChildren = currentBlock.ShowChildren,
                    VisibleInNav = inNav,
                    VisibleInLeftNav = inLeftNav,
                    MenuItemDisplayName = string.IsNullOrEmpty(currentBlock.RootLabel)
                        ? GetAutomaticTrmNavigationBlockDisplayName(iContent)
                        : currentBlock.RootLabel,
                    MenuItemExternalUrl = iContent != null ? iContent.ContentLink.GetExternalUrl_V2() : string.Empty,
                    ShowExpanded = currentBlock.ShowExpandedOnPageLoad,
                    ShowViewAll = currentBlock.ShowViewAll,
                    ViewAllText = currentBlock.ViewAllText,
                    MenuIcon = currentBlock.MenuIcon,
                    Description = currentBlock.Description
                }
            };
            return model;
        }
    }
}