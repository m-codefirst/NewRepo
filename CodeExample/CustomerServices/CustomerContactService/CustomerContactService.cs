using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using Hephaestus.Commerce.Helpers;

namespace Hephaestus.Commerce.CustomerServices.CustomerContactService
{
    public class CustomerContactService : ICustomerContactService
    {
        public const string AccountCreditConst = "AccountCredit_";
        public const string CountryConst = "Country";
        public const string TitleConst = "Title";
        public const string Value = "Value";
        public const string Text = "Text";
        public const string DateTimeConst = "dd/MM/yyyy";
        public const string CultureDateTimeConst = "en-GB";
        //Episervers minimum date is 1/1/1753 12:00:00
        public DateTime EpiServerMinDateTimeConst = new DateTime(1753,1,1,12,00,01);

        public const string GenreConst = "Genres";
        public const string BrandsConst = "Brands";
        public const string ProductAreasConst = "ProductAreas";

        protected readonly IAmCustomerContact CustomerContact;
        protected readonly LocalizationService LocalizationService;
        protected readonly IAmMetaDataHelper MetaDataHelper;

        public CustomerContactService(IAmCustomerContact customerContact, LocalizationService localizationService,
            IAmMetaDataHelper metaDataHelper)
        {
            CustomerContact = customerContact;
            LocalizationService = localizationService;
            MetaDataHelper = metaDataHelper;
        }

        public string ContactId
        {
            get { return CustomerContact.CurrentContact.UserId; }
        }

        public string Email
        {
            get
            {
                return CustomerContact.CurrentContact.Email;
            }
            set
            {
                CustomerContact.CurrentContact.Email = value;
            }
        }

        public DateTime? LastOrder { get { return CustomerContact.CurrentContact.LastOrder; } }
        public string Title
        {
            get
            {
                return CustomerContact.CurrentContact[TitleConst] != null
                    ? CustomerContact.CurrentContact[TitleConst].ToString()
                    : string.Empty;
            }

            set
            {
                CustomerContact.CurrentContact[TitleConst] = value ?? string.Empty;
            }
        }

        public virtual string FirstName
        {
            get
            {
                return CustomerContact.CurrentContact.FirstName;
            }
            set
            {
                CustomerContact.CurrentContact.FirstName = value;
            }
        }

        public string Surname
        {
            get
            {
                return CustomerContact.CurrentContact.LastName;
            }
            set
            {
                CustomerContact.CurrentContact.LastName = value;
            }
        }

        public string Country
        {
            get
            {
                return CustomerContact.CurrentContact[CountryConst] == null ? String.Empty : CustomerContact.CurrentContact[CountryConst].ToString();
            }
            set
            {
                CustomerContact.CurrentContact[CountryConst] = value;
            }
        }

        public string DateOfBirth
        {
            get
            {
                return CustomerContact.CurrentContact.BirthDate == null 
                    || EpiServerMinDateTimeConst == CustomerContact.CurrentContact.BirthDate
                    ? string.Empty
                    : CustomerContact.CurrentContact.BirthDate.Value.ToString(DateTimeConst,
                    CultureInfo.CreateSpecificCulture(CultureDateTimeConst));
            }
            set
            {
                CustomerContact.CurrentContact.BirthDate = string.IsNullOrEmpty(value) 
                    ? EpiServerMinDateTimeConst 
                    : Convert.ToDateTime(value);
            }
        }

        public virtual decimal CurrentCultureAccountCredit
        {
            get
            {
                return GetCultureSpecificAccountCredit(ContentLanguage.PreferredCulture.Name);
            }
            set
            {
                SetCultureSpecificAccountCredit(ContentLanguage.PreferredCulture.Name, value);
            }
        }

        public decimal GetCultureSpecificAccountCredit(string culture)
        {
            const string accountCreditDefault = "0";
            decimal accountCredit;

            decimal.TryParse((CustomerContact.CurrentContact[CultureSpecificName(culture, AccountCreditConst)] ?? accountCreditDefault).ToString(), out accountCredit);

            return accountCredit;
        }

        public void SetCultureSpecificAccountCredit(string culture, decimal accountCreditValue)
        {
            CustomerContact.CurrentContact[CultureSpecificName(culture, AccountCreditConst)] = accountCreditValue;
        }

        private static string CultureSpecificName(string culture, string nameConstant)
        {
            const string hyphen = "-";
            const string underScore = "_";
            //Example name: AccountCredit_en_GB
            return string.Format("{0}{1}", nameConstant, culture).Replace(hyphen, underScore);
        }

        public IEnumerable<int> Genre
        {
            get
            {
                var selectedGenres = (int[])CustomerContact.CurrentContact[GenreConst];
                return selectedGenres == null ? new List<int>() : selectedGenres.ToList();
            }
            set
            {
                CustomerContact.CurrentContact[GenreConst] = value.ToArray();
            }
        }

        public IEnumerable<int> Brands
        {
            get
            {
                var selectedBrands = (int[])CustomerContact.CurrentContact[BrandsConst];
                return selectedBrands == null ? new List<int>() : selectedBrands.ToList();
            }
            set
            {
                CustomerContact.CurrentContact[BrandsConst] = value.ToArray();
            }
        }

        public IEnumerable<int> ProductAreas
        {
            get
            {
                var selectedProductAreas = (int[])CustomerContact.CurrentContact[ProductAreasConst];
                return selectedProductAreas == null ? new List<int>() : selectedProductAreas.ToList();
            }
            set
            {
                CustomerContact.CurrentContact[ProductAreasConst] = value.ToArray();
            }
        }

        public void SaveChanges()
        {
            CustomerContact.CurrentContact.SaveChanges();
        }
    }
}
