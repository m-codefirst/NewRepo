using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;
using System;
using System.Data;
using System.Linq;
using static TRM.Shared.Constants.StringConstants;

namespace MetapackShippingProvider
{
    public partial class ConfigureShippingMethod : OrderBaseUserControl, IGatewayControl
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
            if (this._shippingMethodDto != null && this._shippingMethodDto.ShippingMethod.Count > 0)
            {
                if (_shippingMethodDto != null && _shippingMethodDto.ShippingMethodParameter != null &&
                    _shippingMethodDto.ShippingMethodParameter.Rows.Count > 0)
                {
                    var shippingMethodRow =
                        this._shippingMethodDto.ShippingMethodParameter.FirstOrDefault(
                            x => x.Parameter == ShippingParameters.UserKey);
                    if (shippingMethodRow != null)
                    {
                        txtUserKey.Text = shippingMethodRow.Value;
                    }
                }
            }
            else
                this.Visible = false;
        }

        public void SaveChanges(object dto)
        {
            var shippingDto = (ShippingMethodDto)dto;
            if (shippingDto.ShippingMethodParameter != null && shippingDto.ShippingMethodParameter.Rows.Count > 0)
            {
                AddParamValueForSave(ShippingParameters.UserKey, txtUserKey.Text, shippingDto);
            }
            else
            { // First Time Brand New Param Values
                var newUserKey = NewParameterRow(ShippingParameters.UserKey, txtUserKey.Text, shippingDto);
                
                var methodParameter = shippingDto.ShippingMethodParameter;
                if (methodParameter != null)
                {
                    methodParameter.Rows.Add(newUserKey);
                }
            }

            this._shippingMethodDto = shippingDto;
        }

        public void LoadObject(object dto)
        {
            this._shippingMethodDto = dto as ShippingMethodDto;
        }

        private DataRow NewParameterRow(string paramName, string paramValue, ShippingMethodDto shippingDto)
        {
            var newParam = shippingDto.ShippingMethodParameter.NewShippingMethodParameterRow();
            newParam.Parameter = paramName;
            newParam.Value = paramValue;
            newParam.ShippingMethodId = shippingDto.ShippingMethod[0].ShippingMethodId;
            return newParam;
        }

        private void AddParamValueForSave(string paramName, string paramValue, ShippingMethodDto shippingDto)
        {
            var methodParameterRow = shippingDto.ShippingMethodParameter.FirstOrDefault(x => x.Parameter == paramName);
            if (methodParameterRow != null)
            {
                methodParameterRow.Value = paramValue;
            }
            else
            {
                var newParameterRow = NewParameterRow(paramName, paramValue, shippingDto);
                var methodParameter = shippingDto.ShippingMethodParameter;
                methodParameter?.Rows.Add((DataRow)newParameterRow);
            }
        }
    }
}