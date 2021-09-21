<%@ import namespace="System.Web.Mvc" %>
<%@ import namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ import namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ control language="C#" inherits="ViewUserControl<FileUploadElementBlock>" %>

<%  var formElement = Model.FormElement; 
    var labelText = Model.Label;
    var allowMultiple = Model.AllowMultiple ? "multiple" : string.Empty;
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
   
<div class="FormFileUpload form-group has-feedback"<% %> data-epiforms-element-name="<%: formElement.ElementName %>">
    <label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%: labelText %><span class="text-danger"><%:requiredAsterik %></span></label>
    <input name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" type="file" class="FormFileUpload__Input form-control" <%:allowMultiple%>
        <% if(!string.IsNullOrEmpty(Model.FileExtensions)) { %>
			accept="<%=Model.FileExtensions%>"
    	<%}%>     
        <%= Model.AttributesString %> />
    <div class="FormFileUpload__PostedFile" data-f-postedFile></div>
    <span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
    <span class="trmi form-control-feedback" aria-hidden="true"></span>
    
</div>
    

