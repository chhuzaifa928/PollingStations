(function (window, $) {
    "use strict";
    var ET = window.ElectionTransport;
    ET.Details = ET.Details || {};

    ET.Details.vehicle = function (options) {
        var map = L.map("et-vehicle-detail-map").setView([30.4, 69.3], 6);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", { maxZoom: 19, attribution: "&copy; OpenStreetMap contributors" }).addTo(map);
        ET.get(options.trailUrl, { id: options.assignmentId, minutes: options.trailMinutes }).done(function (r) {
            var rows = r && r.success ? r.data || [] : [];
            if (!rows.length) return;
            var points = rows.map(function (x) { return [x.Latitude, x.Longitude]; });
            L.polyline(points, { color: "#155eef", weight: 4, opacity: .76 }).addTo(map);
            L.circleMarker(points[0], { radius: 6, color: "#079455", fillOpacity: 1 }).bindTooltip("Start").addTo(map);
            L.circleMarker(points[points.length - 1], { radius: 7, color: "#d92d20", fillOpacity: 1 }).bindTooltip("Latest").addTo(map);
            map.fitBounds(points, { padding: [25,25], maxZoom: 16 });
        });
    };

    ET.Details.station = function (options) {
        var map = L.map("et-station-detail-map").setView([options.latitude, options.longitude], 15);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", { maxZoom: 19, attribution: "&copy; OpenStreetMap contributors" }).addTo(map);
        L.circle([options.latitude, options.longitude], { radius: options.geofenceRadius, color: "#0f725c", fillColor: "#0f725c", fillOpacity: .08 }).addTo(map);
        L.marker([options.latitude, options.longitude], { icon: ET.stationIcon({ Sr: options.sr }) }).addTo(map);
        (options.vehicles || []).forEach(function (v) { if (v.Latitude != null && v.Longitude != null) L.marker([v.Latitude,v.Longitude], { icon: ET.vehicleIcon(v) }).bindPopup(ET.vehiclePopup(v, ET.replaceId(options.vehicleDetailsUrl, v.VehicleAssignmentId))).addTo(map); });
    };
})(window, window.jQuery);
