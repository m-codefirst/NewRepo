<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Forms" %>
<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Import Namespace="TRM.Web.Constants" %>
<%@ Control Language="C#" Inherits="ViewUserControl<CaptchaElementBlock>" %>

<%  var formElement = Model.FormElement;
    var labelText = Model.Label; %>

<% using (Html.BeginElement(Model, new { @class = "FormCaptcha", @data_f_type = "captcha" }))
    { %>
<label class="Form__Element__Caption" for="<%: formElement.Guid %>">
    <%:@Html.TranslateFallback(StringResources.FormAreYouARobot, "Are you a robot?") %><span class="text-danger"><%:@Html.TranslateFallback(StringResources.FormRequiredAsterisk, "*") %></span>
</label>
<img src="<%: Model.CaptchaImageHandler %>" alt="<%: Html.Translate("/contenttypes/captchaelementblock/captchaimagealt") %>" class="Form__Element__Caption FormCaptcha__Image img-responsive" />
<input id="<%: formElement.Guid %>" name="<%: formElement.ElementName %>" type="text" class=" form-control form-group FormTextbox__Input FormCaptcha__Input FormHideInSummarized " data-f-datainput />
<button name="submit" type="submit" data-f-captcha-image-handler="<%: formElement.Guid %>" value="<%: SubmitButtonType.RefreshCaptcha.ToString() %>"
    class="FormExcludeDataRebind FormCaptcha__Refresh" data-f-captcha-refresh="true">
    <%: Html.Translate("/episerver/forms/viewmode/refreshcaptcha")%>
</button>
<span class="trmi form-control-feedback" aria-hidden="true"></span>
<span data-f-linked-name="<%: formElement.ElementName %>" data-f-validationerror class="Form__Element__ValidationError help-block" style="display: none;">*</span>
<% } %>
