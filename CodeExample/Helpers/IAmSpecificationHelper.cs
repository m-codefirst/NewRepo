using System.Collections.Generic;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces.EntryProperties;

namespace TRM.Web.Helpers
{
    public interface IAmSpecificationHelper
    {
        List<SpecificationItemDto> GetSpecificationItems(IHaveAProductSpecification aProductSpecification);
    }
}