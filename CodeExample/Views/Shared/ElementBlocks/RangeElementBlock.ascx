<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>

<%@ Control Language="C#" Inherits="ViewUserControl<RangeElementBlock>" %>

<%
    var formElement = Model.FormElement;
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

<% using(Html.BeginElement(Model, new { @class="FormRange" + Model.GetValidationCssClasses(),  data_f_type="range" })) { %>
    <label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%:labelText %><span class="text-danger"><%:requiredAsterik %></span></label>
    <span>
        <span class="FormRange__Min"><%: Model.Min %></span>
        <input name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" type="range" class="FormRange__Input"
               value="<%: Model.GetRangeValue() %>" data-f-datainput
               min="<%: Model.Min %>" max="<%: Model.Max %>" step="<%: Model.Step %>" <%: Html.Raw(Model.AttributesString) %> />
        <span class="FormRange__Max"><%: Model.Max %></span>
    </span>
    <span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
<span class="trmi form-control-feedback" aria-hidden="true"></span>
<% } %>