define([
    "dojo",
    "dojo/_base/declare",
    "epi/_Module",
    "epi/dependency",
    "epi/routes"
], function (
    dojo,
    declare,
    _Module,
    dependency,
    routes
) {
    return declare("app.ModuleInitializer", [_Module], {

        initialize: function () {

            this.inherited(arguments);
            var registry = this.resolveDependency("epi.storeregistry");

            //Register the store
            registry.create("supporteddisplayoptions", this._getRestPath("supporteddisplayoptions"));
        },

        _getRestPath: function (name) {
            return routes.getRestPath({ moduleArea: "app", storeName: name });
        }
    });
});