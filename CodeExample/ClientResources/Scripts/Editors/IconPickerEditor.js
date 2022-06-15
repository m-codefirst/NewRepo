define([
    "dojo/query",
    "dojo/on",
    "dojo/_base/declare",
    "dojo/_base/lang",

    "dijit/_CssStateMixin",
    "dijit/_Widget",
    "dijit/_TemplatedMixin",
    "dijit/_WidgetsInTemplateMixin",

    "epi/epi",
    "epi/shell/widget/_ValueRequiredMixin"
],
    function (
        query,
        on,
        declare,
        lang,

        _CssStateMixin,
        _Widget,
        _TemplatedMixin,
        _WidgetsInTemplateMixin,

        epi,
        _ValueRequiredMixin
    ) {

        return declare("vattenfall.editors.IconPickerEditor", [_Widget, _TemplatedMixin, _WidgetsInTemplateMixin, _CssStateMixin, _ValueRequiredMixin], {
            templateString: "<div class=\"dijitInline\" style=\"width: 50%\">\
                        <div class=\"icon-selector-container\">\
                            <i data-dojo-attach-point=\"selectedIcon\"></i>\
                            <br />\
                            <br />\
                            <br />\
                            <vfc-ce-icon-selector data-dojo-attach-point=\"iconPickerList\"></vfc-ce-icon-selector>\
                        </div>\
                </div>",
            intermediateChanges: false,
            value: null,
            onChange: function (value) {
                // Event
            },
            postCreate: function () {
                this.inherited(arguments);
                this._bindEvents(this);
            },
            startup: function () {
            },
            isValid: function () {
                return !this.required || lang.isArray(this.value) && this.value.length > 0 && this.value.join() != "";
            },
            _setValueAttr: function (value) {
                this._setValue(value, true);
            },
            _setReadOnlyAttr: function (value) {
                this._set("readOnly", value);
            },
            _setIntermediateChangesAttr: function (value) {

                this._set("intermediateChanges", value);
            },
            _bindEvents: function (myself) {
                on(query(this.iconPickerList), "selectedIcon", function (event) {
                    console.log('vfc-ce-icon-selector::selectedIcon', event.detail);
                    myself._chooseIconValue(event.detail, myself);

                    event.preventDefault();
                });

            },
            _chooseIconValue: function (icon, myself) {
                var iconValue = icon.name;
                myself._setValue(iconValue, true);
            },
            _setValue: function (value, updateTextbox) {
                //avoids running this if the widget already is started
                if (this._started && epi.areEqual(this.value, value)) {
                    return;
                }

                // set value to this widget (and notify observers). 
                this._set("value", "vf-icon-" + value);

                // set value to tmp value
                if (updateTextbox) {
                    this.selectedIcon.className = "vf-icon-medium vf-icon-" + value;
                }

                if (this._started && this.validate()) {
                    // Trigger change event
                    this.onChange(value);
                }
            }
        });
    });