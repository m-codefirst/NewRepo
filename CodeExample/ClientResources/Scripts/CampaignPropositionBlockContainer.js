define([
    "dojo/_base/declare",
    "dojo/_base/lang",

    "epi/shell/layout/SimpleContainer"
],
function (
    declare,
    lang,

    SimpleContainer
) {

    return declare([SimpleContainer], {
        campaignDropdown: null,
        campaignPropositionDropdown: null,

        addChild: function (child) {
            //Summary: Add a widget to the container

            var self = this;
            self.inherited(arguments);

            if (child.name.indexOf("campaignProposition.campaignName") >= 0) {
                // If it's the campaign drop down list
                self.campaignDropdown = child;

                // Connect to change event to update the proposition drop down list
                self.own(self.campaignDropdown.on("change", lang.hitch(self, self._updateCampaignPropositionDropdown)));
            } else if (child.name.indexOf("campaignProposition.campaignProposition") >= 0) {
                // If it's the proposition drop down list
                self.campaignPropositionDropdown = child;

                // Update the proposition drop down in case campaign has no initial value
                // some slack
                setTimeout(function ()
                {
                    self._updateCampaignPropositionDropdown(self.campaignDropdown.value);
                }, 2000);
            }
        },

        _updateCampaignPropositionDropdown: function (campaign) {
            // Summary: Update the proposition drop down list according to the selected campaign

            //if (campaign !== "" && this.previousCampaign === "") {
            //    this.previousCampaign = campaign;
            //}
            if (this.previousCampaign === undefined || campaign !== this.previousCampaign) {
                this.previousCampaign = campaign;
                var campaignProposition = this.campaignPropositionDropdown.value;
                if (campaignProposition != null && campaignProposition !== "" && campaignProposition.indexOf("[" + campaign + "]_") !== 0)
                    this.campaignPropositionDropdown.set("value", null);
                //set the filter
                this.campaignPropositionDropdown.set("filter", function (proposition) {
                    return proposition.value.indexOf("[" + campaign + "]_") === 0;
                });
            }
        }
    });
});