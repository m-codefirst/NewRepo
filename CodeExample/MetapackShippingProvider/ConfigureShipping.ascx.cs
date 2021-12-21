using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Data;
using System.Linq;
using static TRM.Shared.Constants.StringConstants;

namespace MetapackShippingProvider
{
    public partial class ConfigureShipping : OrderBaseUserControl, IGatewayControl
    {
        private ShippingMethodDto _shippingMethodDto;

        public string ValidationGroup { get; set; } = string.Empty;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            BindData();
        }

        public override void DataBind()
        {
            if (IsPostBack) return;

            BindData();
            base.DataBind();
        }

        public void BindData()
        {
            if (_shippingMethodDto?.ShippingOptionParameter != null)
            {
                var apiUrl =
                    _shippingMethodDto.ShippingOptionParameter
                    .FirstOrDefault(x => x.Parameter == ShippingParameters.ApiUrl);
                if (apiUrl != null)
                {
                    txtApiUrl.Text = apiUrl.Value;
                }
                Visible = true;
            }
            else
                Visible = false;
        }

        public void SaveChanges(object dto)
        {
            _shippingMethodDto = dto as ShippingMethodDto;

            if (_shippingMethodDto.ShippingOptionParameter != null && _shippingMethodDto.ShippingOptionParameter.Rows.Count > 0)
            {
                AddParamValueForSave(ShippingParameters.ApiUrl, txtApiUrl.Text, _shippingMethodDto);
            }
            else
            {
                // First Time Brand New Param Values
                var newApiUrl = NewParameterRow(ShippingParameters.ApiUrl, txtApiUrl.Text, _shippingMethodDto);

                var optionParameter = _shippingMethodDto.ShippingOptionParameter;
                if (optionParameter != null)
                {
                    optionParameter.Rows.Add(newApiUrl);
                }
            }
        }

        public void LoadObject(object dto)
        {
            _shippingMethodDto = dto as ShippingMethodDto;
        }

        private DataRow NewParameterRow(string paramName, string paramValue, ShippingMethodDto shippingDto)
        {
            var newParam = shippingDto.ShippingOptionParameter.NewShippingOptionParameterRow();
            newParam.Parameter = paramName;
            newParam.Value = paramValue;
            newParam.ShippingOptionId = shippingDto.ShippingOption[0].ShippingOptionId;
            return newParam;
        }

        private void AddParamValueForSave(string paramName, string paramValue, ShippingMethodDto shippingDto)
        {
            var optionParameterRow = shippingDto.ShippingOptionParameter.FirstOrDefault(x => x.Parameter == paramName);
            if (optionParameterRow != null)
            {
                optionParameterRow.Value = paramValue;
            }
            else
            {
                var newParameterRow = NewParameterRow(paramName, paramValue, shippingDto);
                var optionParameter = shippingDto.ShippingOptionParameter;
                optionParameter?.Rows.Add((DataRow)newParameterRow);
            }
        }
    }
}