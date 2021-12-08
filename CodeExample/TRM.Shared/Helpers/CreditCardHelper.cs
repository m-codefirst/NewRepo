using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;
using TRM.Shared.Constants;
using TRM.Shared.Models.DTOs;

namespace TRM.Shared.Helpers
{
    public class CreditCardHelper : IAmCreditCardHelper
    {
        private readonly IAmContactAuditHelper _contactAuditHelper;

        public CreditCardHelper(IAmContactAuditHelper contactAuditHelper)
        {
            _contactAuditHelper = contactAuditHelper;
        }
        
        public bool DeleteCreditCard(CustomerContact customerContact, string token)
        {
            var card = customerContact.ContactCreditCards.FirstOrDefault(
                c => c.ExtendedProperties[0]?.Value.ToString() == token);

            if (card == null)
            {
                throw new Exception($"Card Not found: token {token}");
            }

            customerContact.DeleteCreditCard(card);

            customerContact.SaveChanges();

            _contactAuditHelper.WriteCustomerAudit(customerContact, "Card Details Removed", $"Card details were Removed - {GetCardType(card)}: {card.LastFourDigits}");

            return true;

        }

        public bool AddCustomerCreditCard(CustomerContact customerContact, CreditCardDto creditCard)
        {
            if (string.IsNullOrEmpty(creditCard.LastFour) || string.IsNullOrEmpty(creditCard.Token))
            {
                return false;
            }

            if (customerContact == null) return false;

            if (customerContact.ContactCreditCards.Any(c => c.LastFourDigits == creditCard.LastFour && c.CardType == (int)GetCardType(creditCard.CardType))) return false;

            var cardTypeEnum = CreditCard.eCreditCardType.Visa;
            var otherCardType = string.Empty;

            if (!Enum.TryParse(creditCard.CardType, true, out cardTypeEnum))
            {
                cardTypeEnum = CreditCard.eCreditCardType.Visa;
                otherCardType = creditCard.CardType;
            }

            var card = new CreditCard
            {
                CardType = (int)cardTypeEnum,
                LastFourDigits = creditCard.LastFour
            };

            var objCard = card as EntityObject;

            objCard[StringConstants.CustomFields.CreditCardToken] = creditCard.Token;
            objCard[StringConstants.CustomFields.CreditCardUseAmex] = creditCard.UseAmex;
            objCard[StringConstants.CustomFields.CreditCardOtherType] = otherCardType;
            objCard[StringConstants.CustomFields.CreditCardExpiry] = creditCard.CardExpiry;
            objCard[StringConstants.CustomFields.CreditCardNameOnCard] = creditCard.NameOnCard;
            customerContact.AddCreditCard(card);

            customerContact.SaveChanges();

            _contactAuditHelper.WriteCustomerAudit(customerContact, "Card Details Saved", $"Card details were saved - {creditCard.CardType}: {creditCard.LastFour}");

            return true;

        }

        public List<CreditCardDto> GetCustomerCreditCards(CustomerContact customerContact)
        {
            var cards = new List<CreditCardDto>();

            var creditCards = customerContact.ContactCreditCards.Where(c => c.ExtendedProperties[0]?.Value != null &&
                                                                            !string.IsNullOrEmpty(c.Properties[StringConstants.CustomFields.CreditCardToken]?.Value.ToString()) &&
                                                                            !string.IsNullOrEmpty(c.LastFourDigits));

            foreach (var card in creditCards)
            {
                cards.Add(new CreditCardDto()
                {
                    CardType = GetCardType(card),
                    LastFour = card.LastFourDigits,
                    CardExpiry = card.Properties[StringConstants.CustomFields.CreditCardExpiry]?.Value?.ToString(),
                    Token = card.Properties[StringConstants.CustomFields.CreditCardToken]?.Value?.ToString(),
                    UseAmex = (bool)(card.Properties[StringConstants.CustomFields.CreditCardUseAmex]?.Value ?? false),
                    NameOnCard = card.Properties[StringConstants.CustomFields.CreditCardNameOnCard]?.Value?.ToString()
                });
            }

            return cards;

        }

        public CreditCardDto GetCard(CustomerContact customerContact, string token)
        {
            var card = customerContact.ContactCreditCards.FirstOrDefault(
                c => c.ExtendedProperties[0]?.Value.ToString() == token);

            if (card == null)
            {
               throw new Exception($"Card Not found: token {token}");
            }

            return new CreditCardDto()
            {
                CardType = GetCardType(card),
                LastFour = card.LastFourDigits,
                CardExpiry = card.Properties[StringConstants.CustomFields.CreditCardExpiry]?.Value?.ToString(),
                Token = card.Properties[StringConstants.CustomFields.CreditCardToken]?.Value?.ToString(),
                UseAmex = (bool) (card.Properties[StringConstants.CustomFields.CreditCardUseAmex]?.Value ?? false),
                NameOnCard = card.Properties[StringConstants.CustomFields.CreditCardNameOnCard]?.Value?.ToString()
            };
        }

        private CreditCard.eCreditCardType GetCardType(string cardType)
        {
            var cardTypeEnum = CreditCard.eCreditCardType.Visa;
            
            Enum.TryParse(cardType, true, out cardTypeEnum);

            return cardTypeEnum;
        }

        private string GetCardType(CreditCardEntity card)
        {
            return string.IsNullOrEmpty(card.ExtendedProperties[2].Value?.ToString())
                ? ((CreditCard.eCreditCardType) card.CardType).ToString()
                : card.ExtendedProperties[2].Value.ToString();
        }
    }
}
