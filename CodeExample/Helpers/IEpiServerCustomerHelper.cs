using System.Collections.Generic;
using TRM.IntegrationServices.Models.Export.Customers;

namespace TRM.Web.Helpers
{
    public interface IEpiServerCustomerHelper
    {
        List<CreateCustomerRequest> GetCustomerContactsToBeCreatedInObs(int maxCustomer, int commandTimeout);

        List<UpdateCustomerRequest> GetCustomerContactsToBeUpdatedInObs(int maxCustomersToUpdate, int commandTimeout);
    }
}