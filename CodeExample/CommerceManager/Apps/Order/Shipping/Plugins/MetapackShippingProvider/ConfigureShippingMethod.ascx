<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigureShippingMethod.ascx.cs" Inherits="MetapackShippingProvider.ConfigureShippingMethod" %>
<%@ Register TagPrefix="asp" Namespace="AjaxControlToolkit" Assembly="AjaxControlToolkit, Version=3.0.30930.28736, Culture=neutral, PublicKeyToken=28f01b0e84b6d53e" %>
<style type="text/css">
    .auto-style1 {
        width: 130px;
    }
</style>
<table width="600" runat="server">
    <tr>
        <td class="auto-style1" style="vertical-align: middle; ">User Key:</td>
        <td class="FormLabelCell" align="left">
            <asp:TextBox ID="txtUserKey" runat="server" Width="97px"></asp:TextBox>
        </td>
    </tr>
</table>
