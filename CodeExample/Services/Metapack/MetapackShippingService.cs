using System;
using TRM.Shared.Services;
using TRM.Web.Services.Metapack.Models.Request;
using TRM.Web.Services.Metapack.Models.Response;

namespace TRM.Web.Services.Metapack
{
    public interface IMetapackShippingService
    {
        ShippingResponse GetShippingOptions(string apiUrl, ShippingRequest request);
    }

    public class MetapackShippingService : WebRequestService, IMetapackShippingService
    {
        public MetapackShippingService()
        {

        }

        public ShippingResponse GetShippingOptions(string apiUrl, ShippingRequest request)
        {
            var find = $@"{apiUrl}?{request.ToQueryString()}";
            var response = base.Get<ShippingResponse>(find);

            return response;
        }
    }
}