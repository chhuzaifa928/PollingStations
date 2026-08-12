(function (window, $) {
    "use strict";
    var ET = window.ElectionTransport;
    ET.Lists = ET.Lists || {};

    ET.Lists.vehicles = function (options) {
        var data = [];
        function load() {
            ET.get(options.url, {
                ElectionContextId: options.contextId,
                Status: $("#et-vehicle-status").val() || null,
                VehicleTypeCode: $("#et-vehicle-type").val() || null,
                Search: $.trim($("#et-vehicle-search").val()) || null
            }).done(function (r) { if (r && r.success) { data = r.data || []; render(); } });
        }
        function render() {
            var html = data.map(function (x) {
                return '<tr><td><div class="et-name">' + ET.emoji(x.IconKey) + ' ' + ET.html(x.RegistrationNo) + '</div><div class="et-muted">' + ET.html((x.Make || "") + " " + (x.Model || "")) + ' · ' + ET.html(x.Color || "") + '</div></td><td>' + ET.html(x.VehicleType) + '</td><td><div class="et-name">' + ET.html(x.DriverName) + '</div><div class="et-muted">' + ET.html(x.DriverMobile) + '</div></td><td>' + ET.html(x.ProviderName || "—") + '</td><td>' + ET.html(x.PollingStationName || "Unassigned") + '</td><td>' + ET.statusBadge(x.EffectiveStatus) + '</td><td class="right">' + ET.number(x.TodayTrips) + '</td><td class="right">' + ET.km(x.TodayDistanceKm) + '</td><td class="nowrap"><a class="et-btn small light" href="' + ET.attr(ET.replaceId(options.detailsUrl, x.VehicleAssignmentId)) + '">Open</a> <a class="et-btn small green-light" href="' + ET.attr(options.editUrl.replace("__context__", options.contextId).replace("__id__", x.VehicleAssignmentId)) + '">Edit</a></td></tr>';
            }).join("");
            $("#et-vehicles-body").html(html || '<tr><td colspan="9" class="et-empty">No vehicles found.</td></tr>');
            $("#et-vehicle-count").text(ET.number(data.length) + " vehicles");
            populateTypeFilter();
        }
        function populateTypeFilter() {
            if ($("#et-vehicle-type option[data-loaded]").length) return;
            var types = {};
            data.forEach(function (x) { types[x.VehicleTypeCode] = x.VehicleType; });
            Object.keys(types).forEach(function (code) { $("#et-vehicle-type").append('<option data-loaded="1" value="' + ET.attr(code) + '">' + ET.html(types[code]) + '</option>'); });
        }
        $("#et-vehicle-filter").on("click", load);
        $("#et-vehicle-clear").on("click", function () { $("#et-vehicle-status,#et-vehicle-type").val(""); $("#et-vehicle-search").val(""); load(); });
        $("#et-vehicle-search").on("keydown", function (e) { if (e.which === 13) load(); });
        load();
    };

    ET.Lists.stations = function (options) {
        function load() {
            ET.get(options.url, { contextId: options.contextId }).done(function (r) {
                if (!r || !r.success) return;
                var rows = r.data || [];
                var html = rows.map(function (x) {
                    return '<tr><td><div class="et-name">Sr ' + ET.html(x.Sr) + ' — ' + ET.html(x.PollingStationName) + '</div><div class="et-muted">' + ET.html(x.District || "") + ' · ' + ET.html(x.Category || "") + '</div></td><td class="right">' + ET.number(x.TotalVoters) + '</td><td class="right">' + ET.number(x.PromisedVehicles) + '</td><td class="right">' + ET.number(x.AssignedVehicles) + '</td><td class="right">' + ET.number(x.OnlineVehicles) + '</td><td class="right">' + ET.number(x.ValidTrips) + '</td><td class="right">' + ET.km(x.DistanceKm) + '</td><td class="right">' + ET.number(x.OpenTransportRequests) + '</td><td>' + ET.serviceBadge(x.ServiceStatus) + '</td><td><a class="et-btn small navy" href="' + ET.attr(ET.replaceId(options.detailsUrl, x.PollingStationId)) + '">Open</a></td></tr>';
                }).join("");
                $("#et-stations-body").html(html || '<tr><td colspan="10" class="et-empty">No polling stations found.</td></tr>');
                $("#et-station-count").text(ET.number(rows.length) + " stations");
            });
        }
        $("#et-refresh-stations").on("click", load); load();
    };

    ET.Lists.providers = function (options) {
        function load() {
            ET.get(options.url, { contextId: options.contextId }).done(function (r) {
                if (!r || !r.success) return;
                var rows = r.data || [];
                var html = rows.map(function (x) {
                    var cls = x.PerformanceClass === "Strong" ? "green" : x.PerformanceClass === "Satisfactory" ? "blue" : x.PerformanceClass === "Weak" ? "amber" : "red";
                    return '<tr><td><div class="et-name">' + ET.html(x.ProviderName) + '</div><div class="et-muted">' + ET.html(x.ProviderType) + ' · ' + ET.html(x.Mobile || "") + '</div></td><td>' + ET.html(x.CandidateName || x.PartyName || "—") + '</td><td class="right">' + ET.number(x.PromisedVehicles) + '</td><td class="right">' + ET.number(x.RegisteredVehicles) + '</td><td class="right">' + ET.number(x.OperationalVehicles) + '</td><td class="right">' + ET.number(x.Trips) + '</td><td class="right">' + ET.km(x.DistanceKm) + '</td><td class="right">' + ET.percent(x.PromiseFulfilmentPercent) + '</td><td><span class="et-badge ' + cls + '">' + ET.html(x.PerformanceClass) + '</span></td><td class="nowrap"><a class="et-btn small light" href="' + ET.attr(ET.replaceId(options.detailsUrl, x.ProviderId)) + '">Open</a> <a class="et-btn small green-light" href="' + ET.attr(options.editUrl.replace("__context__", options.contextId).replace("__id__", x.ProviderId)) + '">Edit</a></td></tr>';
                }).join("");
                $("#et-providers-body").html(html || '<tr><td colspan="10" class="et-empty">No provider records.</td></tr>');
                $("#et-provider-count").text(ET.number(rows.length) + " providers");
            });
        }
        $("#et-refresh-providers").on("click", load); load();
    };

    ET.Lists.trips = function (options) {
        var chart = null;
        function load() {
            ET.get(options.url, { contextId: options.contextId, take: 2000 }).done(function (r) {
                if (!r || !r.success) return;
                var rows = r.data || [];
                var html = rows.map(function (x) {
                    return '<tr><td>#' + ET.number(x.TripId) + '</td><td><div class="et-name">' + ET.html(x.RegistrationNo) + '</div><div class="et-muted">' + ET.html(x.VehicleType) + ' · ' + ET.html(x.DriverName) + '</div></td><td>' + ET.html(x.PollingStationName) + '</td><td>' + ET.html(x.ProviderName || "—") + '</td><td>' + ET.dateTime(x.ArrivedAtUtc) + '</td><td class="right">' + ET.number(x.DistanceKm, 2) + '</td><td class="right">' + ET.number(x.DurationMinutes, 1) + '</td><td class="right">' + ET.number(x.DwellSeconds) + ' sec</td><td class="right">' + ET.number(x.ConfirmedPassengers) + '</td><td>' + (x.ValidationStatus === "VALID" ? '<span class="et-badge green">Valid</span>' : '<span class="et-badge red">' + ET.html(x.ValidationStatus) + '</span>') + '</td></tr>';
                }).join("");
                $("#et-trips-body").html(html || '<tr><td colspan="10" class="et-empty">No trips recorded.</td></tr>');
                renderChart(rows);
            });
        }
        function renderChart(rows) {
            var buckets = {};
            rows.forEach(function (x) { var key = ET.time(x.ArrivedAtUtc || x.CompletedAtUtc || x.StartedAtUtc); buckets[key] = (buckets[key] || 0) + 1; });
            var labels = Object.keys(buckets).sort();
            if (chart) chart.destroy();
            chart = new Chart(document.getElementById("et-trip-chart"), { type: "bar", data: { labels: labels, datasets: [{ label: "Trips", data: labels.map(function (x) { return buckets[x]; }), borderRadius: 5 }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } } });
        }
        $("#et-refresh-trips").on("click", load); load();
    };

    ET.Lists.exceptions = function (options) {
        function load() {
            ET.get(options.url, { contextId: options.contextId }).done(function (r) {
                if (!r || !r.success) return;
                var rows = r.data || [];
                var html = rows.map(function (x) {
                    return '<tr><td><span class="et-badge ' + (x.Severity === "Critical" || x.Severity === "High" ? "red" : "amber") + '">' + ET.html(x.Severity) + '</span></td><td><div class="et-name">' + ET.html(x.RegistrationNo) + '</div><div class="et-muted">' + ET.html(x.VehicleType) + ' · ' + ET.html(x.DriverName) + '</div></td><td>' + ET.html(x.ProviderName || "—") + '</td><td>' + ET.html(x.PollingStationName || "—") + '</td><td>' + ET.statusBadge(x.EffectiveStatus) + '</td><td class="right">' + (x.MinutesSinceLastSeen == null ? "—" : ET.number(x.MinutesSinceLastSeen) + " min") + '</td><td class="right">' + ET.number(x.TodayTrips) + '</td><td>' + ET.html(x.Message) + '</td><td><a class="et-btn small light" href="' + ET.attr(ET.replaceId(options.vehicleDetailsUrl, x.VehicleAssignmentId)) + '">Review</a></td></tr>';
                }).join("");
                $("#et-exceptions-body").html(html || '<tr><td colspan="9" class="et-empty">No current exceptions.</td></tr>');
                $("#et-exception-count").text(ET.number(rows.length) + " exceptions");
            });
        }
        $("#et-refresh-exceptions").on("click", load); load();
    };
})(window, window.jQuery);
