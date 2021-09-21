<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Control Language="C#" Inherits="ViewUserControl<NumberElementBlock>" %>

<%  var formElement = Model.FormElement; 
    var labelText = Model.Label;
    string requiredAsterik;
    var check = Model.GetValidationCssClasses();

    if (check.Contains("ValidationRequired"))
    {
        requiredAsterik = "*";
    }
    else
    {
        requiredAsterik = string.Empty;
    }
%>

<% using(Html.BeginElement(Model, new { @class="FormTextbox FormTextbox--Number" + Model.GetValidationCssClasses(), @data_f_type="textbox", data_f_modifier="number" })) { %>
    <label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%: labelText %><span class="text-danger"><%:requiredAsterik %></span></label>
    <input name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" type="text" placeholder="<%: Model.PlaceHolder %>"
        class="FormTextbox__Input form-control" <%: Html.Raw(Model.AttributesString) %> data-f-datainput
        value="<%: Model.GetDefaultValue() %>" />
<span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
 <%= Model.RenderDataList() %>

<% } %>
