define([
        "dojo/_base/declare",
        "dojo/_base/lang",
        "dijit/_Widget",
        "dijit/_TemplatedMixin",
        "dijit/_WidgetsInTemplateMixin",
        "epi/epi",
        "epi/shell/widget/_ValueRequiredMixin",
        "dojo/text!./Templates/TextAreaWithStatistics.html",
        "xstyle/css!./Templates/TextAreaWithStatistics.css"
],
    function (
        declare,
        lang,
        _Widget,
        _TemplatedMixin,
        _WidgetsInTemplateMixin,
        epi,
        _ValueRequiredMixin,
        template
    ) {

        return declare("Editors.TextAreaWithStatistics", [_Widget, _TemplatedMixin, _WidgetsInTemplateMixin, _ValueRequiredMixin], {
            templateString: template,

            intermediateChanges: false,

            value: null,

            postCreate: function () {
                this.inherited(arguments);

                this.textArea.set("intermediateChanges", this.intermediateChanges);
                this.connect(this.textArea, "onChange", this._onInputChange);
                this.connect(this.textArea, "onKeyDown", this._refreshStatistics);
                this.connect(this.textArea, "onKeyUp", this._refreshStatistics);
            },

            _setValueAttr: function (value) {
                this._setValue(value, true);
            },

            _setReadOnlyAttr: function (value) {
                this._set("readOnly", value);
                this.textArea.set("readOnly", value);
            },

            _setIntermediateChangesAttr: function (value) {
                this.textArea.set("intermediateChanges", value);
                this._set("intermediateChanges", value);
            },

            _onInputChange: function (value) {
                this._setValue(value);
            },

            _setValue: function (value, updateTextarea) {
                if (this._started && epi.areEqual(this.value, value)) {
                    return;
                }
                this._set("value", value);

                if (updateTextarea) {
                    this.textArea.set("value", value);
                }
                this._refreshStatistics();

                if (this._started && this.validate()) {
                    this.onChange(value);
                }
            },

            _refreshStatistics: function () {
                var value = this.textArea.get("value");
                this.totalCharacters.innerHTML = value.length;
                if (value.trim().length > 0) {
                    this.totalWords.innerHTML = value.trim().replace(/\s+/gi, ' ').split(' ').length;
                } else {
                    this.totalWords.innerHTML = "0";
                }
                this.totalLines.innerHTML = value.split(/\r*\n/).length;
            }
        });
    });