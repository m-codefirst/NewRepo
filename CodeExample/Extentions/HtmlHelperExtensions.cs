using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using EPiServer.Framework.Localization;
using log4net;
using TRM.Web.Business.Rendering;
using System.Web.WebPages;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Optimization;
using EPiServer.Globalization;


namespace TRM.Web.Extentions
{
    public static class HtmlHelperExtensions
    {
        private static readonly LocalizationService LocalizationService = LocalizationService.Current;

        public static IHtmlString TranslateFallBackRaw(
            this HtmlHelper htmlHelper,
            string translationKey,
            string fallback,
            params object[] args)
        {
            return
                new HtmlString(string.Format(
                    CultureInfo.CurrentCulture,
                    LocalizationService.GetString(translationKey, fallback),
                    args));
        }

        public static string TranslateFallBack(
            this HtmlHelper htmlHelper,
            string translationKey,
            string fallback)
        {
            try
            {
                var translation = LocalizationService.GetStringByCulture(translationKey, fallback, ContentLanguage.PreferredCulture);
                return string.IsNullOrEmpty(translation) ? string.Empty : translation;
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetLogger(typeof(HtmlHelperExtensions));
                if (logger.IsErrorEnabled)
                {
                    logger.Error(string.Format("Failed to translate value '{0}'", translationKey), ex);
                }
                return fallback;
            }
        }


        public static string TranslateFallBack(
            this HtmlHelper htmlHelper,
            string translationKey,
            string fallback,
            params object[] args)
        {
            try
            {
                var translation = LocalizationService.GetString(translationKey, fallback);

                if (string.IsNullOrEmpty(translation))
                {
                    return string.Empty;
                }

                return string.Format(
                    CultureInfo.CurrentCulture,
                    translation,
                    args);
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetLogger(typeof(HtmlHelperExtensions));
                if (logger.IsErrorEnabled)
                {
                    logger.Error(string.Format("Failed to translate value '{0}'", translationKey), ex);
                }
                return translationKey;
            }
        }
        public static MvcHtmlString CustomDropDownList(this HtmlHelper helper, List<CustomSelectListItem> select)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<select>");
            foreach (var item in select)
            {
                sb.AppendFormat("<option value='{0}' data-zone={3} {2}>{1}</option>", item.Value, item.Text, (item.Selected ? "selected" : ""), item.Zone);
            }
            sb.Append("</select>");
            return MvcHtmlString.Create(sb.ToString());
        }

