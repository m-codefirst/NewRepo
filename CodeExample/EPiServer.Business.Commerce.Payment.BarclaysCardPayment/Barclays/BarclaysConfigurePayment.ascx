<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="BarclaysConfigurePayment.ascx.cs" Inherits="EPiServer.Business.Commerce.Payment.BarclaysCardPayment.Barclays.BarclaysConfigurePayment" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
	    <tr>
		    <td class="FormLabelCell" colspan="2"><h1><asp:Literal ID="Literal1" runat="server" Text="Configure Barclays Accounts" /></h1></td>
	    </tr>
	    <tr>
		    <td class="FormLabelCell" colspan="2"><b><asp:Literal ID="Literal10" runat="server" Text="Barclays Card Settings:" /></b></td>
	    </tr>
    </table>
    <br />
    <table class="DataForm">
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal2" runat="server" Text="Secure Acceptance URL" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="SAUrl" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal3" runat="server" Text="Secure Acceptance URL" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="SAUrlSilent" Width="300px" MaxLength="1000"></asp:TextBox>
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
              <td class="FormLabelCell"><asp:Literal ID="Literal14" runat="server" Text="Profile ID" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="ProfileID" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal15" runat="server" Text="Access Key" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="AccessKey" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal16" runat="server" Text="Secret Key" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="SecretKey" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>      
        <tr>
            <td class="FormLabelCell"><asp:Literal ID="Literal12" runat="server" Text="Capture Payment using PAY" />:</td>
            <td class="FormFieldCell">
                <asp:CheckBox Runat="server" ID="CapturePayment"></asp:CheckBox>
            </td>
        </tr>     
<!-- US Customers -->
        <tr>
		    <td class="FormLabelCell" colspan="2">
                <p>
                    <br />
                    <b><asp:Literal ID="Literal9" runat="server" Text="US Customer Settings:" /></b></p>
            </td>
	    </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal6" runat="server" Text="Profile ID" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="UsProfileID" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal7" runat="server" Text="Access Key" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="UsAccessKey" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal8" runat="server" Text="Secret Key" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="UsSecretKey" Width="300px" MaxLength="1000"></asp:TextBox>
	          </td>
        </tr>    
    </table>
</div>