<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Core" %>
<%@ Import Namespace="EPiServer.Forms.EditView" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Control Language="C#" Inherits="ViewUserControl<VisitorDataHiddenElementBlock>" %>

<% 
    var isViewModeInvisibleElement = Model is IViewModeInvisibleElement;
    var extraCSSClass = isViewModeInvisibleElement ? ConstantsFormsUI.CSS_InvisibleElement : string.Empty;
    var formElement = Model.FormElement;
    
    if (EPiServer.Editor.PageEditing.PageIsInEditMode) { %>
<span class="Form__Element FormHidden <%:extraCSSClass %> "><%: Model.EditViewFriendlyTitle %></span>
<% } else { %>

<input name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" type="hidden" class="Form__Element FormHidden FormHideInSummarized" <%: Html.Raw(Model.AttributesString) %> data-f-type="hidden" />
<% } %>