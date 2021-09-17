using System;
using System.Collections.Generic;

namespace Hephaestus.Commerce.CustomerServices.CustomerContactService
{
    public interface ICustomerContactService
    {
        string ContactId { get; }

        string Email { get; set; }

        string Title { get; set; }

        //Title drop down culture specific commented out
        //SelectList TitleList { get; }

        string FirstName { get; set; }

        string Surname { get; set; }

        string Country { get; set; }

        string DateOfBirth { get; set; }

        decimal CurrentCultureAccountCredit { get; set; }

        decimal GetCultureSpecificAccountCredit(string culture);

        void SetCultureSpecificAccountCredit(string culture, decimal accountCreditValue);

        IEnumerable<int> Genre { get; set; }

        IEnumerable<int> Brands { get; set; }

        IEnumerable<int> ProductAreas { get; set; }

        DateTime? LastOrder { get; }

        void SaveChanges();
    }
}
