using System.Collections.Generic;
using EPiServer.Find.Helpers;
using EPiServer.Find.Helpers.Text;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.Logging.Compatibility;
using EPiServer.ServiceLocation;
using TRM.Web.Constants;
using TRM.Web.Models.Catalog.DDS;
using TRM.Web.Models.DTOs;
using TRM.Web.Models.Interfaces.EntryProperties;

namespace TRM.Web.Helpers
{
    public class SpecificationHelper : IAmSpecificationHelper
    {
        protected readonly ILog Logger = LogManager.GetLogger(typeof(SpecificationHelper));
        private readonly LocalizationService _localizationService;
        private readonly IServiceLocator _serviceLocator;

        public SpecificationHelper(LocalizationService localizationService, 
                                   IServiceLocator serviceLocator)
        {
            _localizationService = localizationService;
            _serviceLocator = serviceLocator;
        }

        public List<SpecificationItemDto> GetSpecificationItems(IHaveAProductSpecification aProductSpecification)
        {
            var specificationItems = new List<SpecificationItemDto>();

            if (!string.IsNullOrWhiteSpace(aProductSpecification.RankedDenominationDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecDenomination, "Denomination"),
                    Value = aProductSpecification.RankedDenominationDisplayName
                });               
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.MaximumCoinMintage))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecMaximumCoinMintage, "Maximum Coin Mintage"),
                    Value = aProductSpecification.MaximumCoinMintage
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.AlloyDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecAlloy, "Alloy"),
                    Value = aProductSpecification.AlloyDisplayName
                });
            }
            if (aProductSpecification.SpecifiedWeight.HasValue)
            {
                var weight = string.Format("{0:F}{1}", aProductSpecification.SpecifiedWeight.Value, _localizationService.GetString(StringResources.EntrySpecGrammes, "g"));

                if (aProductSpecification.WeightUnit.IsNotNullOrEmpty())
                {
                    weight = aProductSpecification.WeightWithUnits;
                }
                
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecWeight, "Weight"),
                    Value = weight
                });
            }
            if (aProductSpecification.SpecifiedDiameter.HasValue)
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecDiameter, "Diameter"),
                    Value = string.Format("{0:F}{1}", aProductSpecification.SpecifiedDiameter.Value, _localizationService.GetString(StringResources.EntrySpecMillimeters, "mm"))
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.ReverseDesigner))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecReverseDesigner, "Reverse Designer"),
                    Value = aProductSpecification.ReverseDesigner
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.ObverseDesigner))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecObverseDesigner, "Obverse Designer"),
                    Value = aProductSpecification.ObverseDesigner
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.EdgeInscription))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecEdgeInscription, "Edge Inscription"),
                    Value = aProductSpecification.EdgeInscription
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.QualityDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecQuality, "Quality"),
                    Value = aProductSpecification.QualityDisplayName
                });
            }
            if (aProductSpecification.YearSpecified.IsNotNull())
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecYear, "Year"),
                    Value = aProductSpecification.YearSpecified
                });
            }
            if (aProductSpecification.NumberOfItems > 0)
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecNumberOfItems, "Number Of Items"),
                    Value = aProductSpecification.NumberOfItems.ToString()
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.PureMetalTypeDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecPureMetalType, "Pure Metal Type"),
                    Value = aProductSpecification.PureMetalTypeDisplayName
                });               
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.SpecifiedPureMetalContent))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecPureMetalContent, "Pure Metal Content"),
                    Value = aProductSpecification.SpecifiedPureMetalContent
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.Fineness))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecFineness, "Fineness"),
                    Value = aProductSpecification.Fineness
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.Packaging))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecPackaging, "Packaging"),
                    Value = aProductSpecification.Packaging
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.BarSize))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecBarSize, "Bar Size"),
                    Value = aProductSpecification.BarSize
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.BarThickness))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecBarThickness, "Bar Thickness"),
                    Value = aProductSpecification.BarThickness
                });
            }
            if (!string.IsNullOrWhiteSpace(aProductSpecification.OtherSpecificationValue) && !string.IsNullOrWhiteSpace(aProductSpecification.OtherSpecificationLabel))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = aProductSpecification.OtherSpecificationLabel,
                    Value = aProductSpecification.OtherSpecificationValue
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.Volume))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecVolume, "Volume"),
                    Value = aProductSpecification.VolumeWithUnits
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.Contains))
            {
                var value = aProductSpecification.Contains;
                value = value.Replace(",", ", ");
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecContains, "Contains"),
                    Value = value
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.Length))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecLength, "Length"),
                    Value = aProductSpecification.LengthWithUnits
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.Width))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecWidth, "Width"),
                    Value = aProductSpecification.WidthWithUnits
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.Height))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecHeight, "Height"),
                    Value = aProductSpecification.HeightWithUnits
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.CountryOfOrigin))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecCountryOfOrigin, "Country Of Origin"),
                    Value = aProductSpecification.CountryOfOrigin
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.MaterialDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecMaterial, "Material"),
                    Value = aProductSpecification.MaterialDisplayName
                });
            }

            if (aProductSpecification.IsSuitableForVegetarians)
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecSuitableForVegetarians, "Suitable For Vegetarians"),
                    Value = _localizationService.GetStringByCulture(StringResources.SuitableForVegetarians, StringConstants.TranslationFallback.SuitableForVegetarians, ContentLanguage.PreferredCulture)
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.ColourDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecColour, "Colour"),
                    Value = aProductSpecification.ColourDisplayName
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.EuropeanSizeDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecEuropeanSize, "European Size"),
                    Value = aProductSpecification.EuropeanSizeDisplayName
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.DiamondQualityDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecDiamondQuality, "Diamond Quality"),
                    Value = aProductSpecification.DiamondQualityDisplayName
                });
            }

            if (!string.IsNullOrWhiteSpace(aProductSpecification.UKSizeDisplayName))
            {
                specificationItems.Add(new SpecificationItemDto
                {
                    Label = _localizationService.GetString(StringResources.EntrySpecUKSize, "UK Size"),
                    Value = aProductSpecification.UKSizeDisplayName
                });
            }

            return specificationItems;
        }
    }
}