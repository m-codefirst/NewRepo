using System;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using Hephaestus.Commerce.Product.ProductService;
using Mediachase.Commerce.Orders;

namespace TRM.Web.Helpers
{
    public class TrmLineItemHelper
    {
        /// <summary>
        /// Determines whether [is line item of type] [the specified content loader].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contentLoader">The content loader.</param>
        /// <param name="refConverter">The reference converter.</param>
        /// <param name="lineItem">The line item.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Line item provided was either null or did not contain an entry code to lookup.
        /// or
        /// </exception>
        public static bool IsLineItemOfType<T>(IContentLoader contentLoader, IAmReferenceConverter refConverter, LineItem lineItem)
        {
            if (lineItem == null || string.IsNullOrEmpty(lineItem.Code))
                throw new ArgumentException(
                    "Line item provided was either null or did not contain an entry code to lookup.");

            var variantReference = refConverter.GetContentLink(lineItem.Code);

            // ReSharper disable once UseStringInterpolation -- Disabled as build server does not have .NET 4.6.2 (C# 6)
            if (variantReference == null) throw new ArgumentException(string.Format("Could not look up variant for code {0}", lineItem.Code));

            var reference = contentLoader.Get<EntryContentBase>(variantReference);

            return reference is T;
        }
    }
}