<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigureShippingMethod.ascx.cs" Inherits="TrmShippingProvider.ConfigureShippingMethod" %>
<%@ Register TagPrefix="asp" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit, Version=3.0.30930.28736, Culture=neutral, PublicKeyToken=28f01b0e84b6d53e" %>
        <style type="text/css">
            .auto-style1 {
                width: 130px;
            }
        </style>
        <table width="600" runat="server">
            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">OBS Method Name:</td>
                <td class="FormLabelCell" align="left">
                    <asp:TextBox ID="txtMethodName" runat="server" Width="97px"></asp:TextBox>
                </td>
            </tr>

            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">For Gifting Only:</td>
                <td class="FormLabelCell" align="left">
                    <asp:Checkbox ID="cboForGiftingOnly" runat="server"></asp:Checkbox>
                </td>
            </tr>
            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">Minimum Order Value:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:TextBox runat="server" Width="100px" ID="txtMinOrderValue"></asp:TextBox>&nbsp;&nbsp;&nbsp;
                    <asp:RequiredFieldValidator runat="server" ID="RequiredFieldValidator1" ControlToValidate="txtMinOrderValue" Display="Dynamic" ErrorMessage="Minimum Order Value Required" ValidationGroup="MinOrderAmountValidationGroup" />
                    <asp:RangeValidator runat="server" ID="RangeValidator1" ControlToValidate="txtMinOrderValue" MinimumValue="0" MaximumValue="1000000000" Type="Double" Display="Dynamic" ErrorMessage="<%$ Resources:SharedStrings, Enter_Valid_Value %>" ValidationGroup="MinOrderAmountValidationGroup"></asp:RangeValidator>
                </td>
            </tr>
            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">Maximum Order Value:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:TextBox runat="server" Width="100px" ID="txtMaxOrderValue"></asp:TextBox>&nbsp;&nbsp;&nbsp;
                    <asp:RequiredFieldValidator runat="server" ID="RequiredFieldValidator2" ControlToValidate="txtMaxOrderValue" Display="Dynamic" ErrorMessage="Maximum Order Value Required" ValidationGroup="MaxOrderAmountValidationGroup" />
                    <asp:RangeValidator runat="server" ID="RangeValidator2" ControlToValidate="txtMaxOrderValue" MinimumValue="0" MaximumValue="1000000000" Type="Double" Display="Dynamic" ErrorMessage="<%$ Resources:SharedStrings, Enter_Valid_Value %>" ValidationGroup="MaxOrderAmountValidationGroup"></asp:RangeValidator>
                </td>
            </tr>

            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">Min Weight Value:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:TextBox runat="server" Width="100px" ID="txtMinWeight"></asp:TextBox>&nbsp;&nbsp;&nbsp;
                    <asp:RangeValidator runat="server" ID="RangeValidator3" ControlToValidate="txtMinWeight" MinimumValue="0" MaximumValue="1000000000" Type="Double" Display="Dynamic" ErrorMessage="<%$ Resources:SharedStrings, Enter_Valid_Value %>" ValidationGroup="MinWeightValidationGroup"></asp:RangeValidator>
                </td>
            </tr>
            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">Max Weight Value:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:TextBox runat="server" Width="100px" ID="txtMaxWeight"></asp:TextBox>&nbsp;&nbsp;&nbsp;
                    <asp:RangeValidator runat="server" ID="RangeValidator4" ControlToValidate="txtMaxWeight" MinimumValue="0" MaximumValue="1000000000" Type="Double" Display="Dynamic" ErrorMessage="<%$ Resources:SharedStrings, Enter_Valid_Value %>" ValidationGroup="MaxWeightValidationGroup"></asp:RangeValidator>
                </td>
            </tr>

            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">All order items must be in stock:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:CheckBox runat="server" Width="100px" ID="AllItemsInStock"></asp:CheckBox>
                </td>
            </tr>
            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">Cut Off Time:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:TextBox runat="server" Width="60" ID="cutOffTime"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td class="auto-style1" style="vertical-align: middle; ">Days Available:
                </td>
                <td class="FormLabelCell" align="left">
                    <asp:CheckBox runat="server" Width="100px" ID="availableMonday" Text="Monday"></asp:CheckBox><br/>
                    <asp:CheckBox runat="server" Width="100px" ID="availableTuesday" Text="Tuesday"></asp:CheckBox><br/>
                    <asp:CheckBox runat="server" Width="100px" ID="availableWednesday" Text="Wednesday"></asp:CheckBox><br/>
                    <asp:CheckBox runat="server" Width="100px" ID="availableThursday" Text="Thursday"></asp:CheckBox><br/>
                    <asp:CheckBox runat="server" Width="100px" ID="availableFriday" Text="Friday"></asp:CheckBox><br/>
                    <asp:CheckBox runat="server" Width="100px" ID="availableSaturday" Text="Saturday"></asp:CheckBox><br/>
                    <asp:CheckBox runat="server" Width="100px" ID="availableSunday" Text="Sunday"></asp:CheckBox>
                </td>
            </tr>
        </table>
