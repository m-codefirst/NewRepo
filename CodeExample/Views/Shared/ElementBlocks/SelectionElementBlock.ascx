<%@ import namespace="System.Web.Mvc" %>
<%@ import namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ import namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ control language="C#" inherits="ViewUserControl<SelectionElementBlock>" %>

<%
    var formElement = Model.FormElement; 
    var labelText = Model.Label;
    var placeholderText = Model.PlaceHolder;
    var defaultOptionItemText = !string.IsNullOrWhiteSpace(placeholderText) ? placeholderText : Html.Translate(string.Format("/episerver/forms/viewmode/selection/{0}", Model.AllowMultiSelect ? "selectoptions" : "selectanoption"));
    var defaultOptionSelected = Model.Items.Count(x => x.Checked.HasValue && x.Checked.Value) <= 0 ? "selected=\"selected\"" : "";
    var items = Model.GetItems();
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
<label for="<%: formElement.Guid %>" class="Form__Element__Caption"><%: labelText %><span class="text-danger"><%:requiredAsterik %></span></label>
<div class="Form__Element FormSelection form-group select-form <%:Model.GetValidationCssClasses() %>" id="dropdownforms"<%  %> data-epiforms-element-name="<%: formElement.ElementName %>">
   <select class="form-control" name="<%: formElement.ElementName %>"  id="<%: formElement.Guid %>" <%: Model.AllowMultiSelect ? "multiple" : "" %>  <%= Model.AttributesString %> >
        <option disabled="disabled" <%= defaultOptionSelected %> value=""><%: defaultOptionItemText %></option>
        <%
        foreach (var item in items)
        {
            var defaultSelectedString = Model.GetDefaultSelectedString(item);
            var selectedString = string.IsNullOrEmpty(defaultSelectedString) ? string.Empty : "selected";
        %>
        <option value="<%: item.Value %>" <%= selectedString %> <%= defaultSelectedString %>><%: item.Caption %></option>
        <% } %>
    </select>
    <span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
    <span class="trmi form-control-feedback" aria-hidden="true"></span>
    <%= Html.ValidationMessageFor(Model) %>
</div>
    