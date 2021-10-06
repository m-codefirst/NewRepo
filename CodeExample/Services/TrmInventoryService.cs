using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.InventoryService;
using TRM.Web.Constants;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces;

namespace TRM.Web.Services
{
    public class TrmInventoryService : IInventoryService, ITrmInventoryService
    {
        protected readonly IInventoryService EpiInventoryService;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;

        public TrmInventoryService(ReferenceConverter referenceConverter, IContentLoader contentLoader)
        {
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
            EpiInventoryService = ServiceLocator.Current.GetInstance<IInventoryService>(StringConstants.EpiInventoryService);
        }

        public InventoryRecord Get(string catalogEntryCode, string warehouseCode)
        {
            catalogEntryCode = GetOutSourcedAndSourceCodes(catalogEntryCode).CodeToUse;
            return EpiInventoryService.Get(catalogEntryCode, warehouseCode);
        }

        private IEnumerable<string> GetOutSourcedCodes(IEnumerable<string> catalogEntryCodes)
        {
            var entryCodes = catalogEntryCodes as IList<string> ?? catalogEntryCodes.ToList();
            return entryCodes.Select(x => GetOutSourcedAndSourceCodes(x).CodeToUse).ToList();
        }

        public IList<InventoryRecord> List()
        {
            return EpiInventoryService.List();
        }

        public IList<InventoryRecord> QueryByEntry(IEnumerable<string> catalogEntryCodes)
        {
            var codesToQuery = GetOutSourcedCodes(catalogEntryCodes);
            return EpiInventoryService.QueryByEntry(codesToQuery);
        }

        public IList<InventoryRecord> QueryByEntry(IEnumerable<string> catalogEntryCodes, int offset, int count, out int totalCount)
        {
            var codesToQuery = GetOutSourcedCodes(catalogEntryCodes);
            return EpiInventoryService.QueryByEntry(codesToQuery, offset, count, out totalCount);
        }

        public IList<InventoryRecord> QueryByWarehouse(IEnumerable<string> warehouseCodes)
        {
            return EpiInventoryService.QueryByWarehouse(warehouseCodes);
        }

        public IList<InventoryRecord> QueryByWarehouse(IEnumerable<string> warehouseCodes, int offset, int count, out int totalCount)
        {
            return EpiInventoryService.QueryByWarehouse(warehouseCodes, offset, count, out totalCount);
        }

        public IList<InventoryRecord> QueryByPartialKey(IEnumerable<InventoryKey> partialKeys)
        {
            var outSourcedKeys = GetOutSourcedKeys(partialKeys);
            return EpiInventoryService.QueryByPartialKey(outSourcedKeys);
        }

        public IList<InventoryRecord> QueryByPartialKey(IEnumerable<InventoryKey> partialKeys, int offset, int count, out int totalCount)
        {
            var outSourcedKeys = GetOutSourcedKeys(partialKeys);
            return EpiInventoryService.QueryByPartialKey(outSourcedKeys, offset, count, out totalCount);
        }

        private IEnumerable<InventoryKey> GetOutSourcedKeys(IEnumerable<InventoryKey> partialKeys)
        {
            var outSourcedKeys = new List<InventoryKey>();
            foreach (var key in partialKeys)
            {
                var outSourcedKeyCode = GetOutSourcedAndSourceCodes(key.CatalogEntryCode).CodeToUse; 
                var outSourcedKey = new InventoryKey()
                {
                    CatalogEntryCode = outSourcedKeyCode,
                    WarehouseCode = key.WarehouseCode
                };
                outSourcedKeys.Add(outSourcedKey);
            }
            return outSourcedKeys;
        }

        public InventoryResponse Request(InventoryRequest request)
        {
            var newInventoryRequest = new InventoryRequest
            {
                RequestDateUtc = request.RequestDateUtc,
                Context = request.Context,
                Items = new List<InventoryRequestItem>()
            };
            foreach (var originalRequestItem in request.Items)
            {
                newInventoryRequest.Items.Add(GetOutSourcedInventoryRequestItem(originalRequestItem));
            }
            return EpiInventoryService.Request(newInventoryRequest);
        }

