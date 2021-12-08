using System.Collections.Generic;
using Mediachase.Commerce.Customers;
using TRM.Shared.Models.DTOs;

namespace TRM.Shared.Helpers
{
    public interface IAmCreditCardHelper
    {
       
        bool DeleteCreditCard(CustomerContact customerContact, string token);

        bool AddCustomerCreditCard(CustomerContact customerContact, CreditCardDto creditCard);

        List<CreditCardDto> GetCustomerCreditCards(CustomerContact customerContact);

        CreditCardDto GetCard(CustomerContact customerContact, string token);

    }
}