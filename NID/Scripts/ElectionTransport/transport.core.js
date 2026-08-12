(function (window, $) {
    "use strict";

    if (!$) {
        throw new Error("Election Transport requires jQuery. Load jQuery before transport.core.js.");
    }

    var ET = window.ElectionTransport = window.ElectionTransport || {};

    ET.emoji = function (iconKey) {
        switch ((iconKey || "").toLowerCase()) {
            case "rickshaw": return "🛺";
            case "jeep": return "🚙";
            case "van": return "🚐";
            case "coaster": return "🚌";
            case "bus": return "🚌";
            default: return "🚗";
        }
    };

    ET.statusColor = function (status) {
        switch ((status || "").toLowerCase()) {
            case "moving": return "#079455";
            case "ontrip": return "#079455";
            case "approaching": return "#155eef";
            case "atpollingstation": return "#6941c6";
            case "atpickup": return "#0ba5b7";
            case "idle": return "#dc6803";
            case "offline": return "#d92d20";
            case "neveractivated": return "#667085";
            default: return "#98a2b3";
        }
    };

    ET.statusLabel = function (status) {
        var value = status || "Unknown";
        return value.replace(/([a-z])([A-Z])/g, "$1 $2");
    };

    ET.statusBadge = function (status) {
        var normalized = (status || "").toLowerCase();
        var css = "gray";
        if (normalized === "moving" || normalized === "ontrip") css = "green";
        else if (normalized === "approaching") css = "blue";
        else if (normalized === "atpollingstation") css = "purple";
        else if (normalized === "idle") css = "amber";
        else if (normalized === "offline" || normalized === "neveractivated") css = "red";
        return '<span class="et-badge ' + css + '">' + ET.html(ET.statusLabel(status)) + '</span>';
    };

    ET.requestBadge = function (status) {
        var normalized = (status || "").toUpperCase();
        var css = "gray";
        if (normalized === "COMPLETED" || normalized === "DROPPED_OFF") css = "green";
        else if (normalized === "ASSIGNED" || normalized === "DRIVER_EN_ROUTE" || normalized === "PICKED_UP") css = "blue";
        else if (normalized === "NEW" || normalized === "ROUTING" || normalized === "OFFERED") css = "amber";
        else if (normalized === "NO_VEHICLE" || normalized === "CANCELLED") css = "red";
        return '<span class="et-badge ' + css + '">' + ET.html((status || "Unknown").replace(/_/g, " ")) + '</span>';
    };

    ET.serviceBadge = function (status) {
        var s = (status || "").toLowerCase();
        var css = s === "operational" ? "green" : s === "low service" ? "amber" : "red";
        return '<span class="et-badge ' + css + '">' + ET.html(status || "Unknown") + '</span>';
    };

    ET.html = function (value) {
        return $("<div/>").text(value === null || value === undefined ? "" : value).html();
    };

    ET.attr = function (value) {
        return ET.html(value).replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    };

    ET.number = function (value, decimals) {
        var n = Number(value);
        if (!isFinite(n)) n = 0;
        return n.toLocaleString("en-PK", {
            minimumFractionDigits: decimals || 0,
            maximumFractionDigits: decimals || 0
        });
    };

    ET.km = function (value) {
        return ET.number(value, 1) + " km";
    };

    ET.percent = function (value) {
        return ET.number(value, 1) + "%";
    };

    ET.parseDate = function (value) {
        if (!value) return null;
        if (value instanceof Date) return value;
        if (typeof value === "string" && value.indexOf("/Date(") === 0) {
            return new Date(parseInt(value.replace(/[^0-9-]/g, ""), 10));
        }
        var result = new Date(value);
        return isNaN(result.getTime()) ? null : result;
    };

    ET.dateTime = function (value) {
        var date = ET.parseDate(value);
        if (!date) return "—";
        return date.toLocaleString("en-PK", {
            day: "2-digit", month: "short", year: "numeric",
            hour: "2-digit", minute: "2-digit", second: "2-digit"
        });
    };

    ET.time = function (value) {
        var date = ET.parseDate(value);
        if (!date) return "—";
        return date.toLocaleTimeString("en-PK", { hour: "2-digit", minute: "2-digit" });
    };

    ET.token = function () {
        return $('input[name="__RequestVerificationToken"]').first().val();
    };

    ET.get = function (url, data) {
        return $.ajax({ url: url, type: "GET", data: data || {}, cache: false });
    };

    ET.post = function (url, data) {
        var payload = $.extend({}, data || {}, { __RequestVerificationToken: ET.token() });
        return $.ajax({ url: url, type: "POST", data: payload });
    };

    ET.toast = function (message, type) {
        var $container = $("#et-toast-container");
        if (!$container.length) {
            $container = $('<div id="et-toast-container" class="et-toast-container"></div>').appendTo(document.body);
        }
        var $toast = $('<div class="et-toast ' + (type || "info") + '"></div>').text(message || "Completed.");
        $container.append($toast);
        window.setTimeout(function () { $toast.fadeOut(250, function () { $toast.remove(); }); }, 3200);
    };

    ET.loading = function ($element, active) {
        if (!$element || !$element.length) return;
        $element.toggleClass("et-loading", active === true);
    };

    ET.openDrawer = function (html, title) {
        var $drawer = $("#et-drawer");
        var $overlay = $("#et-overlay");
        if (!$drawer.length) {
            $overlay = $('<div id="et-overlay" class="et-overlay"></div>').appendTo(document.body);
            $drawer = $(
                '<aside id="et-drawer" class="et-drawer">' +
                '<div class="et-drawer-head"><strong id="et-drawer-title"></strong><button class="et-drawer-close" type="button">×</button></div>' +
                '<div id="et-drawer-body" class="et-drawer-body"></div></aside>'
            ).appendTo(document.body);
            $overlay.on("click", ET.closeDrawer);
            $drawer.on("click", ".et-drawer-close", ET.closeDrawer);
        }
        $("#et-drawer-title").text(title || "Details");
        $("#et-drawer-body").html(html || "");
        $drawer.addClass("open");
        $overlay.addClass("open");
    };

    ET.closeDrawer = function () {
        $("#et-drawer").removeClass("open");
        $("#et-overlay").removeClass("open");
    };

    ET.vehicleIcon = function (vehicle) {
        if (!window.L) return null;
        var markerColor = vehicle.MarkerColorHex || "#0f725c";
        var statusColor = ET.statusColor(vehicle.EffectiveStatus);
        var className = vehicle.EffectiveIsOnline ? "online" : "";
        return L.divIcon({
            className: "et-vehicle-marker",
            iconSize: [42, 42],
            iconAnchor: [21, 21],
            popupAnchor: [0, -19],
            html: '<div class="et-vehicle-marker-inner ' + className + '" style="--marker-color:' + ET.attr(markerColor) + ';--status-color:' + statusColor + '">' + ET.emoji(vehicle.IconKey) + '</div>'
        });
    };

    ET.stationIcon = function (station) {
        if (!window.L) return null;
        return L.divIcon({
            className: "",
            iconSize: [30, 30],
            iconAnchor: [15, 27],
            popupAnchor: [0, -25],
            html: '<div class="et-station-marker"><span>' + ET.html(station.Sr) + '</span></div>'
        });
    };

    ET.vehiclePopup = function (vehicle, detailsUrl) {
        return [
            '<div class="et-map-popup">',
            '<h4>', ET.emoji(vehicle.IconKey), ' ', ET.html(vehicle.RegistrationNo), '</h4>',
            '<div class="et-popup-meta">', ET.html(vehicle.VehicleType || "Vehicle"), ' · ', ET.html(vehicle.DriverName || "Driver not set"), '</div>',
            '<div class="et-popup-grid">',
            '<div><span>Status</span><strong>', ET.html(ET.statusLabel(vehicle.EffectiveStatus)), '</strong></div>',
            '<div><span>Trips today</span><strong>', ET.number(vehicle.TodayTrips), '</strong></div>',
            '<div><span>Distance</span><strong>', ET.km(vehicle.TodayDistanceKm), '</strong></div>',
            '<div><span>Speed</span><strong>', ET.number(vehicle.SpeedKph, 0), ' km/h</strong></div>',
            '</div>',
            '<div class="et-popup-meta" style="margin-top:9px">PS: ', ET.html(vehicle.PollingStationName || "Unassigned"), '<br>Provider: ', ET.html(vehicle.ProviderName || "Not linked"), '</div>',
            '<div class="et-popup-actions"><a class="et-btn small primary" href="', ET.attr(detailsUrl), '">Vehicle details</a></div>',
            '</div>'
        ].join("");
    };

    ET.stationPopup = function (station, detailsUrl) {
        return [
            '<div class="et-map-popup">',
            '<h4>Polling Station Sr ', ET.html(station.Sr), '</h4>',
            '<div class="et-popup-meta">', ET.html(station.PollingStationName || "Polling station"), '</div>',
            '<div class="et-popup-grid">',
            '<div><span>Online vehicles</span><strong>', ET.number(station.OnlineVehicles), '</strong></div>',
            '<div><span>Valid trips</span><strong>', ET.number(station.ValidTrips), '</strong></div>',
            '<div><span>Open requests</span><strong>', ET.number(station.OpenTransportRequests), '</strong></div>',
            '<div><span>Voters</span><strong>', ET.number(station.TotalVoters), '</strong></div>',
            '</div>',
            '<div class="et-popup-actions"><a class="et-btn small navy" href="', ET.attr(detailsUrl), '">Station details</a></div>',
            '</div>'
        ].join("");
    };

    ET.replaceId = function (template, id) {
        return (template || "").replace("__id__", encodeURIComponent(id));
    };

    ET.initContextSelector = function () {
        $(document).on("change", "#et-context-selector", function () {
            var url = $(this).data("url") || "";
            window.location.href = url.replace("__context__", encodeURIComponent($(this).val()));
        });
    };

    ET.initContextSelector();
})(window, window.jQuery);
