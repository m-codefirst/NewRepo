<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="EPiServer.Web.Mvc.Html" %>
<%@ Import Namespace="EPiServer.Forms" %>
<%@ Import Namespace="EPiServer.Forms.Core" %>
<%@ Import Namespace="EPiServer.Forms.Helpers.Internal" %>
<%@ Import Namespace="EPiServer.Forms.EditView.Internal" %>
<%@ Import Namespace="EPiServer.Forms.Implementation.Elements" %>
<%@ Import Namespace="TRM.Web.Extentions" %>
<%@ Control Language="C#" Inherits="ViewUserControl<FormContainerBlock>" %>

<%  
    var _formConfig = EPiServer.ServiceLocation.ServiceLocator.Current.GetInstance<EPiServer.Forms.Configuration.IEPiServerFormsImplementationConfig>();
    var isNewDesign = Convert.ToBoolean(ViewContext.Controller.ControllerContext.ParentActionViewContext.ViewData["isNewDesign"]);
    var rowJustifyCenter = isNewDesign ? "row justify-content-center" : string.Empty;
    var col10colsm8collg6 = isNewDesign ? "col-10 col-sm-8 col-lg-6" : string.Empty;
%>

<% if (EPiServer.Editor.PageEditing.PageIsInEditMode)
    { %>
<link rel="stylesheet" type="text/css" data-f-resource="EPiServerForms.css" href='<%: ModuleHelper.ToClientResource(typeof(FormsModule), "ClientResources/ViewMode/EPiServerForms.css")%>' />
<% if (Model.Form != null)
    { %>
<div class="EPiServerForms">
    <h2 class="Form__Title"><%: Html.PropertyFor(m => m.Title) %></h2>
    <h4 class="Form__Description"><%: Html.PropertyFor(m => m.Description) %></h4>

    <%: Html.PropertyFor(m => m.ElementsArea) %>
</div>
<% }
    else
    { %>
<%--In case FormContainerBlock is used as a property, we cannot build Form model so we show a warning message to notify user--%>
<div class="EPiServerForms">
    <span class="Form__Warning"><%: Html.Translate("/episerver/forms/editview/cannotbuildformmodel") %></span>
</div>
<% } %>
<% }
    else if (Model.Form != null)
    { %>

<%-- 
    Using form tag (instead of div) for the sake of html elements' built-in features e.g. reset, file upload
    Using enctype="multipart/form-data" for post data and uploading files 
--%>
<%
    var validationCssClass = ViewBag.ValidationFail ? "ValidationFail" : "ValidationSuccess";
%>
<%--Form will post to its own page Controller --%>

<% if (ViewBag.RenderingFormUsingDivElement)
    { %>
<div data-f-metadata="<%: Model.MetadataAttribute %>" class=" col-xs-12 EPiServerForms <%: validationCssClass %>" data-f-type="form" id="<%: Model.Form.FormGuid %>">
    <%}
        else
        {%>
    <form method="post"
        data-f-metadata="<%: Model.MetadataAttribute %>"
        enctype="multipart/form-data" class="EPiServerForms <%: validationCssClass %>" data-f-type="form" id="<%: Model.Form.FormGuid %>">

        <%} %>
        <%= Html.GenerateAntiForgeryToken(Model) %>
        <%--Meta data, authoring data of this form is transfer to clientside here. We need to take form with language coresponse with current page's language --%>
        <script type="text/javascript" src="<%: _formConfig.CoreController %>/GetFormInitScript?formGuid=<%: Model.Form.FormGuid %>&formLanguage=<%: FormsExtensions.GetCurrentPageLanguage() %>"></script>

        <%--Meta data, send along as a SYSTEM information about this form, so this can work without JS --%>
        <input type="hidden" class="Form__Element Form__SystemElement FormHidden FormHideInSummarized" name="__FormGuid" value="<%: Model.Form.FormGuid %>" data-f-type="hidden" />
        <input type="hidden" class="Form__Element Form__SystemElement FormHidden FormHideInSummarized" name="__FormHostedPage" value="<%: FormsExtensions.GetCurrentPageLink().ToString() %>" data-f-type="hidden" />
        <input type="hidden" class="Form__Element Form__SystemElement FormHidden FormHideInSummarized" name="__FormLanguage" value="<%: FormsExtensions.GetCurrentPageLanguage() %>" data-f-type="hidden" />
        <input type="hidden" class="Form__Element Form__SystemElement FormHidden FormHideInSummarized" name="__FormCurrentStepIndex" value="<%: ViewBag.CurrentStepIndex ?? "" %>" data-f-type="hidden" />
        <input type="hidden" class="Form__Element Form__SystemElement FormHidden FormHideInSummarized" name="__FormSubmissionId" value="<%: ViewBag.FormSubmissionId %>" data-f-type="hidden" />

        <h2 class=" col-xs-12 Form__Title"><%: Model.Title %></h2>
        <aside class=" col-xs-12 Form__Description"><%: Model.Description %></aside>
        <div class="<%: rowJustifyCenter %>">
            <div class="<%: col10colsm8collg6 %>">

                <%  var statusDisplay = "hide";
                    var message = ViewBag.Message;

                    if (ViewBag.FormFinalized || ViewBag.IsProgressiveSubmit)
                    {
                        statusDisplay = "Form__Success__Message";
                    }
                    else if (!ViewBag.Submittable && !string.IsNullOrEmpty(message))
                    {
                        statusDisplay = "Form__Warning__Message";
                    }
                %>
                <%
                    if (ViewBag.IsReadOnlyMode)
                    {
                %>
                <div class=" col-xs-12 Form__Status">
                    <span class="Form__Readonly__Message">
                        <%: Html.Translate("/episerver/forms/viewmode/readonlymode")%>
                    </span>
                </div>
                <% 
                    }
                %>

                <%-- area for showing Form's status or validation --%>
                <div class="col-xs-12 Form__Status">
                    <div class="Form__Status__Message <%: statusDisplay %>" data-f-form-statusmessage>
                        <% if (ViewBag.FormFinalized)
                            { %>
                        <%= message %>
                        <% }
                            else
                            { %>
                        <%: message %>
                        <% } %>
                    </div>
                </div>

                <div class="col-xs-12 Form__MainBody">
                    <%  var i = 0;
                        var currentStepIndex = ViewBag.CurrentStepIndex == null ? -1 : (int)ViewBag.CurrentStepIndex;
                        string stepDisplaying;
                        foreach (var step in Model.Form.Steps)
                        {
                            stepDisplaying = (currentStepIndex == i && !ViewBag.FormFinalized && (bool)ViewBag.IsStepValidToDisplay) ? "" : "hide"; %>
                    <section id="<%: step.ElementName %>" data-f-type="step" data-f-element-name="<%: step.ElementName %>" class="Form__Element FormStep Form__Element--NonData <%: stepDisplaying %>" data-f-stepindex="<%: i %>" data-f-element-nondata>
                        <% 
                            var stepBlock = (step.SourceContent as ElementBlockBase);
                            if (stepBlock != null)
                            {
                                Html.RenderContentData(step.SourceContent, false);
                            }
                        %>

                        <!-- Each FormStep groups the elements below it til the next FormStep -->
                        <%
                            //Html.RenderElementsInStep(i, step.Elements);
                            //Html.RenderFormElements(i, step.Elements, Model);
                            Html.RenderFormElementsExt(i, step.Elements, Model);
                        %>
                        <!-- End - Section FormStep -->
                    </section>

                    <% i++;
                        } // end foreach steps %>

                    <% // show Next/Previous buttons when having Steps > 1 and navigationBar when currentStepIndex is valid
                        var currentDisplayStepCount = Model.Form.Steps.Count();
                        if (currentDisplayStepCount > 1 && currentStepIndex > -1 && currentStepIndex < currentDisplayStepCount && !ViewBag.FormFinalized)
                        {
                            string prevButtonDisableState = (currentStepIndex == 0) || !ViewBag.Submittable ? "disabled" : "";
                            string nextButtonDisableState = (currentStepIndex == currentDisplayStepCount - 1) || !ViewBag.Submittable ? "disabled" : "";
                    %>
                    <% if (Model.ShowNavigationBar)
                        { %>
                    <nav role="navigation" class="Form__NavigationBar" data-f-type="navigationbar" data-f-element-nondata>
                        <button type="submit" name="submit" value="<%: SubmitButtonType.PreviousStep.ToString() %>" class="Form__NavigationBar__Action FormExcludeDataRebind btnPrev"
                            <%: prevButtonDisableState %> data-f-navigation-previous>
                            <%: Html.Translate("/episerver/forms/viewmode/stepnavigation/previous")%></button>

                        <%
                            // calculate the progress style on-server-side
                            var currentDisplayStepIndex = currentStepIndex + 1;
                            var progressWidth = (100 * currentDisplayStepIndex / currentDisplayStepCount) + "%";
                        %>
                        <div class="Form__NavigationBar__ProgressBar">
                            <div class="Form__NavigationBar__ProgressBar--Progress" style="width: <%: progressWidth %>" data-f-navigation-progress></div>
                            <div class="Form__NavigationBar__ProgressBar--Text">
                                <span class="Form__NavigationBar__ProgressBar__ProgressLabel"><%: Html.Translate("/episerver/forms/viewmode/stepnavigation/page")%></span>
                                <span class="Form__NavigationBar__ProgressBar__CurrentStep" data-f-navigation-currentstep><%:currentDisplayStepIndex %></span>/
                    <span class="Form__NavigationBar__ProgressBar__StepsCount" data-f-navigation-stepcount><%:currentDisplayStepCount %></span>
                            </div>
                        </div>



                        <button type="submit" name="submit" value="<%: SubmitButtonType.NextStep.ToString() %>" class="Form__NavigationBar__Action FormExcludeDataRebind btnNext"
                            <%: nextButtonDisableState %> data-f-navigation-next>
                            <%: Html.Translate("/episerver/forms/viewmode/stepnavigation/next")%></button>
                    </nav>
                    <% } %>

                    <% } // endof if %>
                </div>
                <%-- endof FormMainBody --%>
            </div>
        </div>

        <% if (ViewBag.RenderingFormUsingDivElement)
            { %>
</div>
<%}
    else
    {  %>
    </form>
<% } %>

<% } %>