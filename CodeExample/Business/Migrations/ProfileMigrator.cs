using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Find.Helpers.Reflection;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Extensions;
using Mediachase.Commerce.Security;
using TRM.Shared.Constants;
using TRM.Web.Business.Cart;
using TRM.Web.Helpers;
using TRM.Web.Models.Blocks.Bullion;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Business.Migrations
{
    public class ProfileMigrator : IProfileMigrator
    {

        private readonly ICurrentMarket _currentMarket;
        private readonly IOrderRepository _orderRepository;
        private readonly ITrmCartService _cartService;
        private readonly CartMigrator _cartMigrator;
        private readonly IAmBullionContactHelper _bullionContactHelper;
        private readonly IAmShippingMethodHelper _shippingMethodHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:EPiServer.Commerce.Order.ProfileMigrator" /> class.
        /// </summary>
        /// <param name="orderRepository">The order repository.</param>
        /// <param name="currentMarket">The current market.</param>
        /// <param name="cartMigrator">The cart migrator.</param>
        /// <param name="bullionContactHelper"></param>
        public ProfileMigrator(
            IOrderRepository orderRepository, 
            ICurrentMarket currentMarket, 
            ITrmCartService cartService, 
            CartMigrator cartMigrator, 
            IAmBullionContactHelper bullionContactHelper,
            IAmShippingMethodHelper shippingMethodHelper)
        {
            _orderRepository = orderRepository;
            _currentMarket = currentMarket;
            _cartMigrator = cartMigrator;
            _bullionContactHelper = bullionContactHelper;
            _cartService = cartService;
            _shippingMethodHelper = shippingMethodHelper;
        }

        /// <summary>Migrates the orders.</summary>
        /// <param name="anonymousId">The anonymous identifier.</param>
        public virtual void MigrateOrders(Guid anonymousId)
        {
            CustomerContact customerContact = PrincipalInfo.CurrentPrincipal.GetCustomerContact();
            if ((customerContact != null ? (!customerContact.PrimaryKeyId.HasValue ? 1 : 0) : 1) != 0)
                return;
            IEnumerable<IPurchaseOrder> purchaseOrders = this._orderRepository.Load<IPurchaseOrder>(anonymousId, (string)null);
            if (purchaseOrders == null)
                return;
            foreach (IPurchaseOrder purchaseOrder1 in purchaseOrders)
            {
                IPurchaseOrder purchaseOrder2 = purchaseOrder1;
                PrimaryKeyId? primaryKeyId = customerContact.PrimaryKeyId;
                Guid guid = primaryKeyId.HasValue ? (Guid)primaryKeyId.GetValueOrDefault() : Guid.Empty;
                purchaseOrder2.CustomerId = guid;
                this._orderRepository.Save((IOrderGroup)purchaseOrder1);
            }
        }

        public virtual void MigrateWishlists(Guid anonymousId)
        {
            IMarket currentMarket1 = this._currentMarket.GetCurrentMarket();
            if (currentMarket1 == null)
                return;
            CustomerContact customerContact = PrincipalInfo.CurrentPrincipal.GetCustomerContact();
            PrimaryKeyId? primaryKeyId;
            int num;
            if (customerContact == null)
            {
                num = 1;
            }
            else
            {
                primaryKeyId = customerContact.PrimaryKeyId;
                num = !primaryKeyId.HasValue ? 1 : 0;
            }
            if (num != 0)
                return;
            CartMigrator cartMigrator = this._cartMigrator;
            Guid sourceCustomerId = anonymousId;
            primaryKeyId = customerContact.PrimaryKeyId;
            Guid destinationCustomerId = (Guid)primaryKeyId.Value;
            IMarket currentMarket2 = currentMarket1;
            cartMigrator.MigrateWishList(sourceCustomerId, destinationCustomerId, currentMarket2);
        }

        /// <summary>Migrates the existing cart, merges the items.</summary>
        /// <param name="anonymousId">The anonymous identifier.</param>
        public virtual void MigrateCarts(Guid anonymousId)
        {
            CustomerContact customerContact = PrincipalInfo.CurrentPrincipal.GetCustomerContact();
            if ((customerContact != null ? (!customerContact.PrimaryKeyId.HasValue ? 1 : 0) : 1) != 0)
                return;

            MigrateCarts(customerContact, anonymousId, (Guid)customerContact.PrimaryKeyId.Value);
        }


        #region Custom Cart Migration

        void MigrateCarts(CustomerContact customer, Guid sourceCustomerId, Guid destinationCustomerId)
        {
            var sourceCarts = _orderRepository.Load<ICart>(sourceCustomerId, null);
            foreach (var sourceCart in sourceCarts)
            {
                if (!sourceCart.GetAllLineItems().Any())
                {
                    if (sourceCart.OrderLink != null) _orderRepository.Delete(sourceCart.OrderLink);
                    continue;
                }

                // deletes cart if account is not bullion, as not doing this will cause errors later in checkout
                if ((sourceCart.Name == _cartService.DefaultBullionCartName ||
                    sourceCart.Name == _cartService.DefaultBuyNowCartName) &&
                    !_bullionContactHelper.IsBullionAccount(customer))
                {
                    if (sourceCart.OrderLink != null) _orderRepository.Delete(sourceCart.OrderLink);
                    continue;
                }

                var currentMarket = _currentMarket.GetCurrentMarket();
                var cart = _orderRepository.Load<ICart>(destinationCustomerId, sourceCart.Name)
                               .FirstOrDefault(c => c.MarketId == currentMarket.MarketId) ??
                           _orderRepository.Load<ICart>(destinationCustomerId, sourceCart.Name)
                               .FirstOrDefault(c => c.MarketId == sourceCart.MarketId);

                //Check for an empty cart
                if (!cart?.GetAllLineItems().Any() ?? false)
                {
                    if (cart?.OrderLink != null) _orderRepository.Delete(cart.OrderLink);
                    cart = null;
                }

                if (cart == null)
                {
                    cart = _orderRepository.Create<ICart>(destinationCustomerId, sourceCart.Name);
                    cart.MarketId = currentMarket.MarketId;
                    cart.Currency = _bullionContactHelper.GetDefaultCurrencyCode(customer);
                }

                MergeForms(sourceCart, cart);
                MergeShipments(cart);

                // added to allow assignment of cart to account with currency other than default ie USD, EUR
                // such when an item is added to the basket, and a USD customer subsequently logs in,
                // by reusing IAmShippingMethodHelper.UpdateBullionShippingMethod

                if (cart.Name == _cartService.DefaultBullionCartName ||
                    cart.Name == _cartService.DefaultBuyNowCartName)
                {
                    var shipments =
                        from f in cart.Forms
                        from s in f.Shipments
                        select s;

                    foreach (var shipment in shipments)
                    {
                        _shippingMethodHelper.UpdateBullionShippingMethod(cart, shipment.ShipmentId, customer);
                    }
                }

                _cartService.ValidateCart(cart);
                _orderRepository.Save(cart);

                try
                {
                    if (sourceCart.OrderLink != null) _orderRepository.Delete(sourceCart.OrderLink);
                }
                catch (NullReferenceException)
                {
                    //Caused by the source cart having already been deleted
                }
            }
        }

        private void MergeShipments(ICart cart)
        {
            foreach (var form in cart.Forms)
            {
                var shipments = form.Shipments;
                var shipmentsToRemove = new List<IShipment>();
                for (var i = 0; i < shipments.Count - 1; i++)
                {
                    var shipment = shipments.ElementAt(i);
                    for (var j = i + 1; j < shipments.Count; j++)
                    {
                        var nextShipment = shipments.ElementAt(j);
                        if (!shipment.LineItems.Any() || shipment.WarehouseCode == nextShipment.WarehouseCode)
                        {
                            if (MergeTwoShipments(nextShipment, shipment, cart.Name == DefaultCartNames.Default))
                                shipmentsToRemove.Add(nextShipment);
                        }
                    }
                }

                shipmentsToRemove.ForEach(s => form.Shipments.Remove(s));
            }
        }

        private bool MergeTwoShipments(IShipment sourceShipment, IShipment destinationShipment, bool isDefault)
        {
            if (!isDefault)
            {
                var shipForSource = sourceShipment.LineItems.All(x => x.Properties.ContainsKey(CustomFields.BullionDeliver) && (int)x.Properties[CustomFields.BullionDeliver] == 1);
                var shipForDestination = destinationShipment.LineItems.All(x => x.Properties.ContainsKey(CustomFields.BullionDeliver) && (int)x.Properties[CustomFields.BullionDeliver] == 1);

                if (shipForSource != shipForDestination)
                    return false;
            }
            foreach (var li in sourceShipment.LineItems)
            {
                var lineItem = destinationShipment.LineItems.FirstOrDefault(l => l.Code == li.Code && _cartService.GetPwOrderId(l) == _cartService.GetPwOrderId(li));
                
                if (lineItem == null)
                {
                    destinationShipment.LineItems.Add(li);
                }
                else
                {
                    lineItem.Quantity += li.Quantity;
                }
            }
            return true;
        }

        private void MergeForms(ICart sourceCart, ICart destinationCart)
        {
            //adds new forms directly to cart.Forms
            var newForms = sourceCart.Forms.Where(x => !destinationCart.Forms.Any(y => y.Name == x.Name)).ToList();
            foreach (var form in newForms)
            {
                destinationCart.Forms.Add(form);
            }

            //merges forms with the same name
            foreach (var mergingForm in sourceCart.Forms.Except(newForms))
            {
                var form = destinationCart.Forms.First(f => f.Name == mergingForm.Name);
                foreach (var shipment in mergingForm.Shipments)
                {
                    //This is important, make sure we access the merging shipment's lineitem
                    //Otherwise it will used information from form to get line items, which is incorrect
                    if (shipment.LineItems.Any())
                    {
                        form.Shipments.Add(shipment);
                    }
                }

                foreach (var coupon in mergingForm.CouponCodes)
                {
                    form.CouponCodes.Add(coupon);
                }

                foreach (var payment in mergingForm.Payments)
                {
                    form.Payments.Add(payment);
                }

                foreach (var promotion in mergingForm.Promotions)
                {
                    promotion.OrderFormId = form.OrderFormId;
                    form.Promotions.Add(promotion);
                }

                form.CopyPropertiesFrom(mergingForm);

            }
        }

        #endregion
    }
}