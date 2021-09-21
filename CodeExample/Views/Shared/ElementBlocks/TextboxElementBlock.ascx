<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ import namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ control language="C#" inherits="ViewUserControl<TextboxElementBlock>" %>
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
<div class="Form__Element FormTextbox form-group has-feedback <%: Model.GetValidationCssClasses() %>" data-epiforms-element-name="<%: formElement.ElementName %>">
    <label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%: labelText %><span class="text-danger"><%:requiredAsterik %></span></label>
        <input name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" type="text" class="FormTextbox__Input form-control"
        placeholder="<%: Model.PlaceHolder %>" value="<%: Model.GetDefaultValue() %>" <%: Html.Raw(Model.AttributesString) %> />
    
    <span data-epiforms-linked-name="<%: formElement.ElementName %>" class="Form__Element__ValidationError help-block" style="display: none;">*</span>
    <span class="trmi form-control-feedback" aria-hidden="true"></span>
    <%= Model.RenderDataList() %>
</div>
