using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.BusinessFoundation.Data.Sql;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using System.Configuration;
using EPiServer;
using TRM.Web.Models.Pages;
using EPiServer.Core;
using EPiServer.Find.Helpers.Text;
using TRM.Shared.Extensions;
using TRM.Web.Helpers;
using static TRM.Shared.Constants.StringConstants;
using Enums = TRM.Shared.Constants.Enums;
using EPiServer.Logging;

namespace TRM.Web.Services
{
    public interface ICustomerContactService
    {
        IEnumerable<CustomerContact> GetAllContactsNotNotifiedOfKycStatusChange();
        List<CustomerContactViewModel> SearchSIPPCustomers(PrimaryKeyId ownerId, string keyword, int pageIndex, int pageSize, out int totalPages);
        List<CustomerContactViewModel> SearchContactsWithLinkedAccount(string keyword, SortBy sortOrder, string sortType, int pageIndex, int pageSize, out int totalPages);
        List<CustomerContactViewModel> GetAllContactsFromBullionObsAccountNumberList(IEnumerable<string> obsNumbers);
        List<CustomerContactViewModel> GetAllContactsFromContactIdList(IEnumerable<string> contactIds);
        IEnumerable<CustomerContact> GetAllContactsByBullionObsNumberList(IEnumerable<string> obsNumbers);
        IEnumerable<CustomerContact> GetAllContactsByObsNumberList(IEnumerable<string> obsNumbers);
    }

    [ServiceConfiguration(ServiceType = typeof(ICustomerContactService))]
    public class CustomerContactService : ICustomerContactService
    {
        private readonly string _commerceConnectionString = ConfigurationManager.ConnectionStrings["EcfSqlConnection"].ConnectionString;

        private readonly IContentLoader _contentLoader;
        public readonly IAmBullionContactHelper _bullionContactHelper;
        protected readonly ILogger Logger = LogManager.GetLogger(typeof(CustomerContactService));

        public CustomerContactService(IContentLoader contentLoader, IAmBullionContactHelper bullionContactHelper)
        {
            _contentLoader = contentLoader;
            _bullionContactHelper = bullionContactHelper;
        }

        #region Public Methods

        public IEnumerable<CustomerContact> GetAllContactsNotNotifiedOfKycStatusChange()
        {
            var filters = new AndBlockFilterElement(
                new FilterElement(CustomFields.KycStatusNotification, FilterElementType.IsNotNull, true),
                new FilterElement(CustomFields.KycStatusNotification, FilterElementType.Equal, DateTimeHelper.KycNotificationDate),
                new FilterElement(CustomFields.KycStatus, FilterElementType.IsNotNull, true)
            );

            return BusinessManager.List(ContactEntity.ClassName, new FilterElement[] { filters }, null, 0, 100)
                .Select(x => x as CustomerContact)
                .Where(x => x != null);
        }

        public List<CustomerContactViewModel> SearchSIPPCustomers(PrimaryKeyId ownerId, string keyword, int pageIndex, int pageSize, out int totalPages)
        {
            var searchRequest = new SearchContactRequest
            {
                OwnerId = ownerId,
                BullionCustomerType = (int)Enums.BullionCustomerType.SIPPCustomer,
                Keyword = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize,
                IgnoreGuest = true,
                ContactMustHaveLinkedAccount = true
            };

            return SearchContactsInternal(searchRequest, out totalPages);
        }

        public List<CustomerContactViewModel> SearchContactsWithLinkedAccount(string keyword, SortBy sortOrder, string sortType, int pageIndex, int pageSize, out int totalPages)
        {
            var searchRequest = new SearchContactRequest
            {
                Keyword = keyword,
                PageIndex = pageIndex,
                PageSize = pageSize,
                OrderBy = sortOrder,
                SortType = sortType,

                IgnoreGuest = true,
                ContactMustHaveLinkedAccount = true
            };

            return SearchContactsInternal(searchRequest, out totalPages);
        }