        private static IEnumerable<SelectListItem> GetSelectListWithDefaultValue(IEnumerable<SelectListItem> selectList, object defaultValue)
        {

            var enumerable = new object[1]
            {
                defaultValue
            };

            IEnumerable<string> first = from object value in enumerable
                                        select Convert.ToString(value, CultureInfo.CurrentCulture);
            IEnumerable<string> second = from Enum value in enumerable.OfType<Enum>()
                                         select value.ToString("d");
            first = first.Concat(second);
            HashSet<string> hashSet = new HashSet<string>(first, StringComparer.OrdinalIgnoreCase);
            List<SelectListItem> list = new List<SelectListItem>();
            foreach (SelectListItem select in selectList)
            {
                select.Selected = ((select.Value != null) ? hashSet.Contains(select.Value) : hashSet.Contains(select.Text));
                list.Add(select);
            }
            return list;
        }
        private static StringBuilder BuildItems(string optionLabel, IEnumerable<CustomSelectListItem> selectList)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(optionLabel))
            {
                stringBuilder.AppendLine(ListItemToOption(new CustomSelectListItem
                {
                    Text = optionLabel,
                    Value = string.Empty,
                    Selected = false
                }));
            }
            IEnumerable<IGrouping<int, CustomSelectListItem>> enumerable = selectList.GroupBy(delegate (CustomSelectListItem i)
            {
                if (i.Group != null)
                {
                    return i.Group.GetHashCode();
                }
                return i.GetHashCode();
            });
            foreach (IGrouping<int, CustomSelectListItem> item in enumerable)
            {
                SelectListGroup group = item.First().Group;
                TagBuilder tagBuilder = null;
                if (group != null)
                {
                    tagBuilder = new TagBuilder("optgroup");
                    if (group.Name != null)
                    {
                        tagBuilder.MergeAttribute("label", group.Name);
                    }
                    if (group.Disabled)
                    {
                        tagBuilder.MergeAttribute("disabled", "disabled");
                    }
                    stringBuilder.AppendLine(tagBuilder.ToString(TagRenderMode.StartTag));
                }
                foreach (CustomSelectListItem item2 in item)
                {
                    stringBuilder.AppendLine(ListItemToOption(item2));
                }
                if (group != null)
                {
                    stringBuilder.AppendLine(tagBuilder.ToString(TagRenderMode.EndTag));
                }
            }
            return stringBuilder;
        }

        internal static string ListItemToOption(CustomSelectListItem item)
        {
            TagBuilder tagBuilder = new TagBuilder("option")
            {
                InnerHtml = HttpUtility.HtmlEncode(item.Text)
            };
            if (item.Value != null)
            {
                tagBuilder.Attributes["value"] = item.Value;
            }
            if (!string.IsNullOrEmpty(item.Zone) && item.Value != null)
            {
                tagBuilder.Attributes["data-zone"] = item.Zone.ToString();
            }
            if (item.Selected)
            {
                tagBuilder.Attributes["selected"] = "selected";
            }
            if (item.Disabled)
            {
                tagBuilder.Attributes["disabled"] = "disabled";
            }
            return tagBuilder.ToString(TagRenderMode.Normal);
        }

        public static MvcHtmlString CustomDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, List<CustomSelectListItem> selectList, string optionLabel, object htmlAttributes)
        {
            var name = ExpressionHelper.GetExpressionText(expression);
            string fullHtmlFieldName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            var htmlAttrs = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

            StringBuilder stringBuilder = BuildItems(optionLabel, selectList);
            TagBuilder tagBuilder = new TagBuilder("select")
            {
                InnerHtml = stringBuilder.ToString()
            };
            tagBuilder.MergeAttributes(htmlAttrs);
            tagBuilder.MergeAttribute("name", fullHtmlFieldName, replaceExisting: true);
            tagBuilder.GenerateId(fullHtmlFieldName);
            ModelState value;
            if (htmlHelper.ViewData.ModelState.TryGetValue(fullHtmlFieldName, out value) && value.Errors.Count > 0)
            {
                tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
            }
            tagBuilder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata));
            return new MvcHtmlString(tagBuilder.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString CustomDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, List<CustomSelectListItem> selectList, object htmlAttributes)
        {
            return CustomDropDownListFor(htmlHelper, expression, selectList, string.Empty, htmlAttributes);
        }

        public static MvcHtmlString GetMyPictureHtml(this HtmlHelper htmlHelper, string originalUrl, Dictionary<string, string> customAttributes, Dictionary<string, int> sizeStrings)
        {
            var pictureBuilder = new StringBuilder();

            pictureBuilder.Append("<picture>");

            foreach (var size in sizeStrings)
            {
                var mediaQuery = "media='(min-width: " + size.Key + "px)'";
                pictureBuilder.Append("<source " + mediaQuery + " ng-srcset='" + originalUrl + "?width=" + size.Value + "'>");
            }

            var customAttributeBuilder = new StringBuilder();
            foreach (var customAttribute in customAttributes)
            {
                customAttributeBuilder.Append(customAttribute.Key + "='" + customAttribute.Value + "' ");
            }
            pictureBuilder.Append("<img ng-src='" + originalUrl + "?width=" + sizeStrings.FirstOrDefault().Value + "' " + customAttributeBuilder + ">");

            pictureBuilder.Append("</picture>");
            return MvcHtmlString.Create(pictureBuilder.ToString());
        }

        // This function is a duplicate of GetMyPictureHtml, without any angular code
        // TODO: Consider renaming function and deleting GetMyPictureHtml
        public static MvcHtmlString GetMyPictureHtmlNoAngular(this HtmlHelper htmlHelper, string originalUrl, Dictionary<string, string> customAttributes, Dictionary<string, int> sizeStrings)
        {
            var pictureBuilder = new StringBuilder();

            pictureBuilder.Append("<picture>");

            foreach (var size in sizeStrings)
            {
                var mediaQuery = "media='(min-width: " + size.Key + "px)'";
                pictureBuilder.Append("<source " + mediaQuery + " srcset='" + originalUrl + "?width=" + size.Value + "'>");
            }

            var customAttributeBuilder = new StringBuilder();
            foreach (var customAttribute in customAttributes)
            {
                customAttributeBuilder.Append(customAttribute.Key + "='" + customAttribute.Value + "' ");
            }
            pictureBuilder.Append("<img src='" + originalUrl + "?width=" + sizeStrings.FirstOrDefault().Value + "' " + customAttributeBuilder + ">");

            pictureBuilder.Append("</picture>");
            return MvcHtmlString.Create(pictureBuilder.ToString());
        }

        public static MvcHtmlString Script(this HtmlHelper htmlHelper, Func<object, HelperResult> template)
        {
            htmlHelper.ViewContext.HttpContext.Items["_script_" + Guid.NewGuid()] = template;
            return MvcHtmlString.Empty;
        }

        public static IHtmlString RenderPartialViewScripts(this HtmlHelper htmlHelper)
        {
            foreach (object key in htmlHelper.ViewContext.HttpContext.Items.Keys)
            {
                if (key.ToString().StartsWith("_script_"))
                {
                    var template = htmlHelper.ViewContext.HttpContext.Items[key] as Func<object, HelperResult>;
                    if (template != null)
                    {
                        htmlHelper.ViewContext.Writer.Write(template(null));
                    }
                }
            }
            return MvcHtmlString.Empty;
        }

        /// <summary>
        /// Return Partial View.
        /// The element naming convention is maintained in the partial view by setting the prefix name from the expression.
        /// The name of the view (by default) is the class name of the Property or a UIHint("partial name").
        /// @Html.PartialFor(m => m.Address)  - partial view name is the class name of the Address property.
        /// </summary>
        /// <param name="expression">Model expression for the prefix name (m => m.Address)</param>
        /// <returns>Partial View as Mvc string</returns>

        public static MvcHtmlString PartialFor<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression)
        {
            return html.PartialFor(expression, null);
        }

        /// <summary>
        /// Return Partial View.
        /// The element naming convention is maintained in the partial view by setting the prefix name from the expression.
        /// </summary>
        /// <param name="partialName">Partial View Name</param>
        /// <param name="expression">Model expression for the prefix name (m => m.Group[2])</param>
        /// <returns>Partial View as Mvc string</returns>
        public static MvcHtmlString PartialFor<TModel, TProperty>(this HtmlHelper<TModel> html,
            Expression<Func<TModel, TProperty>> expression,
            string partialName
        )
        {
            string name = ExpressionHelper.GetExpressionText(expression);
            string modelName = html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            ModelMetadata metaData = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
            object model = metaData.Model;


            if (partialName == null)
            {
                partialName = metaData.TemplateHint == null
                    ? typeof(TProperty).Name    // Class name
                    : metaData.TemplateHint;    // UIHint("template name")
            }

            // Use a ViewData copy with a new TemplateInfo with the prefix set
            ViewDataDictionary viewData = new ViewDataDictionary(html.ViewData)
            {
                TemplateInfo = new TemplateInfo { HtmlFieldPrefix = modelName }
            };

            // Call standard MVC Partial
            return html.Partial(partialName, model, viewData);
        }


        public static string RenderPartialViewToString(this Controller controller, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.RouteData.GetRequiredString("action");
            }

            controller.ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                // Find the partial view by its name and the current controller context.
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);

                // Create a view context.
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);

                // Render the view using the StringWriter object.
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }

        public static IHtmlString InlineScript(this HtmlHelper htmlHelper, string path)
        {
            return htmlHelper.Raw(WrapFileContentWithTag(path, "script"));
        }

        public static IHtmlString InlineCss(this HtmlHelper htmlHelper, string path)
        {
            return htmlHelper.Raw(WrapFileContentWithTag(path, "style"));
        }

        private static string WrapFileContentWithTag(string path, string tag)
        {
            // load the contents of that file
            string fileContent;
            try
            {
                fileContent = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + path);
            }
            catch (Exception)
            {
                // blank string if we can't read the file for any reason
                fileContent = string.Empty;
            }

            return $"<{tag}>{fileContent}</{tag}>";
        }

        public static IHtmlString InlineStyles(this HtmlHelper htmlHelper, string bundleVirtualPath)
        {
            var bundleContent = LoadBundleContent(htmlHelper.ViewContext.HttpContext, bundleVirtualPath);
            var htmlTag = $"<style>{bundleContent}</style>";

            return new HtmlString(htmlTag);
        }

        private static string LoadBundleContent(HttpContextBase httpContext, string bundleVirtualPath)
        {
            var bundleContext = new BundleContext(httpContext, BundleTable.Bundles, bundleVirtualPath);
            var bundle = BundleTable.Bundles.FirstOrDefault(b => b.Path == bundleVirtualPath);
            if (bundle == null) return string.Empty;

            var bundleResponse = bundle.GenerateBundleResponse(bundleContext);
            return bundleResponse.Content;
        }

        public static IHtmlString StripHtmlTagsAndSpecialChars(this HtmlHelper htmlHelper, string text)
        {
            return new HtmlString(text.StripHtmlTagsAndSpecialChars());
        }

        public static bool IsDebug(this HtmlHelper htmlHelper)
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public static IDisposable WrapInTag(this HtmlHelper htmlHelper, string tag, string className = "")
        {
            htmlHelper.OpenTag(tag, className);
            return new DisposableHtmlHelper(() => htmlHelper.CloseTag(tag));
        }

        public static void OpenTag(this HtmlHelper htmlHelper, string tag, string className)
        {
            htmlHelper.ViewContext.Writer.Write($@"<{tag} class='{className}'>");
        }        
        
        public static void CloseTag(this HtmlHelper htmlHelper, string tag)
        {
            htmlHelper.ViewContext.Writer.Write($"</{tag}>");
        }

        public static string AntiForgeryTokenValue(this HtmlHelper helper)
        {
            var antiForgeryInputTag = helper.AntiForgeryToken().ToString();
            var removedStart = antiForgeryInputTag.Replace(@"<input name=""__RequestVerificationToken"" type=""hidden"" value=""", "");
            var tokenValue = removedStart.Replace(@""" />", "");
            return tokenValue;
        }
    }
}