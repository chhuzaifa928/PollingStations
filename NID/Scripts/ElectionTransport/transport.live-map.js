(function (window, $) {
    "use strict";
    var ET = window.ElectionTransport;
    ET.LiveMap = ET.LiveMap || {};

    ET.LiveMap.init = function (options) {
        var map = L.map("et-live-map", { preferCanvas: true, zoomControl: true }).setView([30.4, 69.3], 6);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", { maxZoom: 19, attribution: "&copy; OpenStreetMap contributors" }).addTo(map);
        var vehicleLayer = L.layerGroup().addTo(map);
        var stationLayer = L.layerGroup().addTo(map);
        var trailLayer = L.layerGroup().addTo(map);
        var currentVehicles = [];
        var currentStations = [];
        var refreshHandle = null;
        var demoHandle = null;
        var selectedAssignmentId = null;

        function filters() {
            return {
                ElectionContextId: options.contextId,
                Status: $("#et-filter-status").val() || null,
                VehicleTypeCode: $("#et-filter-type").val() || null,
                ProviderId: $("#et-filter-provider").val() || null,
                PollingStationId: $("#et-filter-station").val() || null,
                Search: $.trim($("#et-filter-search").val()) || null
            };
        }

        function load(fit) {
            ET.get(options.mapUrl, filters())
                .done(function (response) {
                    if (!response || !response.success) return;
                    currentVehicles = response.vehicles || [];
                    currentStations = response.pollingStations || [];
                    populateFilters();
                    render(fit === true);
                    renderVehicleList();
                    $("#et-map-updated").text(ET.dateTime(response.serverTimeUtc));
                })
                .fail(function () { ET.toast("Live map could not be refreshed.", "error"); });
        }

        function populateFilters() {
            if (!$("#et-filter-provider option[data-loaded]").length) {
                var providers = {};
                currentVehicles.forEach(function (x) { if (x.ProviderId) providers[x.ProviderId] = x.ProviderName; });
                Object.keys(providers).sort(function (a,b) { return (providers[a] || "").localeCompare(providers[b] || ""); }).forEach(function (id) {
                    $("#et-filter-provider").append('<option data-loaded="1" value="' + ET.attr(id) + '">' + ET.html(providers[id]) + '</option>');
                });
            }
            if (!$("#et-filter-station option[data-loaded]").length) {
                currentStations.forEach(function (x) {
                    $("#et-filter-station").append('<option data-loaded="1" value="' + x.PollingStationId + '">Sr ' + ET.html(x.Sr) + ' — ' + ET.html(x.PollingStationName) + '</option>');
                });
            }
            if (!$("#et-filter-type option[data-loaded]").length) {
                var types = {};
                currentVehicles.forEach(function (x) { types[x.VehicleTypeCode] = { name: x.VehicleType, icon: x.IconKey }; });
                Object.keys(types).forEach(function (code) {
                    $("#et-filter-type").append('<option data-loaded="1" value="' + ET.attr(code) + '">' + ET.emoji(types[code].icon) + ' ' + ET.html(types[code].name) + '</option>');
                });
            }
        }

        function render(fit) {
            vehicleLayer.clearLayers();
            stationLayer.clearLayers();
            var bounds = [];
            currentStations.forEach(function (station) {
                if (station.Latitude == null || station.Longitude == null) return;
                var latlng = [station.Latitude, station.Longitude];
                L.circle(latlng, { radius: options.geofenceRadius, color: "#0f725c", weight: 1, fillColor: "#0f725c", fillOpacity: .065 }).addTo(stationLayer);
                L.marker(latlng, { icon: ET.stationIcon(station) })
                    .bindPopup(ET.stationPopup(station, ET.replaceId(options.stationDetailsUrl, station.PollingStationId)))
                    .addTo(stationLayer);
                bounds.push(latlng);
            });
            currentVehicles.forEach(function (vehicle) {
                if (vehicle.Latitude == null || vehicle.Longitude == null) return;
                var marker = L.marker([vehicle.Latitude, vehicle.Longitude], { icon: ET.vehicleIcon(vehicle) })
                    .bindPopup(ET.vehiclePopup(vehicle, ET.replaceId(options.vehicleDetailsUrl, vehicle.VehicleAssignmentId)))
                    .on("click", function () { selectVehicle(vehicle.VehicleAssignmentId); })
                    .addTo(vehicleLayer);
                marker._etAssignmentId = vehicle.VehicleAssignmentId;
                bounds.push([vehicle.Latitude, vehicle.Longitude]);
            });
            if (fit && bounds.length) map.fitBounds(bounds, { padding: [25,25], maxZoom: 14 });
            $("#et-map-count").text(ET.number(currentVehicles.length) + " vehicles");
        }

        function renderVehicleList() {
            var html = currentVehicles.map(function (v) {
                return '<button type="button" class="et-list-item et-vehicle-list-button" data-id="' + v.VehicleAssignmentId + '"><div class="et-list-icon">' + ET.emoji(v.IconKey) + '</div><div><div class="et-list-title">' + ET.html(v.RegistrationNo) + ' · ' + ET.html(v.DriverName) + '</div><div class="et-list-meta">' + ET.html(v.PollingStationName || "Unassigned") + ' · ' + ET.number(v.TodayTrips) + ' trips · ' + ET.km(v.TodayDistanceKm) + '</div></div><div>' + ET.statusBadge(v.EffectiveStatus) + '</div></button>';
            }).join("");
            $("#et-live-vehicle-list").html(html || '<div class="et-empty">No vehicles match the filters.</div>');
        }

        function selectVehicle(id) {
            selectedAssignmentId = id;
            var vehicle = currentVehicles.filter(function (x) { return x.VehicleAssignmentId === id; })[0];
            if (!vehicle) return;
            map.setView([vehicle.Latitude, vehicle.Longitude], Math.max(map.getZoom(), 15));
            loadTrail(id);
            var details = [
                '<div class="et-stat-row">',
                '<div class="et-stat"><span>Status</span><strong>', ET.html(ET.statusLabel(vehicle.EffectiveStatus)), '</strong></div>',
                '<div class="et-stat"><span>Trips</span><strong>', ET.number(vehicle.TodayTrips), '</strong></div>',
                '<div class="et-stat"><span>Distance</span><strong>', ET.km(vehicle.TodayDistanceKm), '</strong></div>',
                '<div class="et-stat"><span>Speed</span><strong>', ET.number(vehicle.SpeedKph,0), ' km/h</strong></div>',
                '</div>',
                '<div style="margin-top:13px"><div class="et-name">', ET.emoji(vehicle.IconKey), ' ', ET.html(vehicle.RegistrationNo), '</div>',
                '<div class="et-sub">', ET.html(vehicle.Make || ""), ' ', ET.html(vehicle.Model || ""), ' · ', ET.html(vehicle.Color || ""), '</div></div>',
                '<table class="et-table" style="margin-top:12px"><tbody>',
                '<tr><td>Driver</td><td class="right">', ET.html(vehicle.DriverName || "—"), '</td></tr>',
                '<tr><td>Mobile</td><td class="right">', ET.html(vehicle.DriverMobile || "—"), '</td></tr>',
                '<tr><td>Provider</td><td class="right">', ET.html(vehicle.ProviderName || "—"), '</td></tr>',
                '<tr><td>Polling station</td><td class="right">', ET.html(vehicle.PollingStationName || "—"), '</td></tr>',
                '<tr><td>Last seen</td><td class="right">', ET.dateTime(vehicle.LastSeenAtUtc), '</td></tr>',
                '</tbody></table>',
                '<div style="margin-top:13px"><a class="et-btn primary" href="', ET.attr(ET.replaceId(options.vehicleDetailsUrl, id)), '">Open complete vehicle record</a></div>'
            ].join("");
            ET.openDrawer(details, "Live Vehicle");
        }

        function loadTrail(id) {
            ET.get(options.trailUrl, { id: id, minutes: options.trailMinutes })
                .done(function (response) {
                    trailLayer.clearLayers();
                    if (!response || !response.success || !response.data || response.data.length < 2) return;
                    var points = response.data.map(function (x) { return [x.Latitude, x.Longitude]; });
                    L.polyline(points, { color: "#155eef", weight: 4, opacity: .75 }).addTo(trailLayer);
                    L.circleMarker(points[0], { radius: 5, color: "#079455", fillOpacity: 1 }).bindTooltip("Trail start").addTo(trailLayer);
                    L.circleMarker(points[points.length-1], { radius: 5, color: "#d92d20", fillOpacity: 1 }).bindTooltip("Latest point").addTo(trailLayer);
                });
        }

        function tickDemo() { ET.post(options.simulationUrl, { contextId: options.contextId }); }
        function toggleDemo() {
            if (demoHandle) {
                clearInterval(demoHandle); demoHandle = null;
                $("#et-map-demo").text("Start dummy movement").removeClass("danger").addClass("green-light");
            } else {
                tickDemo(); demoHandle = setInterval(tickDemo, options.demoTickSeconds * 1000);
                $("#et-map-demo").text("Stop dummy movement").removeClass("green-light").addClass("danger");
            }
        }

        $("#et-live-vehicle-list").on("click", ".et-vehicle-list-button", function () { selectVehicle(Number($(this).data("id"))); });
        $("#et-apply-map-filter").on("click", function () { load(true); });
        $("#et-clear-map-filter").on("click", function () { $("#et-filter-status,#et-filter-type,#et-filter-provider,#et-filter-station").val(""); $("#et-filter-search").val(""); load(true); });
        $("#et-map-refresh").on("click", function () { load(false); });
        $("#et-map-demo").on("click", toggleDemo);
        $("#et-filter-search").on("keydown", function (e) { if (e.which === 13) load(true); });

        load(true);
        refreshHandle = setInterval(function () { load(false); if (selectedAssignmentId) loadTrail(selectedAssignmentId); }, options.refreshSeconds * 1000);
        $(window).on("beforeunload", function () { if (refreshHandle) clearInterval(refreshHandle); if (demoHandle) clearInterval(demoHandle); });
    };
})(window, window.jQuery);
