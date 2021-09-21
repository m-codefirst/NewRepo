<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Control Language="C#" Inherits="ViewUserControl<SubmitButtonElementBlock>" %>

<%
    var formElement = Model.FormElement;
    var buttonText = Model.Label;

    var buttonDisableState = Model.GetFormSubmittableStatus(ViewContext.Controller.ControllerContext.HttpContext);

    var isNewDesign = Convert.ToBoolean(ViewContext.Controller.ControllerContext.ParentActionViewContext.ViewData["isNewDesign"]);
    var btnPrimaryOrSecondary = isNewDesign ? "btn-primary" : "btn-secondary";
%>

<button id="<%: formElement.Guid %>" name="submit" type="submit" value="<%: formElement.Guid %>" data-f-is-finalized="<%: Model.FinalizeForm.ToString().ToLower() %>"
    data-f-is-progressive-submit="true" data-f-type="submitbutton"
    <%= Model.AttributesString %> <%: buttonDisableState %>
    <% if (Model.Image == null)
    { %>
    class="Form__Element FormExcludeDataRebind FormSubmitButton btn <%: btnPrimaryOrSecondary %>">
    <%: buttonText %>
    <% }
    else
    { %>
        class="Form__Element FormExcludeDataRebind FormSubmitButton FormImageSubmitButton btn btn-secondary">
        <img src="<%: Model.Image.Path %>" data-f-is-progressive-submit="true" data-f-is-finalized="<%: Model.FinalizeForm.ToString().ToLower() %>" />
    <% } %>
</button>
