"use strict";
var tinymce = tinymce || {};
//Register the plugin
var iconClasses = [['Afbeelding downloaden', 'vf-icon-VF_download_rgb'], ['Contact', 'vf-icon-VF_call_rgb'], ['Contract', 'vf-icon-VF_contract_details_rgb'], ['Kalender', 'vf-icon-VF_calendar_rgb'], ['PDF downloaden', 'vf-icon-VF_download_pdf_rgb'], ['Rekeningen', 'vf-icon-VF_invoices_rgb'], ['Vraagteken', 'vf-icon-VF_help_rgb'], ['Zoeken', 'vf-icon-VF_search_rgb']];
tinymce.PluginManager.add('iconlist', function (ed, url) {// ed as editor

    var items = [];
    tinymce.each(iconClasses, function (iconClassName) {
        items.push({
            text: iconClassName[0],
            icon: ' ' + iconClassName[1],
            classes: ' ' + iconClassName[1],
            onclick: function () {
                var content = tinyMCE.activeEditor.selection.getNode().innerHTML;
                if (tinyMCE.activeEditor.selection.getNode().className.includes(iconClassName[1]) && !isEmptyOrSpaces(content)) {
                    var selectedElement = tinyMCE.activeEditor.selection.getNode();
                    selectedElement.parentElement.innerHTML = content;
                } else {
                    if (content !== undefined && !isEmptyOrSpaces(content) && !isContainLi(content) &&
                        (tinyMCE.activeEditor.selection.getNode().nodeName !== "I" &&
                            ed.selection.getNode().firstChild.nodeName !== "I")) {
                        identifyUlAndImplementClass(ed.selection.getNode());
                        ed.selection.getNode().innerHTML = "<i class='" + iconClassName[1] + " vf-icon-small'></i>" + content;
                    }
                }
            }
        });
    });

    ed.addButton('iconlist', {
        type: 'menubutton',
        text: 'Icon List',
        icon: 'code',
        valid_children: "+body[i]",
        menu: items
    });

    function isEmptyOrSpaces(str) {
        return str === null || str.match(/^ *$/) !== null;
    }

    function isContainLi(str) {
        return str.includes("<li>");
    }

    function identifyUlAndImplementClass(currentNode) {
        if (currentNode.nodeName === "LI") {
            currentNode.parentNode.className = "vf-list vf-list--icon";
        }
    }
});