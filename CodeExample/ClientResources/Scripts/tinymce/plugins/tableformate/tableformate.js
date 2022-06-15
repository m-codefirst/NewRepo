tinymce.PluginManager.add('tableformate', function (editor, url) {

    var formateTable = function () {
        var tables = editor.$('table');

        if (tables.length >= 1) {
            tables.each(function (t) {
                var tableContent = $(this);
                var thead = tableContent.find("thead");
                var firstTr = tableContent.find("tr:first");
                var hasTh = tableContent.find("tr:has(th)");

                if (hasTh.length === 0) {
                    var thRow = $('td', firstTr).map(
                        function (i, e) {
                            return $("<th>").html(e.textContent).get(0);
                        }
                    );
                    var rowWithTh = $('<tr></tr>').append(thRow);

                    if (thead.length === 0) {
                        thead = $("<thead></thead>").prependTo(tableContent);
                    }

                    rowWithTh.clone(true).appendTo(thead);
                    firstTr.remove();

                    var tbody = tableContent.find("tbody");
                    $('td:first-child', tbody).each(function () {
                        $(this).replaceWith('<th>' + $(this).text() + '</th>');
                    });
                }
            });

            editor.$('table').each(function () {
                removeAttributes(this);
                $('thead', this).each(function () { removeAttributes(this); });
                $('tbody', this).each(function () { removeAttributes(this); });
                $('tr', this).each(function () { removeAttributes(this); });
                $('th', this).each(function () { removeAttributes(this); });
                $('td', this).each(function () { removeAttributes(this); });
            });

            var hasDivWithClass = editor.$('div').hasClass("vf-table-inform");
            if (hasDivWithClass === undefined || hasDivWithClass === false) {
                editor.$()[0].innerHTML = "<div class='vf-table-inform'><div class='vf-table-inform__scroll-container'>" + editor.$()[0].innerHTML + "</div></div>";
            }
        }
    };

    function removeAttributes(el) {

        if (el === undefined) return;

        var attributes = el.attributes;
        var i = attributes.length;
        while (i--) {
            el.removeAttributeNode(attributes[i]);
        }
    }
    editor.addCommand('tableformate', function () {
        formateTable();
    });

    editor.addButton('tableformate', {
        cmd: 'tableformate',
        text: 'Formate Table',
        valid_children: "+body[table]"
    });
});