        public List<CustomerContactViewModel> GetAllContactsFromBullionObsAccountNumberList(IEnumerable<string> obsNumbers)
        {
            if (!obsNumbers.Any()) return new List<CustomerContactViewModel>();
            Logger.Error($"Before Execute SQL Query.");
            var sqlSelect =
                @"SELECT F1.ContactId, F1.Code, F1.UserID, F1.FullName, F1.Email, F1.Activated, F1.CustomerLockedOut, F1.CustomerCustomerType, F1.PreferredCurrency, F1.CustomerBullionObsAccountNumber, F1.BullionCustomerEffectiveBalance " +
                "FROM cls_Contact F1 " +
                "where F1.CustomerBullionObsAccountNumber IN(" +
                string.Join(",", obsNumbers.Select(x => $"'{x}'")) + ")";

            using (var conn = new SqlConnection(_commerceConnectionString))
            {
                return conn.Query<CustomerContactViewModel>(sqlSelect).ToList();
            }
        }

        public List<CustomerContactViewModel> GetAllContactsFromContactIdList(IEnumerable<string> contactIds)
        {
            if (!contactIds.Any()) return new List<CustomerContactViewModel>();

            const string prefixUserId = "String:";
            var customerContacts = QueryAllContactsByFindInStringListValues(ContactEntity.PrimaryKeyName, contactIds);
            return customerContacts.Select(x => new CustomerContactViewModel
            {
                ContactId = x.PrimaryKeyId.Value,
                Code = x.Code,
                UserID = x.UserId,
                Username = x.UserId?.Substring(prefixUserId.Length),
                FullName = x.FullName,
                Email = x.Email,
                Activated = x.GetBooleanProperty(CustomFields.Activated),
                CustomerLockedOut = x.GetBooleanProperty(CustomFields.CustomerLockedOut),
                CustomerCustomerType = x.GetStringProperty(CustomFields.CustomerType),
                PreferredCurrency = x.PreferredCurrency,
                CustomerBullionObsAccountNumber = x.GetStringProperty(CustomFields.BullionObsAccountNumber)
            }).ToList();
        }

        public IEnumerable<CustomerContact> GetAllContactsByObsNumberList(IEnumerable<string> obsNumbers)
        {
            return QueryAllContactsByFindInStringListValues(new List<string>() { CustomFields.BullionObsAccountNumber, CustomFields.ObsAccountNumber }, obsNumbers);
        }

        public IEnumerable<CustomerContact> GetAllContactsByBullionObsNumberList(IEnumerable<string> obsNumbers)
        {
            return QueryAllContactsByFindInStringListValues(CustomFields.BullionObsAccountNumber, obsNumbers);
        }

        private IEnumerable<CustomerContact> QueryAllContactsByFindInStringListValues(string contactFieldName, IEnumerable<string> values)
        {
            return QueryAllContactsByFindInStringListValues(new List<string>() { contactFieldName }, values);
        }

        private IEnumerable<CustomerContact> QueryAllContactsByFindInStringListValues(IEnumerable<string> contactFieldNames, IEnumerable<string> values)
        {
            var filters = new List<FilterElement>();
            foreach (var contactFieldName in contactFieldNames)
            {
                filters.Add(new FilterElement(contactFieldName, FilterElementType.In, values));
            }
            var keywordFilters = new OrBlockFilterElement(filters.ToArray());

            return BusinessManager.List(ContactEntity.ClassName, new FilterElement[] { keywordFilters })
                .Select(x => x as CustomerContact)
                .Where(x => x != null);
        }

        #endregion // Public Methods

        #region Helpers
        private List<CustomerContactViewModel> SearchContactsInternal(SearchContactRequest searchRequest, out int totalPages)
        {
            var useSqlFullTextSearch = _contentLoader.Get<StartPage>(ContentReference.StartPage).UseSqlFullTextSearchForContactSearch;
            if (useSqlFullTextSearch)
            {
                return SearchContactsInternalUsingFullTextSearch(searchRequest, out totalPages);
            }

            return
               SearchContactsUsingBMInternal(searchRequest, out totalPages)
               .Select(x => new CustomerContactViewModel
               {
                   UserID = x.UserId,
                   Activated = x.GetBooleanProperty(CustomFields.Activated),
                   Email = x.Email,
                   CustomerCustomerType = x.Properties[CustomFields.CustomerType].Value?.ToString(),
                   CustomerLockedOut = x.GetBooleanProperty(CustomFields.CustomerLockedOut),
                   FullName = x.FullName,
                   Username = _bullionContactHelper.GetUsername(x),
                   PreferredCurrency = x.PreferredCurrency,
                   CustomerBullionObsAccountNumber = x.GetStringProperty(CustomFields.BullionObsAccountNumber),
                   BeneficiaryReference = _bullionContactHelper.GetBeneficiaryReference(x),
                   IsGuest = x.GetBooleanProperty(CustomFields.IsGuest)
               }).ToList();
        }

