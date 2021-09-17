using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Personalization.VisitorGroups.Internal;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace TRM.Web.Business.VisitorGroups
{
  [VisitorGroupCriterion(Category = "Site Criteria", Description = "Matches after the user has visited a certain content", DisplayName = "Visited Content", LanguagePath = "/shell/cms/visitorgroups/criteria/viewedcontent")]
  public class ViewedContentCriterion : CriterionBase<ViewedContentCriterionModel>
  {
    private const string SessionKey = "Custom:ViewedContent";
    private readonly IStateStorage _stateStorage;

    public ViewedContentCriterion()
      : this(ServiceLocator.Current.GetInstance<CookieBasedStateStorage>())
    {
    }

    public ViewedContentCriterion(CookieBasedStateStorage stateStorage)
    {
      this._stateStorage = stateStorage;
    }

    public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
    {
      if (!this._stateStorage.IsAvailable)
        return false;
      HashSet<string> viewedPages = this.GetViewedContent();
      return viewedPages != null && viewedPages.Contains(this.Model.GetContentReference().ToString());
    }

    public override void Subscribe(ICriterionEvents criterionEvents)
    {
      criterionEvents.StartRequest += this.criterionEvents_VisitedPage;
    }

    public override void Unsubscribe(ICriterionEvents criterionEvents)
    {
      criterionEvents.StartRequest -= this.criterionEvents_VisitedPage;
    }

    private void criterionEvents_VisitedPage(object sender, CriterionEventArgs e)
    {
        var path = e.HttpContext.Request.Url?.PathAndQuery;
        var contentReference = UrlResolver.Current.Route(new UrlBuilder(path))?.ContentLink;
        if (!this._stateStorage.IsAvailable || ContentReference.IsNullOrEmpty(contentReference) ||
            contentReference == Model.GetContentReference())
        {
            return;
        }

        this.AddViewedContent(e.HttpContext, contentReference);
    }

    private void AddViewedContent(HttpContextBase httpContext, ContentReference pageLink)
    {
      HashSet<string> contentReferences = this.GetViewedContent() ?? new HashSet<string>();
      contentReferences.Add(pageLink.ToString());
      this._stateStorage.Save(SessionKey, string.Join(",", contentReferences));
    }

    private HashSet<string> GetViewedContent()
    {
        return ToHashSet(this._stateStorage.LoadAsString(SessionKey));
    }

    private static HashSet<string> ToHashSet(string references, char delimiter = ',')
    {
        if (string.IsNullOrEmpty(references))
        {
            return null;
        }

        string[] strArray = references.Split(delimiter);
        return new HashSet<string>(strArray);
    }
  }
}
