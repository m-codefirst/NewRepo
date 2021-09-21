<%@ import namespace="System.Web.Mvc" %>
<%@ import namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ import namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ control language="C#" inherits="ViewUserControl<ChoiceElementBlock>" %>

<%
    var formElement = Model.FormElement;
    var labelText = Model.Label;
    var items = Model.GetItems().ToList();
    string requiredAsterik;
    string radioOrCheckbox;

    if (Model.AllowMultiSelect)
    {
        radioOrCheckbox = "checkbox small fancy";
    }
    else
    {
        radioOrCheckbox = "radio small fancy";
    }
  
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
<label class="Form__Element__Caption"><%: labelText %> <span class="text-danger"><%:requiredAsterik %></span></></label>
<% using(Html.BeginElement(Model, new { id=formElement.Guid, @class="FormChoice " + radioOrCheckbox + Model.GetValidationCssClasses(), data_f_type="choice" }, true)) { %>
    <%  if(!string.IsNullOrEmpty(labelText)) { %>
    
    <% } 
        for(var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var defaultCheckedString = Model.GetDefaultSelectedString(item);
            var checkedString = string.IsNullOrEmpty(defaultCheckedString) ? string.Empty : "checked";
            var elementId = formElement.ElementName + "_" + Guid.NewGuid() + "_" + item.Value + "_" + i;
    %>

    <label>
        <% if(Model.AllowMultiSelect) { %>
        <input type="checkbox" name="<%: formElement.ElementName %>" id="<%: elementId %>" value="<%: item.Value %>" class="FormChoice__Input FormChoice__Input--Checkbox form-control" <%: checkedString %> <%: defaultCheckedString %> data-original-title title />
         <label class="" for="<%: elementId %>"><%: item.Caption %></label>
        <% } else  { %>
        <input type="radio" name="<%: formElement.ElementName %>" id="<%: elementId %>" value="<%: item.Value %>" class="FormChoice__Input FormChoice__Input--Radio form-control" <%: checkedString %> <%: defaultCheckedString %> data-original-title title />
        <label class="" for="<%: elementId %>"><%: item.Caption %></label>
        <% } %>
    </label>
    
    <% } %>
<span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
<span class="trmi form-control-feedback" aria-hidden="true"></span>
 
<% } %>
 