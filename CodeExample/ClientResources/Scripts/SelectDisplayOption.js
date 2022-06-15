define("epi-cms/contentediting/command/SelectDisplayOption", [
    // General application modules
    "dojo/_base/declare",
    "dojo/_base/lang",
    "dojo/when",

    "epi/dependency",

    "epi-cms/contentediting/command/_ContentAreaCommand",
    "epi-cms/contentediting/viewmodel/ContentBlockViewModel",

    "epi-cms/widget/DisplayOptionSelector",

    // Resources
    "epi/i18n!epi/cms/nls/episerver.cms.contentediting.editors.contentarea.displayoptions"
], function (declare, lang, when, dependency, _ContentAreaCommand, ContentBlockViewModel, DisplayOptionSelector, resources) {

    return declare([_ContentAreaCommand], {
        // tags:
        //      internal

        // label: [public] String
        //      The action text of the command to be used in visual elements.
        label: resources.label,

        // category: [readonly] String
        //      A category which hints that this item should be displayed as an popup menu.
        category: "popup",

        _labelAutomatic: lang.replace(resources.label, [resources.automatic]),

        constructor: function () {
            this.popup = new DisplayOptionSelector();
        },

        postscript: function () {
            this.inherited(arguments);

            if (!this.store) {
                var registry = dependency.resolve("epi.storeregistry");
                this.store = registry.get("epi.cms.displayoptions");
            }

            when(this.store.get(), lang.hitch(this, function (options) {
                // Nuon: Save allOptions in a separate property for later use
                this.set("allOptions", options);

                // Reset command's available property in order to reset dom's display property of the given node
                this._setCommandAvailable(options);

                this.popup.set("displayOptions", options);
            }));
        },

        _onModelChange: function () {
            // summary:
            //      Updates canExecute after the model value has changed.
            // tags:
            //      protected

            this.inherited(arguments);

            // Nuon: Begin modified to filter on supported display options and adjust the popup
            if (!this.storeSupported) {
                var registry = dependency.resolve("epi.storeregistry");
                this.storeSupported = registry.get("supporteddisplayoptions");
            }

            // Load the supported display options from our custom store
            when(this.storeSupported.get(this.model.contentLink), lang.hitch(this, function (data) {

                // Get supported display options and replace the displayOptions in the popup
                var supportedOptions = [];
                var i = this.allOptions.length;
                while (i--) {
                    var j = data.displayOptions.length;
                    while (j--) {
                        if (this.allOptions[i].id === data.displayOptions[j]) {
                            supportedOptions.push(this.allOptions[i]);
                            break;
                        }
                    }
                }
                // Reverse the array because we were looping backwards
                supportedOptions = supportedOptions.reverse();

                // Set the isAvailable property
                this._setCommandAvailable(supportedOptions);

                // Set the displayOptions property
                this.popup.set("displayOptions", supportedOptions);

                // Set default labe and return, if display options are not available for this model
                if (!this.isAvailable) {
                    this.set("label", this._labelAutomatic);
                    return;
                }

                // Update model
                this.popup.set("model", this.model);
            }));
            // Nuon: End modified to filter on supported display options and adjust the popup

            var selectedOption = this.model.get("displayOption");
            if (!selectedOption) {
                this.set("label", this._labelAutomatic);
            } else {
                this._setLabel(selectedOption);
            }

            this._watch("displayOption", function (property, oldValue, newValue) {
                if (!newValue) {
                    this.set("label", this._labelAutomatic);
                } else {
                    this._setLabel(newValue);
                }
            }, this);
        },

        _setCommandAvailable: function (/*Array*/displayOptions) {
            // summary:
            //      Set command available
            // displayOptions: [Array]
            //      Collection of a content display mode
            // tags:
            //      private

            this.set("isAvailable", displayOptions && displayOptions.length > 0 && this.model instanceof ContentBlockViewModel);
        },

        _setLabel: function (displayOption) {
            when(this.store.get(displayOption), lang.hitch(this, function (option) {
                this.set("label", lang.replace(resources.label, [option.name]));

            }), lang.hitch(this, function (error) {

                console.log("Could not get the option for: ", displayOption, error);

                this.set("label", this._labelAutomatic);
            }));
        },

        _onModelValueChange: function () {
            this.set("canExecute", !!this.model && this.model.contentLink && !this.model.get("readOnly"));
        }
    });
});