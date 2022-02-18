using System;
using System.Data;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Web.Console.BaseClasses;
using Mediachase.Web.Console.Interfaces;

namespace EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays
{
    public partial class BarclaysConfigurePayment : OrderBaseUserControl, IGatewayControl
    {
        // Fields
        private PaymentMethodDto _paymentMethodDto;
        private string _validationGroup;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurePayment"/> class.
        /// </summary>
        public BarclaysConfigurePayment()
        {
            this._validationGroup = string.Empty;
            this._paymentMethodDto = null;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            this.BindData();
        }

        /// <summary>
        /// Binds the data.
        /// </summary>
        public void BindData()
        {
            if ((this._paymentMethodDto != null) && (this._paymentMethodDto.PaymentMethodParameter != null))
            {
                PaymentMethodDto.PaymentMethodParameterRow parameterByName = null;
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.SAUrlParameter);
                if (parameterByName != null)
                {
                    this.SAUrl.Text = parameterByName.Value;
                }                
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.SAUrlSilentParameter);
                if (parameterByName != null)
                {
                    this.SAUrlSilent.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.MerchantIdParameter);
                if (parameterByName != null)
                {
                    this.MerchantID.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.ProfileID);
                if (parameterByName != null)
                {
                    this.ProfileID.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.AccessKey);
                if (parameterByName != null)
                {
                    this.AccessKey.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.SecretKey);
                if (parameterByName != null)
                {
                    this.SecretKey.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.CapturePayment);
                if (parameterByName != null)
                {
                    this.CapturePayment.Checked = parameterByName.Value == "1";
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.UsProfileID);
                if (parameterByName != null)
                {
                    this.UsProfileID.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.UsAccessKey);
                if (parameterByName != null)
                {
                    this.UsAccessKey.Text = parameterByName.Value;
                }
                parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.UsSecretKey);
                if (parameterByName != null)
                {
                    this.UsSecretKey.Text = parameterByName.Value;
                }
            }
            else
            {
                this.Visible = false;
            }
        }

        private PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(string name)
        {
            return this._paymentMethodDto.GetParameterByName(name);
        }

        /// <summary>
        /// Create parameters (settings) for paymentMethodDto (which is Mastercard).
        /// <example>APIUsername, APIPassword, MastercardCheckoutUrl, ...</example>
        /// </summary>
        /// <param name="dto">the Mastercard payment method</param>
        /// <param name="name">param's name</param>
        /// <param name="value">param's value</param>
        /// <param name="paymentMethodId">id of Mastercard payment method</param>
        private void CreateParameter(PaymentMethodDto dto, string name, string value, Guid paymentMethodId)
        {
            PaymentMethodDto.PaymentMethodParameterRow row = dto.PaymentMethodParameter.NewPaymentMethodParameterRow();
            row.PaymentMethodId = paymentMethodId;
            row.Parameter = name;
            row.Value = value;
            if (row.RowState == DataRowState.Detached)
            {
                dto.PaymentMethodParameter.Rows.Add(row);
            }
        }
        #region IGatewayControl Members

        /// <summary>
        /// Loads the PaymentMethodDto object
        /// </summary>
        /// <param name="dto">The PaymentMethodDto object</param>
        public void LoadObject(object dto)
        {
            this._paymentMethodDto = dto as PaymentMethodDto;
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="dto">The dto.</param>
        public void SaveChanges(object dto)
        {
            if (this.Visible)
            {
                this._paymentMethodDto = dto as PaymentMethodDto;
                if ((this._paymentMethodDto != null) && (this._paymentMethodDto.PaymentMethodParameter != null))
                {
                    Guid paymentMethodId = Guid.Empty;
                    if (this._paymentMethodDto.PaymentMethod.Count > 0)
                    {
                        paymentMethodId = this._paymentMethodDto.PaymentMethod[0].PaymentMethodId;
                    }

                    PaymentMethodDto.PaymentMethodParameterRow parameterByName = null;
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.SAUrlParameter);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.SAUrl.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.SAUrlParameter, this.SAUrl.Text, paymentMethodId);
                    }                    
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.SAUrlSilentParameter);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.SAUrlSilent.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.SAUrlSilentParameter, this.SAUrlSilent.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.MerchantIdParameter);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.MerchantID.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.MerchantIdParameter, this.MerchantID.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.ProfileID);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.ProfileID.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.ProfileID, this.ProfileID.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.AccessKey);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.AccessKey.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.AccessKey, this.AccessKey.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.SecretKey);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.SecretKey.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.SecretKey, this.SecretKey.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.CapturePayment);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.CapturePayment.Checked ? "1" : "0";
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.CapturePayment, (this.CapturePayment.Checked ? "1" : "0"), paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.UsProfileID);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.UsProfileID.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.UsProfileID, this.UsProfileID.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.UsAccessKey);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.UsAccessKey.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.UsAccessKey, this.UsAccessKey.Text, paymentMethodId);
                    }
                    parameterByName = this.GetParameterByName(BarclaysCardPaymentGateway.UsSecretKey);
                    if (parameterByName != null)
                    {
                        parameterByName.Value = this.UsSecretKey.Text;
                    }
                    else
                    {
                        this.CreateParameter(this._paymentMethodDto, BarclaysCardPaymentGateway.UsSecretKey, this.UsSecretKey.Text, paymentMethodId);
                    }

                }
            }

        }

        /// <summary>
        /// Gets or sets the validation group.
        /// </summary>
        /// <value>The validation group.</value>
        public string ValidationGroup
        {
            get
            {
                return _validationGroup;
            }
            set
            {
                _validationGroup = value;
            }
        }

        #endregion
    }
}