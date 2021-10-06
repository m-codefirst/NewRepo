using System.Collections.Generic;
using TRM.Shared.Models.DTOs;
using TRM.Web.Constants;
using TRM.Web.Models.EntityFramework.CustomerContactContext;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoPurchaseService : IAutoPurchaseService
    {
        private const int FundsThreshold = 10;

        private readonly IAutoInvestUserService autoInvestUserService;
        private readonly IAutoInvestPurchaseService autoInvestPurchaseService;
        private readonly IAutoInvestmentSerializationHelper autoInvestmentSerializationHelper;

        public AutoPurchaseService(IAutoInvestPurchaseService autoInvestPurchaseService, IAutoInvestUserService autoInvestUserService,
            IAutoInvestmentSerializationHelper autoInvestmentSerializationHelper)
        {
            this.autoInvestPurchaseService = autoInvestPurchaseService;
            this.autoInvestUserService = autoInvestUserService;
            this.autoInvestmentSerializationHelper = autoInvestmentSerializationHelper;
        }

        public List<AutoPurchaseProcessedUserDto> UpdateContactsInRange(IEnumerable<cls_Contact> contactsToProcess)
        {
            var result = new List<AutoPurchaseProcessedUserDto>();

            foreach (var contact in contactsToProcess)
            {
                if (contact.KYCStatus != (int)AccountKycStatus.Approved)
                {
                    autoInvestUserService.UpdateLastAutoInvestStatus(contact.UserId, Enums.AutoInvestUpdateOrderStatus.KycNotApproved);
                    result.Add(new AutoPurchaseProcessedUserDto(contact, Enums.AutoInvestUpdateOrderStatus.KycNotApproved, null));
                    continue;
                }

                if (contact.BullionCustomerEffectiveBalance < FundsThreshold || contact.BullionCustomerEffectiveBalance < contact.LtAutoInvestAmountDecimal)
                {
                    autoInvestUserService.UpdateLastAutoInvestStatus(contact.UserId, Enums.AutoInvestUpdateOrderStatus.InsufficientFunds);
                    result.Add(new AutoPurchaseProcessedUserDto(contact, Enums.AutoInvestUpdateOrderStatus.InsufficientFunds, null));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(contact.CustomerBullionObsAccountNumber))
                {
                    autoInvestUserService.UpdateLastAutoInvestStatus(contact.UserId, Enums.AutoInvestUpdateOrderStatus.MissingCustomerBullionObsAccountNumber);
                    result.Add(new AutoPurchaseProcessedUserDto(contact, Enums.AutoInvestUpdateOrderStatus.MissingCustomerBullionObsAccountNumber, "Customer Bullion OBS AccountNumber is Null/Missing."));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(contact.LtAutoInvestProductCode))
                {
                    autoInvestUserService.UpdateLastAutoInvestStatus(contact.UserId, Enums.AutoInvestUpdateOrderStatus.MissingAutoInvestProductCode);
                    result.Add(new AutoPurchaseProcessedUserDto(contact, Enums.AutoInvestUpdateOrderStatus.MissingAutoInvestProductCode, "Auto Invest Product Code is Null/Missing."));
                    continue;
                }

                var updateOrderResponse = autoInvestPurchaseService.UpdateOrder(contact.CustomerBullionObsAccountNumber,
                    autoInvestmentSerializationHelper.DeserializeInvestments(contact.LtAutoInvestProductCode,
                        contact.LtAutoInvestAmountDecimal ?? 0));
                
                autoInvestUserService.UpdateLastAutoInvestStatus(contact.UserId, updateOrderResponse?.Status ?? Enums.AutoInvestUpdateOrderStatus.Error);

                result.Add(new AutoPurchaseProcessedUserDto(contact, updateOrderResponse?.Status ?? Enums.AutoInvestUpdateOrderStatus.Error, updateOrderResponse?.Message));
            }

            return result;
        }
    }
}