        private List<CustomerContactViewModel> SearchContactsInternalUsingFullTextSearch(SearchContactRequest searchRequest, out int totalPages)
        {
            var words = searchRequest.Keyword.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Replace("'", "''"));
            var searchKey = string.Join(" AND ", words);

            if (!Guid.TryParse(searchRequest.OwnerId.ToString(), out var ownerIdParam))
            {
                ownerIdParam = Guid.Empty;
                searchRequest.OwnerId = null;
            }

            var sqlWhere = searchRequest.OwnerId.HasValue ? "WHERE F1.OwnerID = @ownerId" : "";
            if (!searchRequest.BullionCustomerType.HasValue)
            {
                sqlWhere = string.IsNullOrWhiteSpace(sqlWhere)
                    ? $"WHERE F1.{CustomFields.BullionCustomerType}={searchRequest.BullionCustomerType}"
                    : sqlWhere + $" AND F1.{CustomFields.BullionCustomerType}={searchRequest.BullionCustomerType}";
            }

            var sqlFrom = @"cls_Contact F1";
            if (!string.IsNullOrWhiteSpace(searchRequest.Keyword))
            {
                sqlFrom = sqlFrom + @" INNER JOIN CONTAINSTABLE(cls_Contact, (FirstName, LastName, MiddleName, FullName, Email), @searchKey) F2
                            ON F1.ContactId = F2.[KEY]";
            }

            var sqlSelect = $@"SELECT F1.UserID, F1.FullName, F1.Email, F1.Activated, F1.CustomerLockedOut, CustomerCustomerType, PreferredCurrency, CustomerBullionObsAccountNumber FROM {sqlFrom}
                            {sqlWhere}
                            ORDER BY F1.FullName
                            OFFSET(@pageIndex - 1) * @pageSize ROWS
                            FETCH NEXT @pageSize ROWS ONLY";

            int totalCount = 0;
            List<CustomerContactViewModel> result = null;
            using (var conn = new SqlConnection(_commerceConnectionString))
            {
                totalCount = conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM {sqlFrom} {sqlWhere}", new { searchKey, ownerId = ownerIdParam });
                result = conn.Query<CustomerContactViewModel>(sqlSelect, new { searchRequest.PageIndex, searchRequest.PageSize, searchKey, ownerId = ownerIdParam }).ToList();
            }

            totalPages = totalCount / searchRequest.PageSize;
            if (totalCount % searchRequest.PageSize > 0) totalPages++;

            return result;
        }

