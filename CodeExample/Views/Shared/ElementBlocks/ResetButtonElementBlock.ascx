<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Control Language="C#" Inherits="ViewUserControl<ResetButtonElementBlock>" %>

<%  var formElement = Model.FormElement;
    var buttonText = Model.Label; %>
<input
    <%--name="<%: formElement.ElementName %>" id="<%: formElement.Guid %>" --%>
    type="reset" onclick="confirm_reset()" form="<%: Model.FormElement.Form.FormGuid %>"
    class="Form__Element FormResetButton Form__Element--NonData btn btn-default" data-f-element-nondata="<%: Html.Raw(Model.AttributesString) %>"
    value="<%: buttonText %>" data-f-type="resetbutton">

<script type="text/javascript">
    function confirm_reset() {
        $('.ValidationFail').removeClass('ValidationFail');
        $('.ValidationSuccess').removeClass('ValidationSuccess');
    }

</script>
