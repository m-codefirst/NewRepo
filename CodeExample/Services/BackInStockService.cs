using System;
using System.Linq;
using EPiServer;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.Web;
using Mediachase.Commerce.Catalog;
using TRM.Web.Business.DataAccess;
using TRM.Web.Business.Email;
using TRM.Web.Helpers;
using TRM.Web.Models.Catalog;
using TRM.Web.Models.EntityFramework.EmailBackInStock;
using TRM.Web.Models.Pages;

namespace TRM.Web.Services
{
    public class BackInStockService : IBackInStockService
    {
        private readonly IEmailBackInStockRepository _backInStockRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IEmailHelper _emailHelper;
        private readonly IAmEntryHelper _entryHelper;
        private readonly IContentRepository _contentRepository;
        private readonly IAmInventoryHelper _inventoryHelper;
        private readonly ReferenceConverter _referenceConverter;
        public BackInStockService(
            IEmailBackInStockRepository backInStockRepository,
            IContentLoader contentLoader,
            IEmailHelper emailHelper,
            IAmEntryHelper entryHelper,
            IContentRepository contentRepository,
            IAmInventoryHelper inventoryHelper,
            ReferenceConverter referenceConverter)
        {
            _referenceConverter = referenceConverter;
            _inventoryHelper = inventoryHelper;
            _backInStockRepository = backInStockRepository;
            _contentLoader = contentLoader;
            _emailHelper = emailHelper;
            _entryHelper = entryHelper;
            _contentRepository = contentRepository;
        }

        public Guid? SignupEmailBackInStock(BackInStockSubscription subscription)
        {
            if (subscription == null) return null;

            subscription.Id = Guid.NewGuid();
            subscription.CreatedOn = DateTime.Now;
            subscription.LastModifiedOn = DateTime.Now;
            subscription.Processed = false;

            var variant = _entryHelper.GetVariantFromCode(subscription.VariantCode);
            if (variant != null && CanSignupEmailForVariant(variant))
            {
                var subscriptionId = _backInStockRepository.AddSignUpSubscription(subscription);
                SendEmaiConfirmationUserSignupSuccess(subscription, variant);
                UpdateStatusEmailBackInStockForVariant(variant);
                return subscriptionId;
            }
            return null;
        }

        public bool IsSignupExisted(string email, string variantCode)
        {
            return _backInStockRepository.GetAll()
                .FirstOrDefault(x => x.Email.Equals(email) && x.VariantCode.Equals(variantCode) && x.Processed == false) != null;
        }

        public bool CanSignupEmailForVariant(TrmVariant variant)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            if (startPage == null) return false;

            var disableSignupEmail = (startPage.EmailBackInStockSettingsPage != null) ?
                _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage).DisableSignupEmail :
                startPage.DisableSignupEmail;

            if (disableSignupEmail) return false;
            return !variant.DisableSignupEmail;

