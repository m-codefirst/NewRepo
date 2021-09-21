<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>

<%@ Control Language="C#" Inherits="ViewUserControl<UrlElementBlock>" %>

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

<% 
    %>
    <div class="FormTextbox FormTextbox--Url form-group has-feedback"<%:Model.GetValidationCssClasses()%> data-epiforms-element-name="<%: formElement.ElementName %>">
   <label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%: labelText %><span class="text-danger"><%:requiredAsterik %></span></label>
    <input name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" type="url" class="FormTextbox__Input FormUrl__Input form-control"
           placeholder="<%: Model.PlaceHolder %>" value="<%: Model.GetDefaultValue() %>" <%: Html.Raw(Model.AttributesString) %>  data-f-datainput/>
        <span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
       <span class="trmi form-control-feedback" aria-hidden="true"></span>
    
</div>
