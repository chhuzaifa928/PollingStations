(function (window, $) {
    "use strict";
    var ET = window.ElectionTransport;
    ET.PublicRequest = ET.PublicRequest || {};

    ET.PublicRequest.init = function (options) {
        var initialLat = Number($("#Latitude").val()) || options.defaultLatitude || 30.4;
        var initialLon = Number($("#Longitude").val()) || options.defaultLongitude || 69.3;
        var map = L.map("et-public-map").setView([initialLat, initialLon], initialLat === 30.4 ? 6 : 15);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", { maxZoom: 19, attribution: "&copy; OpenStreetMap contributors" }).addTo(map);
        var pickupMarker = null;

        function setPoint(lat, lon, zoom) {
            $("#Latitude").val(Number(lat).toFixed(7));
            $("#Longitude").val(Number(lon).toFixed(7));
            if (pickupMarker) pickupMarker.setLatLng([lat,lon]);
            else pickupMarker = L.marker([lat,lon], { draggable: true }).addTo(map).bindTooltip("Pickup location", { permanent: false });
            pickupMarker.on("dragend", function () { var p = pickupMarker.getLatLng(); $("#Latitude").val(p.lat.toFixed(7)); $("#Longitude").val(p.lng.toFixed(7)); });
            map.setView([lat,lon], zoom || 16);
        }

        if (Number($("#Latitude").val()) && Number($("#Longitude").val())) setPoint(initialLat, initialLon, 15);
        map.on("click", function (e) { setPoint(e.latlng.lat, e.latlng.lng, map.getZoom()); });
        $("#et-use-location").on("click", function () {
            if (!navigator.geolocation) { ET.toast("Browser location is unavailable.", "error"); return; }
            navigator.geolocation.getCurrentPosition(function (p) { setPoint(p.coords.latitude, p.coords.longitude, 16); ET.toast("Pickup location captured.", "success"); }, function () { ET.toast("Location permission was not granted.", "error"); }, { enableHighAccuracy: true, timeout: 12000 });
        });
        $("#et-request-form").on("submit", function (e) {
            if (!Number($("#Latitude").val()) || !Number($("#Longitude").val())) { e.preventDefault(); ET.toast("Please mark the pickup location on the map or use current location.", "error"); }
        });
        setTimeout(function () { map.invalidateSize(); }, 100);
    };
})(window, window.jQuery);
