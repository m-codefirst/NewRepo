<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Control Language="C#" Inherits="ViewUserControl<TextareaElementBlock>" %>

<%  var formElement = Model.FormElement; 
    var labelText = Model.Label;

    string requiredAsterisk;
    var check = Model.GetValidationCssClasses();

    if (check.Contains("ValidationRequired"))
    {
        requiredAsterisk = "*";
    }
    else
    {
        requiredAsterisk = string.Empty;
    }
%>
<% using(Html.BeginElement(Model, new { @class="FormTextbox FormTextbox--Textarea form-group has-feedback" + Model.GetValidationCssClasses(), data_f_type="textbox", data_f_modifier="textarea" })) { %>
    <label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%: labelText %><span class="text-danger"><%:requiredAsterisk %></span></label>
    <textarea name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" class="FormTextbox__Input form-control"
        placeholder="<%: Model.PlaceHolder %>" data-f-label="<%: labelText %>" data-f-datainput
        <%= Model.AttributesString %> ><%: Model.GetDefaultValue() %></textarea>
<span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;" aria-hidden="true">*</span>
   <% } %>
