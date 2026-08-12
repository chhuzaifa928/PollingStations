(function (window, $) {
    "use strict";
    var ET = window.ElectionTransport;
    ET.Analytics = ET.Analytics || {};

    ET.Analytics.init = function (options) {
        var productivityChart = null, providerChart = null, stationChart = null;
        ET.get(options.dashboardUrl, { contextId: options.contextId }).done(function (r) {
            if (!r || !r.success) return;
            var d = r.data, vehicles = d.Vehicles || [], providers = d.Providers || [], stations = d.PollingStations || [];
            renderProductivity(vehicles); renderProviders(providers); renderStations(stations); renderNarrative(d);
        });

        function renderProductivity(rows) {
            if (productivityChart) productivityChart.destroy();
            productivityChart = new Chart(document.getElementById("et-productivity-chart"), { type: "scatter", data: { datasets: [{ label: "Vehicles", data: rows.map(function (x) { return { x: Number(x.TodayDistanceKm || 0), y: Number(x.TodayTrips || 0), vehicle: x.RegistrationNo }; }), pointRadius: 6 }] }, options: { responsive: true, maintainAspectRatio: false, parsing: false, plugins: { tooltip: { callbacks: { label: function (ctx) { return ctx.raw.vehicle + ': ' + ctx.raw.y + ' trips, ' + ET.number(ctx.raw.x,1) + ' km'; } } }, legend: { display: false } }, scales: { x: { title: { display: true, text: "Distance travelled (km)" }, beginAtZero: true }, y: { title: { display: true, text: "Valid trips" }, beginAtZero: true } } } });
        }
        function renderProviders(rows) {
            if (providerChart) providerChart.destroy();
            providerChart = new Chart(document.getElementById("et-provider-chart"), { type: "bar", data: { labels: rows.map(function (x) { return x.ProviderName; }), datasets: [{ label: "Effectiveness score", data: rows.map(function (x) { return x.EffectivenessScore; }), borderRadius: 5 }] }, options: { responsive: true, maintainAspectRatio: false, indexAxis: "y", plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true, max: 100 } } } });
        }
        function renderStations(rows) {
            var selected = rows.slice().sort(function (a,b) { return b.TotalVoters-a.TotalVoters; }).slice(0,15);
            if (stationChart) stationChart.destroy();
            stationChart = new Chart(document.getElementById("et-station-chart"), { type: "bar", data: { labels: selected.map(function (x) { return 'Sr '+x.Sr; }), datasets: [{ label: "Trips", data: selected.map(function (x) { return x.ValidTrips; }) }, { label: "Online vehicles", data: selected.map(function (x) { return x.OnlineVehicles; }) }] }, options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: "bottom" } }, scales: { y: { beginAtZero: true } } } });
        }
        function renderNarrative(d) {
            var s=d.Summary||{}, vehicles=d.Vehicles||[], providers=d.Providers||[], stations=d.PollingStations||[];
            var star=vehicles.slice().sort(function(a,b){return b.TodayTrips-a.TodayTrips;})[0];
            var weak=providers.slice().sort(function(a,b){return a.EffectivenessScore-b.EffectivenessScore;})[0];
            var gap=stations.filter(function(x){return x.ServiceStatus!=="Operational";}).sort(function(a,b){return b.TotalVoters-a.TotalVoters;})[0];
            var html='<div class="et-list-item"><div class="et-list-icon">★</div><div><div class="et-list-title">Highest vehicle productivity</div><div class="et-list-meta">'+(star?ET.html(star.RegistrationNo)+' completed '+ET.number(star.TodayTrips)+' trips and '+ET.km(star.TodayDistanceKm):'No vehicle data')+'</div></div></div>'+
                '<div class="et-list-item"><div class="et-list-icon">⚠</div><div><div class="et-list-title">Provider requiring attention</div><div class="et-list-meta">'+(weak?ET.html(weak.ProviderName)+' has an effectiveness score of '+ET.percent(weak.EffectivenessScore):'No provider data')+'</div></div></div>'+
                '<div class="et-list-item"><div class="et-list-icon">PS</div><div><div class="et-list-title">Largest service gap by voter load</div><div class="et-list-meta">'+(gap?'Sr '+ET.html(gap.Sr)+' — '+ET.html(gap.PollingStationName)+' ('+ET.number(gap.TotalVoters)+' voters), '+ET.html(gap.ServiceStatus):'All stations operational')+'</div></div></div>'+
                '<div class="et-list-item"><div class="et-list-icon">%</div><div><div class="et-list-title">Promise fulfilment</div><div class="et-list-meta">'+ET.percent(s.PromiseFulfilmentPercent)+' of promised vehicles are currently operational; station coverage is '+ET.percent(s.PollingStationCoveragePercent)+'.</div></div></div>';
            $("#et-analytics-insights").html(html);
        }
    };
})(window, window.jQuery);
