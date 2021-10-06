using System.Web;
using EPiServer;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Web;
using TRM.Web.Constants;
using TRM.Web.Models.Pages;
using TRM.Web.Models.ViewModels.Bullion.CustomerBankAccount;

namespace TRM.Web.Services
{
    public class BankAccountValidationService : IBankAccountValidationService
    {
        private readonly IContentLoader _contentLoader;
        private readonly LocalizationService _localizationService;

        public BankAccountValidationService(IContentLoader contentLoader, LocalizationService localizationService)
        {
            _contentLoader = contentLoader;
            _localizationService = localizationService;
        }

        public bool ValidateUkBankAccount(AddOrEditBankAccountViewModel viewModel, out bool invalidSortCode, out bool invalidAccountNumber)
        {
            var url = "https://api.addressy.com/BankAccountValidation/Interactive/Validate/v2.00/dataset.ws?";
            url += "AccountNumber=" + HttpUtility.UrlEncode(viewModel.AccountNumber);
            url += "&SortCode=" + HttpUtility.UrlEncode(viewModel.SortCode);
            string statusInformation;
            var isCorrect = Validate(url, out statusInformation);
            invalidAccountNumber = !isCorrect && statusInformation.Contains("AccountNumber");
            invalidSortCode = !isCorrect &&  statusInformation.Contains("SortCode");
            
            return isCorrect;
        }

        public string ValidateIBan(AddOrEditBankAccountViewModel viewModel)
        {
            var url = "https://api.addressy.com/InternationalBankValidation/Interactive/Validate/v1.00/dataset.ws?";
            url += "IBAN=" + HttpUtility.UrlEncode(viewModel.Iban);
            string statusInformation;
            var isCorrect = Validate(url, out statusInformation);
            return isCorrect ? string.Empty : _localizationService.GetStringByCulture(StringResources.IbanInvalid, "Iban Invalid", ContentLanguage.PreferredCulture);
        }


        private bool Validate(string url, out string statusInformation)
        {
            statusInformation = string.Empty;
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return false;

            url += "&Key=" + HttpUtility.UrlEncode(startPage.BankAccountVerificationApiKey);

            var dataSet = new System.Data.DataSet();
            dataSet.ReadXml(url);

            if (dataSet.Tables.Count == 1 && dataSet.Tables[0].Columns.Count == 4 &&
                dataSet.Tables[0].Columns[0].ColumnName == "Error")
            {
                if (dataSet.Tables[0].Columns.Contains("Description")) statusInformation = dataSet.Tables[0].Rows[0]["Description"]?.ToString();
                return false;
            }

            var results = dataSet.Tables[0].Rows[0];
            bool isCorrect;
            bool.TryParse(results["IsCorrect"]?.ToString(), out isCorrect);
            if (dataSet.Tables[0].Columns.Contains("StatusInformation")) statusInformation = results["StatusInformation"]?.ToString();
            return isCorrect;
        }

        public bool ValidateSortCode(string sortCode)
        {
            var url = "https://api.addressy.com/BankAccountValidation/Interactive/RetrieveBySortcode/v1.00/dataset.ws?";
            url += "&SortCode=" + HttpUtility.UrlEncode(sortCode);

            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return false;

            url += "&Key=" + HttpUtility.UrlEncode(startPage.BankAccountVerificationApiKey);

            var dataSet = new System.Data.DataSet();
            dataSet.ReadXml(url);

            //Check for an error
            if (dataSet.Tables.Count == 0 || dataSet.Tables.Count == 1 && dataSet.Tables[0].Columns.Count == 4 &&
                dataSet.Tables[0].Columns[0].ColumnName == "Error") return false;

            return true;
        }
    }
}