        private List<CustomerContact> SearchContactsUsingBMInternal(SearchContactRequest searchRequest, out int totalPages)
        {
            var filters = new List<FilterElement>();

            if (searchRequest.ContactMustHaveLinkedAccount)
            {
                filters.Add(new FilterElement(ContactEntity.FieldUserId, FilterElementType.StartsWith, "String:"));
            }

            if (searchRequest.IgnoreGuest)
            {
                var filterGuest = new OrBlockFilterElement(
                    new FilterElement(CustomFields.IsGuest, FilterElementType.IsNull, true),
                    new FilterElement(CustomFields.IsGuest, FilterElementType.Equal, false));

                filters.Add(filterGuest);
            }

            if (searchRequest.OwnerId != null && searchRequest.OwnerId.Value.ToString().IsNotNullOrEmpty())
            {
                filters.Add(new FilterElement(ContactEntity.FieldOwnerId, FilterElementType.Equal, searchRequest.OwnerId.Value));
            }
            if (searchRequest.BullionCustomerType.HasValue)
            {
                filters.Add(new FilterElement(CustomFields.BullionCustomerType, FilterElementType.Equal, searchRequest.BullionCustomerType.Value));
            }
            if (!string.IsNullOrWhiteSpace(searchRequest.Keyword))
            {
                var keywordFilters = new OrBlockFilterElement(
                    new FilterElement(ContactEntity.FieldFullName, FilterElementType.Contains, searchRequest.Keyword),
                    new FilterElement(CustomFields.ObsAccountNumber, FilterElementType.Contains, searchRequest.Keyword),
                    new FilterElement(CustomFields.BullionObsAccountNumber, FilterElementType.Contains, searchRequest.Keyword),
                    new FilterElement(ContactEntity.FieldUserId, FilterElementType.Contains, searchRequest.Keyword),
                    new FilterElement(ContactEntity.FieldEmail, FilterElementType.Contains, searchRequest.Keyword)
                );
                filters.Add(keywordFilters);
            }

            SortingElement sortElement;

            SortingElementType sortType;

            Enum.TryParse(searchRequest.SortType, true, out sortType);

            switch (searchRequest.OrderBy)
            {
                case SortBy.Email:
                    sortElement = new SortingElement(ContactEntity.FieldEmail, sortType);
                    break;
                case SortBy.Username:
                    sortElement = new SortingElement(ContactEntity.FieldUserId, sortType);
                    break;
                default:
                    sortElement = new SortingElement(CustomFields.Activated, sortType);
                    break;
            }

            var start = (searchRequest.PageIndex - 1) * searchRequest.PageSize;
            var list = BusinessManager.List(ContactEntity.ClassName, filters.ToArray(), new[] { sortElement }, start, searchRequest.PageSize)
                .Select(x => x as CustomerContact)
                .Where(x => x != null)
                .ToList();

            var mc = DataContext.Current.GetMetaClass(ContactEntity.ClassName);
            var contactConfig = mc.GetTableConfig();
            var totalCount = CustomTableRow.GetTotalCount(contactConfig, filters.ToArray());

            totalPages = totalCount / searchRequest.PageSize;
            if (totalCount % searchRequest.PageSize > 0) totalPages++;

            return list;
        }

        private List<CustomerContact> SearchContactsUsingBMInternal(string obsAccountNumber)
        {
            var filters = new List<FilterElement>();

            if (!string.IsNullOrWhiteSpace(obsAccountNumber))
            {
                var keywordFilters = new OrBlockFilterElement(
                    new FilterElement(CustomFields.ObsAccountNumber, FilterElementType.Equal, obsAccountNumber),
                    new FilterElement(CustomFields.BullionObsAccountNumber, FilterElementType.Equal, obsAccountNumber)
                );
                filters.Add(keywordFilters);
            }

            const int start = 0;
            const int pageSize = 10;
            var sortElement = new SortingElement(CustomFields.ObsAccountNumber, SortingElementType.Asc);

            var list = BusinessManager.List(ContactEntity.ClassName, filters.ToArray(), new[] { sortElement }, start, pageSize)
                .Select(x => x as CustomerContact)
                .Where(x => x != null)
                .ToList();

            return list;
        }

        internal class SearchContactRequest
        {
            public string Keyword { get; set; }
            public PrimaryKeyId? OwnerId { get; set; }
            public int? BullionCustomerType { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public SortBy OrderBy { get; set; }
            public string SortType { get; set; }
            public bool ContactMustHaveLinkedAccount { get; set; }
            public bool IgnoreGuest { get; set; }
        }
        #endregion // Helpers
    }

    public class CustomerContactViewModel
    {
        public Guid ContactId { get; set; }
        public string Code { get; set; }
        public string UserID { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool Activated { get; set; }
        public bool CustomerLockedOut { get; set; }
        public string CustomerCustomerType { get; set; }
        public string PreferredCurrency { get; set; }
        public string CustomerBullionObsAccountNumber { get; set; }
        public string BeneficiaryReference { get; set; }
        public bool IsGuest { get; set; }
        public decimal EffectiveBalance { get; set; }
    }

    public enum SortBy
    {
        Username,
        Email,
        AccountStatus
    }

    public class SortOrder
    {
        public SortBy OrderBy { get; set; }
        public string SortType { get; set; }
    }
}