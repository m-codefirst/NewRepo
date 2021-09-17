using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Hephaestus.Commerce.AddressBook.Services;
using Hephaestus.Commerce.Cart.Extensions;
using Hephaestus.Commerce.Cart.Models;
using Hephaestus.Commerce.Shared.Facades;
using Hephaestus.Commerce.Shared.Models;
using Hephaestus.Commerce.Shared.Services;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using TRM.Shared.Extensions;

namespace Hephaestus.Commerce.Cart.Services
{
    [ServiceConfiguration(typeof(ICartService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CartService : ICartService
    {
        
        private readonly IPricingService _pricingService;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IPlacedPriceProcessor _placedPriceProcessor;
        private readonly IInventoryProcessor _inventoryProcessor;
        private readonly ILineItemValidator _lineItemValidator;
        private readonly IPromotionEngine _promotionEngine;
        private readonly IOrderRepository _orderRepository;
        private readonly IAddressBookService _addressBookService;
        private readonly ICurrentMarket _currentMarket;
        private readonly ICurrencyService _currencyService;
        private readonly CustomerContextFacade _customerContext;

        public CartService(
            IPricingService pricingService,
            IOrderGroupFactory orderGroupFactory,
            IPlacedPriceProcessor placedPriceProcessor,
            IInventoryProcessor inventoryProcessor,
            ILineItemValidator lineItemValidator,
            IOrderRepository orderRepository,
            IPromotionEngine promotionEngine,
            IAddressBookService addressBookService,
            ICurrentMarket currentMarket,
            ICurrencyService currencyService, 
            CustomerContextFacade customerContext)
        {
            
            _pricingService = pricingService;
            _orderGroupFactory = orderGroupFactory;
            _placedPriceProcessor = placedPriceProcessor;
            _inventoryProcessor = inventoryProcessor;
            _lineItemValidator = lineItemValidator;
            _promotionEngine = promotionEngine;
            _orderRepository = orderRepository;
            _addressBookService = addressBookService;
            _currentMarket = currentMarket;
            _currencyService = currencyService;
            _customerContext = customerContext;
        }

        public void ChangeCartItem(ICart cart, int shipmentId, string code, decimal quantity, string size, string newSize)
        {
            ChangeCartItem(cart, shipmentId, code, quantity, size, newSize, null);
        }
        public void ChangeCartItem(ICart cart, int shipmentId, string code, decimal quantity, string size, string newSize, CustomerContact customerContact)
        {
            if (quantity > 0)
            {
                if (size == newSize)
                {
                    ChangeQuantity(cart, shipmentId, code, quantity, customerContact);
                }
                else
                {
                    //var newCode = _productService.GetSiblingVariantCodeBySize(code, newSize);
                    //UpdateLineItemSku(cart, shipmentId, code, newCode, quantity);
                }
            }
            else
            {
                RemoveLineItem(cart, shipmentId, code, customerContact);
            }
        }

        public string DefaultCartName
        {
            get { return "Default"; }
        }

        public string DefaultWishListName
        {
            get { return "WishList"; }
        }

        public void RecreateLineItemsBasedOnShipments(ICart cart, IEnumerable<CartItemModel> cartItems, IEnumerable<AddressModel> addresses)
        {
            var form = cart.GetFirstForm();
            var items = cartItems
                .GroupBy(x => new { x.AddressId, x.Code, x.IsGift })
                .Select(x => new
                {
                    // ReSharper disable once RedundantAnonymousTypePropertyName
                    Code = x.Key.Code,
                    // ReSharper disable once RedundantAnonymousTypePropertyName
                    AddressId = x.Key.AddressId,
                    Quantity = x.Count(),
                    // ReSharper disable once RedundantAnonymousTypePropertyName
                    IsGift = x.Key.IsGift
                }).ToList();

            foreach (var shipment in form.Shipments)
            {
                shipment.LineItems.Clear();
            }

            form.Shipments.Clear();

            foreach (var address in addresses)
            {
                var shipment = cart.CreateShipment(_orderGroupFactory);
                form.Shipments.Add(shipment);
                shipment.ShippingAddress = _addressBookService.ConvertToAddress(address, cart);

                foreach (var item in items.Where(x => x.AddressId == address.AddressId))
                {
                    var lineItem = cart.CreateLineItem(item.Code, _orderGroupFactory);
                    lineItem.IsGift = item.IsGift;
                    lineItem.Quantity = item.Quantity;
                    shipment.LineItems.Add(lineItem);
                }
            }

            ValidateCart(cart);
        }

        public void MergeShipments(ICart cart)
        {
            if (cart == null || !cart.GetAllLineItems().Any())
            {
                return;
            }

            var form = cart.GetFirstForm();
            var keptShipment = cart.GetFirstShipment();
            var removedShipments = form.Shipments.Skip(1).ToList();
            var movedLineItems = removedShipments.SelectMany(x => x.LineItems).ToList();
            removedShipments.ForEach(x => x.LineItems.Clear());
            removedShipments.ForEach(x => cart.GetFirstForm().Shipments.Remove(x));

            foreach (var item in movedLineItems)
            {
                var existingLineItem = keptShipment.LineItems.SingleOrDefault(x => x.Code == item.Code);
                if (existingLineItem != null)
                {
                    existingLineItem.Quantity += item.Quantity;
                    continue;
                }

                keptShipment.LineItems.Add(item);
            }

            ValidateCart(cart);
        }

        public bool AddToCart(ICart cart, string code, out string warningMessage)
        {
            warningMessage = string.Empty;

            var lineItem = cart.GetAllLineItems().FirstOrDefault(x => x.Code == code);

            if (lineItem == null)
            {
                lineItem = cart.CreateLineItem(code, _orderGroupFactory);
                lineItem.Quantity = 1;
                cart.AddLineItem(lineItem, _orderGroupFactory);
            }
            else
            {
                var shipment = cart.GetFirstShipment();
                cart.UpdateLineItemQuantity(shipment, lineItem, lineItem.Quantity + 1);
            }

            var validationIssues = ValidateCart(cart);

            foreach (var validationIssue in validationIssues)
            {
                warningMessage += String.Format("Line Item with code {0} ", lineItem.Code);
                warningMessage = validationIssue.Value.Aggregate(warningMessage, (current, issue) => current + String.Format("{0}, ", issue));
                warningMessage = warningMessage.Substring(0, warningMessage.Length - 2);
            }

            if (validationIssues.HasItemBeenRemoved(lineItem))
            {
                return false;
            }

            return GetFirstLineItem(cart, code) != null;
        }

        public void SetCartCurrency(ICart cart, Currency currency)
        {
            SetCartCurrency(cart, currency, null);
        }
        public void SetCartCurrency(ICart cart, Currency currency, CustomerContact customerContact)
        {
            if (currency.IsEmpty || currency == cart.Currency)
            {
                return;
            }

            cart.Currency = currency;
            foreach (var lineItem in cart.GetAllLineItems())
            {
                //If there is an item which has no price in the new currency, a NullReference exception will be thrown.
                //Mixing currencies in cart is not allowed.
                //It's up to site's managers to ensure that all items have prices in allowed currency.
                var price = _pricingService.GetPrice(lineItem.Code, cart.MarketId, currency);
                if (price != null)
                {
                    lineItem.PlacedPrice = price.Value.Amount;
                }
            }

            ValidateCart(cart, customerContact);
        }

        public Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(ICart cart)
        {
            return this.ValidateCart(cart, null);
        }
        public Dictionary<ILineItem, List<ValidationIssue>> ValidateCart(ICart cart, CustomerContact customerContact)
        {
            if (cart.Name.Equals(DefaultWishListName))
            {
                return new Dictionary<ILineItem, List<ValidationIssue>>();
            }

            var validationIssues = new Dictionary<ILineItem, List<ValidationIssue>>();
            cart.ValidateOrRemoveLineItems((item, issue) => validationIssues.AddValidationIssues(item, issue), _lineItemValidator);
            cart.UpdatePlacedPriceOrRemoveLineItems(customerContact ?? _customerContext.GetContactById(cart.CustomerId), (item, issue) => validationIssues.AddValidationIssues(item, issue), _placedPriceProcessor);
            cart.UpdateInventoryOrRemoveLineItems((item, issue) => validationIssues.AddValidationIssues(item, issue), _inventoryProcessor);

            cart.ApplyDiscounts(_promotionEngine, new PromotionEngineSettings());

            return validationIssues;
        }

        public Dictionary<ILineItem, List<ValidationIssue>> RequestInventory(ICart cart)
        {
            var validationIssues = new Dictionary<ILineItem, List<ValidationIssue>>();
            cart.AdjustInventoryOrRemoveLineItems((item, issue) => validationIssues.AddValidationIssues(item, issue), _inventoryProcessor);
            return validationIssues;
        }

        public void InventoryProcessorAdjustInventoryOrRemoveLineItem(ICart cart)
        {
            var validationIssues = new Dictionary<ILineItem, List<ValidationIssue>>();
            var shipment = cart.GetFirstShipment();
            _inventoryProcessor.AdjustInventoryOrRemoveLineItem(shipment, cart.OrderStatus, (item, issue) => validationIssues.AddValidationIssues(item, issue));
        }

        public ICart LoadCart(string name)
        {
            var cart = _orderRepository.LoadCart<ICart>(_customerContext.CurrentContactId, name, _currentMarket);
            if (cart != null)
            {
                SetCartCurrency(cart, _currencyService.GetCurrentCurrency());

                var validationIssues = ValidateCart(cart);
                // After validate, if there is any change in cart, saving cart.
                if (validationIssues.Any())
                {
                    _orderRepository.Save(cart);
                }
            }

            return cart;
        }

        public ICart LoadOrCreateCart(string name, CustomerContact customerContact = null)
        {
            var currentContentId = _customerContext.CurrentContactId;
            Currency currentCurrency = null;
            if (customerContact != null)
            {
                currentContentId = (Guid)customerContact.PrimaryKeyId;
                currentCurrency = _currencyService.GetCurrentCurrency(customerContact.GetDefaultCurrencyCode());
            }
            else
            {
                currentCurrency = _currencyService.GetCurrentCurrency();
            }
            
            var cart = customerContact != null ? _orderRepository.LoadOrCreateCart<ICart>(currentContentId, name, customerContact) : _orderRepository.LoadOrCreateCart<ICart>(currentContentId, name, _currentMarket);
            if (cart != null)
            {
                SetCartCurrency(cart, currentCurrency, customerContact);
            }

            return cart;
        }

        public bool AddCouponCode(ICart cart, string couponCode)
        {
            var couponCodes = cart.GetFirstForm().CouponCodes;
            if (couponCodes.Any(c => c.Equals(couponCode, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            couponCodes.Add(couponCode);
            var rewardDescriptions = cart.ApplyDiscounts(_promotionEngine, new PromotionEngineSettings());
            var appliedCoupons = rewardDescriptions
                .Where(r => r.AppliedCoupon != null)
                .Select(r => r.AppliedCoupon);

            var couponApplied = appliedCoupons.Any(c => c.Equals(couponCode, StringComparison.OrdinalIgnoreCase));
            if (!couponApplied)
            {
                couponCodes.Remove(couponCode);
            }
            return couponApplied;
        }

        public void RemoveCouponCode(ICart cart, string couponCode)
        {
            cart.GetFirstForm().CouponCodes.Remove(couponCode);
            cart.ApplyDiscounts(_promotionEngine, new PromotionEngineSettings());
        }

        private void RemoveLineItem(ICart cart, int shipmentId, string code)
        {
            RemoveLineItem(cart, shipmentId, code, null);
        }
        private void RemoveLineItem(ICart cart, int shipmentId, string code, CustomerContact customerContact)
        {
            var shipment = cart.GetFirstForm().Shipments.First(s => s.ShipmentId == shipmentId || shipmentId <= 0);

            var lineItem = shipment.LineItems.FirstOrDefault(l => l.Code == code);
            if (lineItem != null)
            {
                shipment.LineItems.Remove(lineItem);
            }

            if (!shipment.LineItems.Any())
            {
                cart.GetFirstForm().Shipments.Remove(shipment);
            }

            ValidateCart(cart, customerContact);
        }

        private void UpdateLineItemSku(ICart cart, int shipmentId, string oldCode, string newCode, decimal quantity)
        {
            RemoveLineItem(cart, shipmentId, oldCode);

            //merge same sku's
            var newLineItem = GetFirstLineItem(cart, newCode);
            if (newLineItem != null)
            {
                var shipment = cart.GetFirstForm().Shipments.First(s => s.ShipmentId == shipmentId || shipmentId <= 0);
                cart.UpdateLineItemQuantity(shipment, newLineItem, newLineItem.Quantity + quantity);
            }
            else
            {
                newLineItem = cart.CreateLineItem(newCode, _orderGroupFactory);
                newLineItem.Quantity = quantity;
                cart.AddLineItem(newLineItem, _orderGroupFactory);

                var price = _pricingService.GetCurrentPrice(newCode);
                if (price.HasValue)
                {
                    newLineItem.PlacedPrice = price.Value.Amount;
                }
            }

            ValidateCart(cart);
        }

        private void ChangeQuantity(ICart cart, int shipmentId, string code, decimal quantity)
        {
            ChangeQuantity(cart, shipmentId, code, quantity, null);
        }
        private void ChangeQuantity(ICart cart, int shipmentId, string code, decimal quantity, CustomerContact customerContact)
        {
            if (quantity == 0)
            {
                RemoveLineItem(cart, shipmentId, code, customerContact);
            }
            var shipment = cart.GetFirstForm().Shipments.First(s => s.ShipmentId == shipmentId || shipmentId <= 0);
            var lineItem = shipment.LineItems.FirstOrDefault(x => x.Code == code);
            if (lineItem == null)
            {
                return;
            }

            cart.UpdateLineItemQuantity(shipment, lineItem, quantity);
            ValidateCart(cart, customerContact);
        }

        private ILineItem GetFirstLineItem(IOrderGroup cart, string code)
        {
            return cart.GetAllLineItems().FirstOrDefault(x => x.Code == code);
        }
    }
}