            //keep it the old way for now - no idea why this has changed. potential bugfix? keep it here till TRM raise it
            //var numberOfSignup = GetNumberOfSignupsNotProcessedOfVariant(variant.Code);
            //var maximumNumberOfSignupsPerVariant = (startPage.EmailBackInStockSettingsPage != null) ?
            //    _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage).MaximumNumberOfSignupsPerVariant :
            //    startPage.MaximumNumberOfSignupsPerVariant;
            //return !(numberOfSignup > maximumNumberOfSignupsPerVariant);
        }

        private int GetNumberOfSignupOfVariant(string variantCode)
        {
            return _backInStockRepository.GetAll().Count(x => x.VariantCode.Equals(variantCode));
        }

        private int GetNumberOfSignupsNotProcessedOfVariant(string productCode)
        {
            return _backInStockRepository.GetAll().Count(x => x.VariantCode.Equals(productCode) && x.Processed == false);
        }

        private void UpdateStatusEmailBackInStockForVariant(TrmVariant variant)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var maximumNumberOfSignupsPerVariant = (startPage.EmailBackInStockSettingsPage != null) ?
                _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage).MaximumNumberOfSignupsPerVariant :
                startPage.MaximumNumberOfSignupsPerVariant;
            if (startPage == null || maximumNumberOfSignupsPerVariant == 0) return;

            var numberOfSignup = GetNumberOfSignupsNotProcessedOfVariant(variant.Code);

            if (numberOfSignup >= maximumNumberOfSignupsPerVariant)
            {
                //Update Variant property
                var variantClone = variant.CreateWritableClone() as TrmVariant;
                if (variantClone != null)
                {
                    variantClone.DisableSignupEmail = true;
                    _contentRepository.Save(variantClone, SaveAction.Publish, AccessLevel.NoAccess);
                }
            }
        }

        private void SendEmaiConfirmationUserSignupSuccess(BackInStockSubscription subscription, TrmVariant variant)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var emailConfirmingSignedUp = (startPage?.EmailBackInStockSettingsPage != null) ?
                _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage).EmailConfirmingSignedUp :
                startPage.EmailConfirmingSignedUp;
            if (emailConfirmingSignedUp != null)
            {
                var confirmEmailPage = _contentLoader.Get<TRMEmailPage>(emailConfirmingSignedUp);
                if (confirmEmailPage != null) _emailHelper.SendSignupEmailBackInStockConfirmEmail(confirmEmailPage, subscription.Email, subscription.Name, variant);
            }
        }
        private int GetBatchSize()
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var totalNumberOfEmailToSendForAvariant = (startPage.EmailBackInStockSettingsPage != null) ?
                _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage).TotalNumberOfEmailToSendForAvariant :
                startPage.TotalNumberOfEmailToSendForAvariant;
            return totalNumberOfEmailToSendForAvariant;
        }
        public string NotifyForAvailableVariants()
        {
            var numberOfBackInStockVariant = 0;
            var numberOfEmailSent = 0;

            var batchSize = GetBatchSize();

            var pendingVariantCodes = _backInStockRepository.GetAll().Where(b => b.Processed == false)
                .Select(b => b.VariantCode).Distinct().ToList();

            foreach (var pendingVariantCode in pendingVariantCodes)
            {
                var entryContentLink = _referenceConverter.GetContentLink(pendingVariantCode);

                var stockStatus = _inventoryHelper.GetStockSummary(entryContentLink);

                if (stockStatus.PurchaseAvailableQuantity <= 0) continue;

                numberOfBackInStockVariant++;
                var variant = _entryHelper.GetVariantFromCode(pendingVariantCode);
                var subcribes = TakeNotProcessed(pendingVariantCode, batchSize);

                foreach (var subscription in subcribes)
                {
                    if (!SendEmailNotifyBackInStock(subscription, variant)) continue;
                    subscription.Processed = true;
                    subscription.EmailSentOn = DateTime.Now;
                    numberOfEmailSent++;
                }

                _backInStockRepository.SaveChange();
            }

            return $"There are {numberOfBackInStockVariant} products back in stock and {numberOfEmailSent} emails have sent to notify users";
        }
        private bool SendEmailNotifyBackInStock(BackInStockSubscription subscription, TrmVariant variant)
        {
            var startPage = _contentLoader.Get<StartPage>(SiteDefinition.Current.StartPage);
            var emailWhenBackInStock = (startPage?.EmailBackInStockSettingsPage != null) ?
                _contentLoader.Get<EmailBackInStockSettingsPage>(startPage.EmailBackInStockSettingsPage).EmailWhenBackInStock :
                startPage.EmailWhenBackInStock;
            if (emailWhenBackInStock != null)
            {
                var notifyEmailPage = _contentLoader.Get<TRMEmailPage>(emailWhenBackInStock);
                if (notifyEmailPage != null) return _emailHelper.SendEmailNotifyBackInStock(notifyEmailPage, subscription.Email, subscription.Name, variant);
            }

            return false;
        }
        public IQueryable<BackInStockSubscription> TakeNotProcessed(string variantCode, int size)
        {
            return _backInStockRepository.GetAll().Where(s => s.VariantCode.Equals(variantCode) && s.Processed == false).Take(size);
        }
    }
}