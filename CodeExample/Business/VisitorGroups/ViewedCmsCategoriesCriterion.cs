using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Web;
using EPiServer;
using EPiServer.Core;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.Personalization.VisitorGroups.Internal;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Hephaestus.ContentTypes.Models.Interfaces;

namespace TRM.Web.Business.VisitorGroups
{
    [VisitorGroupCriterion(Category = "Site Criteria", Description = "Matches after visiting any Content with category", DisplayName = "Visited CMS Category", LanguagePath = "/shell/cms/visitorgroups/criteria/cmscategories")]
    public class ViewedCategoriesCriterion : CriterionBase<ViewedCmsCategoriesModel>
    {
        private const string SessionKey = "Custom:VisitedCmsCategories";

        private readonly IContentLoader _contentLoader;
        private readonly IStateStorage _stateStorage;

        public ViewedCategoriesCriterion()
          : this(ServiceLocator.Current.GetInstance<IContentLoader>(), ServiceLocator.Current.GetInstance<CookieBasedStateStorage>())
        {
        }
        
        public ViewedCategoriesCriterion(IContentLoader contentLoader, CookieBasedStateStorage stateStorage)
        {
            this._contentLoader = contentLoader;
            this._stateStorage = stateStorage;
        }

        public override bool IsMatch(IPrincipal principal, HttpContextBase httpContext)
        {
            return this._stateStorage.IsAvailable && this.IsCriterionTrue(this.Load(httpContext));
        }

        public override void Subscribe(ICriterionEvents criterionEvents)
        {
            criterionEvents.StartRequest += this.criterionEvents_VisitedContent;
        }

        public override void Unsubscribe(ICriterionEvents criterionEvents)
        {
            criterionEvents.StartRequest -= this.criterionEvents_VisitedContent;
        }

        private void criterionEvents_VisitedContent(object sender, CriterionEventArgs e)
        {
            var path = e.HttpContext.Request.Url?.PathAndQuery;
            var contentReference = UrlResolver.Current.Route(new UrlBuilder(path))?.ContentLink;
            if (!this._stateStorage.IsAvailable || ContentReference.IsNullOrEmpty(contentReference))
            {
                return;
            }

            var viewedCategories = this.Load(e.HttpContext);
            if (this.IsCriterionTrue(viewedCategories))
            {
                return;
            }

            var str = contentReference.ToString();
            HashSet<string> stringSet = (HashSet<string>)null;
            if (viewedCategories != null && viewedCategories.TryGetValue(this.Model.SelectedCategory, out stringSet) &&
                stringSet.Contains(e.GetPageLink().ToString()) || !this.IsContentInCategory(contentReference, this.Model.SelectedCategory))
            {
                return;
            }

            viewedCategories = viewedCategories ?? new Dictionary<int, HashSet<string>>();

            stringSet = stringSet ?? new HashSet<string>();
            stringSet.Add(str);
            viewedCategories[this.Model.SelectedCategory] = stringSet;
            this.Save(e, viewedCategories);

        }

        private void Save(CriterionEventArgs e, IDictionary<int, HashSet<string>> viewedCategories)
        {
            if (e?.HttpContext?.Items != null)
            {
                e.HttpContext.Items[SessionKey] = viewedCategories;
            }

            this._stateStorage.Save(SessionKey, ViewedCategoriesConverter.ToString(viewedCategories));
        }

        private IDictionary<int, HashSet<string>> Load(HttpContextBase httpContext)
        {
            return httpContext?.Items?[SessionKey] as Dictionary<int, HashSet<string>> ?? ViewedCategoriesConverter.ToDictionary(this._stateStorage.LoadAsString(SessionKey));
        }

        private bool IsContentInCategory(ContentReference contentReference, int category)
        {
            this._contentLoader.TryGet(contentReference, out PageData page);
            if (page != null)
            {
                return page.Category.Any(c => c == category);
            }

            this._contentLoader.TryGet<IHaveCmsCategories>(contentReference, out var content);
            if (content != null)
            {
                return content.CmsCategoriesArray.Any(c => int.Parse(c) == category);
            }

            return false;
        }

        private bool IsCriterionTrue(IDictionary<int, HashSet<string>> viewedCategories)
        {
            return viewedCategories != null && viewedCategories.TryGetValue(this.Model.SelectedCategory, out HashSet<string> stringSet) && stringSet.Count >= this.Model.NumberOfPageViews;
        }
    }

    internal static class ViewedCategoriesConverter
    {
        private const string delimiterInsideCriteria = ",";
        private const char delimiterInsideCriteria_Char = ',';
        private const string delimiterBewteenCriterion = "#";
        private const char delimiterBewteenCriterion_Char = '#';

        public static string ToString(IDictionary<int, HashSet<string>> visitedCats)
        {
            if (visitedCats == null)
                return (string)null;
            StringBuilder stringBuilder = new StringBuilder();
            int num = visitedCats.Count<KeyValuePair<int, HashSet<string>>>();
            foreach (KeyValuePair<int, HashSet<string>> visitedCat in (IEnumerable<KeyValuePair<int, HashSet<string>>>)visitedCats)
            {
                stringBuilder.Append(visitedCat.Key.ToString());
                stringBuilder.Append(",");
                stringBuilder.Append(visitedCat.Value.Join(","));
                if (--num > 0)
                    stringBuilder.Append("#");
            }
            return stringBuilder.ToString();
        }

        public static IDictionary<int, HashSet<string>> ToDictionary(
          string visitedCats)
        {
            IDictionary<int, HashSet<string>> dictionary = (IDictionary<int, HashSet<string>>)null;
            string[] strArray1;
            if (visitedCats == null)
                strArray1 = (string[])null;
            else
                strArray1 = visitedCats.Split('#');
            if (strArray1 == null)
                strArray1 = Array.Empty<string>();
            foreach (string str in strArray1)
            {
                char[] chArray = new char[1] { ',' };
                string[] strArray2 = str.Split(chArray);
                int result;
                if (int.TryParse(strArray2[0], out result))
                {
                    HashSet<string> stringSet = new HashSet<string>();
                    for (int index = 1; index < strArray2.Length; ++index)
                        stringSet.Add(strArray2[index]);
                    if (dictionary == null)
                        dictionary = (IDictionary<int, HashSet<string>>)new Dictionary<int, HashSet<string>>();
                    dictionary.Add(result, stringSet);
                }
            }
            return dictionary;
        }
    }

    internal static class HashSetExtenstions
    {
        internal static string Join(this HashSet<string> pageReferences, string delimiter = ",")
        {
            return string.Join(delimiter, (IEnumerable<string>)pageReferences);
        }

        internal static HashSet<string> ToHashSet(this string pages, char delimiter = ',')
        {
            if (string.IsNullOrEmpty(pages))
                return (HashSet<string>)null;
            string[] strArray;
            if (pages == null)
                strArray = (string[])null;
            else
                strArray = pages.Split(delimiter);
            return new HashSet<string>((IEnumerable<string>)strArray);
        }
    }
}
