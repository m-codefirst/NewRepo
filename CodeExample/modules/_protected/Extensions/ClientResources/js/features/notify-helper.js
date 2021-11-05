function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } }

function _createClass(Constructor, protoProps, staticProps) { if (protoProps) _defineProperties(Constructor.prototype, protoProps); if (staticProps) _defineProperties(Constructor, staticProps); return Constructor; }

var NotifyHelper = /*#__PURE__*/function () {
    function NotifyHelper() {
        _classCallCheck(this, NotifyHelper);
    }

    _createClass(NotifyHelper, [{
        key: "Success",
        value: function Success(message, encodeMess) {
            var options = this.setOptions("success", encodeMess);
            $.notify(message, options);
        }
    }, {
        key: "Error",
        value: function Error(message, encodeMess) {
            var options = this.setOptions("error", encodeMess);
            $.notify(message, options);
        }
    }, {
        key: "Warning",
        value: function Warning(message, encodeMess) {
            var options = this.setOptions("warning", encodeMess);
            $.notify(message, options);
        }
    }, {
        key: "Info",
        value: function Info(message, encodeMess) {
            var options = this.setOptions("info", encodeMess);
            $.notify(message, options);
        }
    }, {
        key: "setOptions",
        value: function setOptions(className, encodeMess) {
            var options = {
                className: className
            };

            if (encodeMess != undefined) {
                options.encodeMess = encodeMess;
            } else {
                options.encodeMess = true;
            }

            return options;
        }
    }]);

    return NotifyHelper;
}();