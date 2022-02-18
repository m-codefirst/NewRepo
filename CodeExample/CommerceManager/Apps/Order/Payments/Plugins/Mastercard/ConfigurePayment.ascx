<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="EPiServer.Business.Commerce.Payment.Mastercard.Mastercard.ConfigurePayment" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
	    <tr>
		    <td class="FormLabelCell" colspan="2"><h1><asp:Literal ID="Literal1" runat="server" Text="Configure Mastercard Accounts" /></h1></td>
	    </tr>
	    <tr>
		    <td class="FormLabelCell" colspan="2"><b><asp:Literal ID="Literal10" runat="server" Text="Mastercard Settings:" /></b></td>
	    </tr>
    </table>
    <br />
    <table class="DataForm">
         <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal4" runat="server" Text="Session.js URL" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="SessionJsUrl" Width="300px" MaxLength="250"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="SessionJsUrl" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Session.js Url required" runat="server" id="Requiredfieldvalidator2"></asp:RequiredFieldValidator>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal2" runat="server" Text="API URL Stub" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="APIUrlStub" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>       
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal5" runat="server" Text="Merchant ID" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="MerchantID" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>       
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="lbl123" runat="server" Text="API Password" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="APIPassword" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell"><asp:Literal ID="Literal12" runat="server" Text="Capture Payment" />:</td>
            <td class="FormFieldCell">
                <asp:CheckBox Runat="server" ID="CapturePayment"></asp:CheckBox>
            </td>
        </tr> 
    </table>
    <table cellpadding="0" cellspacing="2">
	    <tr>
		    <td class="FormLabelCell" colspan="2"><b><asp:Literal ID="Literal3" runat="server" Text="Americal Express Settings:" /></b></td>
	    </tr>
    </table>
    <br />
    <table class="DataForm">
         <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal6" runat="server" Text="Session.js URL" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="AmexSessionJsUrl" Width="300px" MaxLength="250"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="SessionJsUrl" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Session.js Url required" runat="server" id="Requiredfieldvalidator1"></asp:RequiredFieldValidator>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal7" runat="server" Text="API URL Stub" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="AmexAPIUrlStub" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>       
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal8" runat="server" Text="Merchant ID" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="AmexMerchantID" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>       
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal9" runat="server" Text="API Password" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="AmexAPIPassword" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell"><asp:Literal ID="Literal13" runat="server" Text="Capture Payment" />:</td>
            <td class="FormFieldCell">
                <asp:CheckBox Runat="server" ID="AmexCapturePayment"></asp:CheckBox>
            </td>
        </tr>       
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
            <td class="FormLabelCell"><asp:Literal ID="Literal11" runat="server" Text="Country Codes For Amex Payments (comma separated)" />:</td>
            <td class="FormFieldCell">
                <asp:TextBox Runat="server" ID="AmexCountryCodes" Width="300px" MaxLength="1000"></asp:TextBox>
            </td>
        </tr>       
    </table>
</div>