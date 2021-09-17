using System;
using System.Collections.Generic;
using TRM.IntegrationServices.Interfaces;
using TRM.IntegrationServices.Models.Export.Customers;

namespace TRM.Web.Helpers
{
    public class EpiServerCustomerHelper : IEpiServerCustomerHelper
    {
        private readonly ICustomerExportService _customerExportService;

        public EpiServerCustomerHelper(ICustomerExportService customerExportService)
        {
            _customerExportService = customerExportService;
        }

        public List<CreateCustomerRequest> GetCustomerContactsToBeCreatedInObs(int maxCustomer, int commandTimeout)
        {
            var exportResultList = new List<CreateCustomerRequest>();
            // Get a list of new contacts from EPiServer to create in AX
            var contactsToBeCreated = _customerExportService.GetCustomerContactsToBeCreatedInObs(maxCustomer, commandTimeout);

            foreach (var customerContact in contactsToBeCreated)
            {
                var customerToCreate = new CreateCustomerRequest();
                try
                {
                    customerToCreate.CustomerInfo = _customerExportService.PopulateAndUpdateCustomerRequest(customerContact);
                }
                catch (Exception ex)
                {
                    customerToCreate.Exception = ex;
                    customerToCreate.Message = ex.Message;
                }
                exportResultList.Add(customerToCreate);
            }

            return exportResultList;
        }

        public List<UpdateCustomerRequest> GetCustomerContactsToBeUpdatedInObs(int maxCustomersToUpdate, int commandTimeout)
        {
            var exportResultList = new List<UpdateCustomerRequest>();
            // Get a list of contacts to update from EPiServer to update in AX
            var contactsToBeUpdated = _customerExportService.GetCustomerContactsToBeUpdatedInObs(maxCustomersToUpdate, commandTimeout);

            foreach (var customerContact in contactsToBeUpdated)
            {
                var customerToUpdate = new UpdateCustomerRequest();
                try
                {
                    customerToUpdate.CustomerInfo = _customerExportService.PopulateAndUpdateCustomerRequest(customerContact);
                }
                catch (Exception ex)
                {
                    customerToUpdate.Exception = ex;
                    customerToUpdate.Message = ex.Message;
                }
                exportResultList.Add(customerToUpdate);
            }

            return exportResultList;
        }
    }
}