        private InventoryRequestItem GetOutSourcedInventoryRequestItem(InventoryRequestItem originalItem)
        {
            var outSourcedItem = new InventoryRequestItem
            {
                CatalogEntryCode = GetOutSourcedAndSourceCodes(originalItem.CatalogEntryCode).CodeToUse,
                Context = originalItem.Context,
                ItemIndex = originalItem.ItemIndex,
                OperationKey = originalItem.OperationKey,
                Quantity = originalItem.Quantity,
                RequestType = originalItem.RequestType,
                WarehouseCode = originalItem.WarehouseCode
            };
            return outSourcedItem;
        }

        public void Save(IEnumerable<InventoryRecord> records)
        {
            EpiInventoryService.Save(records);
        }

        public void Insert(IEnumerable<InventoryRecord> records)
        {
            EpiInventoryService.Insert(records);
        }

        public void Update(IEnumerable<InventoryRecord> records)
        {
            EpiInventoryService.Update(records);
        }

        public void Delete(IEnumerable<InventoryKey> inventoryKeys)
        {
            var outSourcedKeys = GetOutSourcedKeys(inventoryKeys);
            EpiInventoryService.Delete(outSourcedKeys);
        }

        public void Adjust(IEnumerable<InventoryChange> changes)
        {
            EpiInventoryService.Adjust(changes);
        }

        public void DeleteByEntry(IEnumerable<string> catalogEntryCodes)
        {
            var codesToQuery = GetOutSourcedCodes(catalogEntryCodes);
            EpiInventoryService.DeleteByEntry(codesToQuery);
        }

        public void DeleteByWarehouse(IEnumerable<string> warehouseCode)
        {
            EpiInventoryService.DeleteByWarehouse(warehouseCode);
        }

        public IInventoryService GetCacheSkippingInstance()
        {
            return EpiInventoryService.GetCacheSkippingInstance();
        }

        public OutSourceCodesDto GetOutSourcedAndSourceCodes(string catalogEntryCode)
        {
            var dtoToReturn = new OutSourceCodesDto
            {
                CodeToUse = catalogEntryCode ?? string.Empty,
                BaseCodeIfOutSourced = string.Empty,
                IsOutSourced = false
            };

            if (string.IsNullOrEmpty(catalogEntryCode)) return dtoToReturn;

            var contentReference = _referenceConverter.GetContentLink(catalogEntryCode);
            if (contentReference == null || contentReference.ID <= 0) return dtoToReturn;

            var originalRequestContent = _contentLoader.Get<EntryContentBase>(contentReference);
            if (originalRequestContent == null) return dtoToReturn;

            var outSourcedInventory = originalRequestContent as IOutSourceInventory;
            if (outSourcedInventory == null ||
                outSourcedInventory.OutSrcInventoryRef == null || outSourcedInventory.OutSrcInventoryRef.ID <= 0)
                // if we don't outsource, just use the original and let Epi deal with it
                return dtoToReturn;

            var childReference = outSourcedInventory.OutSrcInventoryRef;
            var childContent = _contentLoader.Get<EntryContentBase>(childReference);
            var childOutSourcedInventory = childContent as IOutSourceInventory;
            if (childOutSourcedInventory == null ||
                childOutSourcedInventory.OutSrcInventoryRef == null ||
                childOutSourcedInventory.OutSrcInventoryRef.ID <= 0)
            {
                // if child doesn't point anywhere, use it
                dtoToReturn.CodeToUse = childContent.Code;
                dtoToReturn.BaseCodeIfOutSourced = catalogEntryCode;
                dtoToReturn.IsOutSourced = true;
            }

            // if child does point somewhere (anywhere - including loop), use the original - this is bad.
            return dtoToReturn;
        }
    }
}