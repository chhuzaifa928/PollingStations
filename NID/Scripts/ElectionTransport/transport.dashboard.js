(function (window, $) {
    "use strict";

    var ET = window.ElectionTransport;

    ET.Dashboard = ET.Dashboard || {};

    ET.Dashboard.init = function (options) {

        var map =
            L.map(
                "et-dashboard-map",
                {
                    preferCanvas: true,
                    zoomControl: true
                }
            )
                .setView(
                    [30.4, 69.3],
                    6
                );


        L.tileLayer(
            "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            {
                maxZoom: 19,
                attribution: "&copy; OpenStreetMap contributors"
            }
        )
            .addTo(map);


        var stationLayer =
            L.layerGroup()
                .addTo(map);

        var vehicleLayer =
            L.layerGroup()
                .addTo(map);


        var timelineChart = null;
        var typeChart = null;

        var refreshHandle = null;
        var demoHandle = null;

        var loading = false;

        var currentVehicles = [];
        var currentStations = [];

        var stationMarkers = {};
        var vehicleMarkers = [];

        var focusedStationId = null;
        var initialMapFitDone = false;


        /* =========================================================
           LOAD / RENDER
           ========================================================= */

        function load() {

            if (loading) {
                return;
            }

            loading = true;

            ET.loading(
                $("#et-dashboard-content"),
                true
            );


            ET.get(
                options.dashboardUrl,
                {
                    contextId:
                        options.contextId
                }
            )
                .done(function (response) {

                    if (
                        !response ||
                        !response.success
                    ) {

                        ET.toast(
                            "Dashboard data could not be loaded.",
                            "error"
                        );

                        return;
                    }

                    render(
                        response.data
                    );
                })
                .fail(function (xhr) {

                    ET.toast(
                        xhr.responseText ||
                        "Dashboard request failed.",
                        "error"
                    );
                })
                .always(function () {

                    loading = false;

                    ET.loading(
                        $("#et-dashboard-content"),
                        false
                    );
                });
        }


        function render(data) {

            data =
                data || {};

            currentVehicles =
                data.Vehicles || [];

            currentStations =
                data.PollingStations || [];


            renderSummary(
                data.Summary || {}
            );

            renderMap(
                currentVehicles,
                currentStations
            );

            renderTimeline(
                data.Timeline || []
            );

            renderVehicleTypes(
                data.VehicleTypes || []
            );

            renderProviders(
                data.Providers || []
            );

            renderStations(
                currentStations
            );

            renderRequests(
                data.Requests || []
            );

            renderExceptions(
                data.Exceptions || []
            );


            $("#et-server-time")
                .text(
                    ET.dateTime(
                        data.ServerTimeUtc
                    )
                );
        }


        function set(id, value) {

            $(id)
                .text(value);
        }


        /* =========================================================
           SUMMARY
           ========================================================= */

        function renderSummary(s) {

            set(
                "#et-kpi-promised",
                ET.number(
                    s.PromisedVehicles
                )
            );

            set(
                "#et-kpi-active",
                ET.number(
                    s.ActiveVehicles
                )
            );

            set(
                "#et-kpi-trips",
                ET.number(
                    s.TripsCompleted
                )
            );

            set(
                "#et-kpi-distance",
                ET.number(
                    s.DistanceKm,
                    1
                )
            );

            set(
                "#et-kpi-missing",
                ET.number(
                    (s.OfflineVehicles || 0) +
                    (s.NeverActivatedVehicles || 0)
                )
            );

            set(
                "#et-kpi-stations",
                ET.number(
                    s.PollingStationsServed
                ) +
                " / " +
                ET.number(
                    s.TotalPollingStations
                )
            );

            set(
                "#et-kpi-requests",
                ET.number(
                    s.OpenRequests
                )
            );

            set(
                "#et-kpi-passengers",
                ET.number(
                    s.ConfirmedPassengers
                )
            );

            set(
                "#et-fulfilment",
                ET.percent(
                    s.PromiseFulfilmentPercent
                )
            );


            $("#et-fulfilment-bar")
                .css(
                    "width",
                    Math.min(
                        100,
                        Number(
                            s.PromiseFulfilmentPercent || 0
                        )
                    ) +
                    "%"
                );


            set(
                "#et-coverage",
                ET.percent(
                    s.PollingStationCoveragePercent
                )
            );


            $("#et-coverage-bar")
                .css(
                    "width",
                    Math.min(
                        100,
                        Number(
                            s.PollingStationCoveragePercent || 0
                        )
                    ) +
                    "%"
                );
        }


        /* =========================================================
           MAP HELPERS
           ========================================================= */

        function sameId(a, b) {

            return String(a) === String(b);
        }


        function stationHasVehicle(station) {

            return Number(
                station.AssignedVehicles || 0
            ) > 0;
        }


        function dashboardStationIcon(station) {

            var hasVehicle =
                stationHasVehicle(
                    station
                );

            var markerColor =
                hasVehicle
                    ? "#111827"
                    : "#d92d20";


            return L.divIcon({

                className:
                    "",

                iconSize:
                    [32, 32],

                iconAnchor:
                    [16, 29],

                popupAnchor:
                    [0, -27],

                html:
                    '<div class="et-station-marker" ' +
                    'style="background:' +
                    markerColor +
                    ';">' +
                    '<span>' +
                    ET.html(
                        station.Sr
                    ) +
                    '</span>' +
                    '</div>'
            });
        }


        function stationPopupHtml(station) {

            var stationUrl =
                ET.replaceId(
                    options.stationDetailsUrl,
                    station.PollingStationId
                );

            var hasVehicle =
                stationHasVehicle(
                    station
                );


            return [
                '<div class="et-map-popup et-dashboard-station-popup">',

                '<div class="et-station-popup-title">',
                'Polling Station Sr ',
                ET.html(
                    station.Sr
                ),
                '</div>',

                '<div class="et-station-popup-name">',
                ET.html(
                    station.PollingStationName ||
                    "Polling station"
                ),
                '</div>',

                (
                    !hasVehicle

                        ? '<div class="et-station-popup-alert">' +
                        'No vehicle is currently assigned to this polling station.' +
                        '</div>'

                        : ''
                ),

                '<div class="et-popup-grid" style="margin-top:9px">',

                '<div>',
                '<span>Assigned</span>',
                '<strong>',
                ET.number(
                    station.AssignedVehicles
                ),
                '</strong>',
                '</div>',

                '<div>',
                '<span>Online</span>',
                '<strong>',
                ET.number(
                    station.OnlineVehicles
                ),
                '</strong>',
                '</div>',

                '<div>',
                '<span>Valid trips</span>',
                '<strong>',
                ET.number(
                    station.ValidTrips
                ),
                '</strong>',
                '</div>',

                '<div>',
                '<span>Open requests</span>',
                '<strong>',
                ET.number(
                    station.OpenTransportRequests
                ),
                '</strong>',
                '</div>',

                '</div>',

                '<div class="et-station-popup-actions">',

                '<button type="button" ' +
                'class="focus et-map-focus-station" ' +
                'data-id="' +
                ET.attr(
                    station.PollingStationId
                ) +
                '">' +
                'View station vehicles' +
                '</button>',

                '<a class="record" href="',
                ET.attr(
                    stationUrl
                ),
                '">',
                'Station record',
                '</a>',

                '</div>',

                '</div>'
            ]
                .join("");
        }


        function allMapBounds() {

            var points = [];


            currentStations
                .forEach(
                    function (station) {

                        if (
                            station.Latitude == null ||
                            station.Longitude == null
                        ) {
                            return;
                        }

                        points.push(
                            [
                                station.Latitude,
                                station.Longitude
                            ]
                        );
                    }
                );


            currentVehicles
                .forEach(
                    function (vehicle) {

                        if (
                            vehicle.Latitude == null ||
                            vehicle.Longitude == null
                        ) {
                            return;
                        }

                        points.push(
                            [
                                vehicle.Latitude,
                                vehicle.Longitude
                            ]
                        );
                    }
                );


            return points;
        }


        function fitAllMap(animate) {

            var points =
                allMapBounds();


            if (!points.length) {
                return;
            }


            var bounds =
                L.latLngBounds(
                    points
                );


            if (animate === true) {

                map.flyToBounds(
                    bounds,
                    {
                        paddingTopLeft:
                            [35, 50],

                        paddingBottomRight:
                            [35, 45],

                        maxZoom:
                            14,

                        duration:
                            0.9
                    }
                );
            }
            else {

                map.fitBounds(
                    bounds,
                    {
                        padding:
                            [25, 25],

                        maxZoom:
                            14
                    }
                );
            }
        }


        function stationById(id) {

            var found = null;


            currentStations
                .some(
                    function (station) {

                        if (
                            sameId(
                                station.PollingStationId,
                                id
                            )
                        ) {

                            found =
                                station;

                            return true;
                        }

                        return false;
                    }
                );


            return found;
        }


        function vehiclesForStation(id) {

            return currentVehicles
                .filter(
                    function (vehicle) {

                        return (
                            vehicle.PollingStationId != null &&
                            sameId(
                                vehicle.PollingStationId,
                                id
                            )
                        );
                    }
                );
        }


        function applyMapFocusVisuals(stationId) {

            Object.keys(
                stationMarkers
            )
                .forEach(
                    function (key) {

                        var marker =
                            stationMarkers[key];

                        marker.setOpacity(
                            sameId(
                                key,
                                stationId
                            )
                                ? 1
                                : 0.28
                        );
                    }
                );


            vehicleMarkers
                .forEach(
                    function (item) {

                        item.marker
                            .setOpacity(
                                sameId(
                                    item.stationId,
                                    stationId
                                )
                                    ? 1
                                    : 0.16
                            );
                    }
                );
        }


        function clearMapFocusVisuals() {

            Object.keys(
                stationMarkers
            )
                .forEach(
                    function (key) {

                        stationMarkers[key]
                            .setOpacity(1);
                    }
                );


            vehicleMarkers
                .forEach(
                    function (item) {

                        item.marker
                            .setOpacity(1);
                    }
                );
        }


        function updateFocusBar(station, vehicles) {

            $("#et-map-focus-title")
                .text(
                    "Polling Station Sr " +
                    ET.number(
                        station.Sr
                    ) +
                    " - " +
                    (
                        station.PollingStationName ||
                        "Polling station"
                    )
                );


            $("#et-map-focus-meta")
                .text(
                    ET.number(
                        station.AssignedVehicles
                    ) +
                    " assigned, " +
                    ET.number(
                        station.OnlineVehicles
                    ) +
                    " online, " +
                    ET.number(
                        vehicles.length
                    ) +
                    " vehicle records in dashboard"
                );


            $("#et-map-focus-bar")
                .addClass(
                    "open"
                );
        }


        function focusStation(
            stationId,
            animate,
            openPopup
        ) {

            var station =
                stationById(
                    stationId
                );


            if (
                !station ||
                station.Latitude == null ||
                station.Longitude == null
            ) {

                ET.toast(
                    "Polling-station coordinates are not available.",
                    "error"
                );

                return;
            }


            focusedStationId =
                station.PollingStationId;


            var vehicles =
                vehiclesForStation(
                    station.PollingStationId
                );


            applyMapFocusVisuals(
                station.PollingStationId
            );


            updateFocusBar(
                station,
                vehicles
            );


            var points = [
                [
                    station.Latitude,
                    station.Longitude
                ]
            ];


            vehicles
                .forEach(
                    function (vehicle) {

                        if (
                            vehicle.Latitude == null ||
                            vehicle.Longitude == null
                        ) {
                            return;
                        }

                        points.push(
                            [
                                vehicle.Latitude,
                                vehicle.Longitude
                            ]
                        );
                    }
                );


            var marker =
                stationMarkers[
                String(
                    station.PollingStationId
                )
                ];


            function showPopup() {

                if (
                    openPopup !== false &&
                    marker
                ) {

                    marker.openPopup();
                }
            }


            if (points.length > 1) {

                var bounds =
                    L.latLngBounds(
                        points
                    );


                if (animate === false) {

                    map.fitBounds(
                        bounds,
                        {
                            paddingTopLeft:
                                [55, 75],

                            paddingBottomRight:
                                [55, 60],

                            maxZoom:
                                15
                        }
                    );

                    showPopup();
                }
                else {

                    map.once(
                        "moveend",
                        showPopup
                    );


                    map.flyToBounds(
                        bounds,
                        {
                            paddingTopLeft:
                                [55, 75],

                            paddingBottomRight:
                                [55, 60],

                            maxZoom:
                                15,

                            duration:
                                1.05
                        }
                    );
                }
            }
            else {

                if (animate === false) {

                    map.setView(
                        [
                            station.Latitude,
                            station.Longitude
                        ],
                        16
                    );

                    showPopup();
                }
                else {

                    map.once(
                        "moveend",
                        showPopup
                    );


                    map.flyTo(
                        [
                            station.Latitude,
                            station.Longitude
                        ],
                        16,
                        {
                            duration:
                                1.05
                        }
                    );
                }
            }
        }


        function resetMapFocus() {

            focusedStationId =
                null;

            clearMapFocusVisuals();

            $("#et-map-focus-bar")
                .removeClass(
                    "open"
                );

            map.closePopup();

            fitAllMap(
                true
            );
        }


        /* =========================================================
           MAP RENDER
           ========================================================= */

        function renderMap(vehicles, stations) {

            stationLayer.clearLayers();
            vehicleLayer.clearLayers();

            stationMarkers = {};
            vehicleMarkers = [];


            stations
                .forEach(
                    function (station) {

                        if (
                            station.Latitude == null ||
                            station.Longitude == null
                        ) {
                            return;
                        }


                        var latlng = [
                            station.Latitude,
                            station.Longitude
                        ];


                        var hasVehicle =
                            stationHasVehicle(
                                station
                            );


                        var stationColor =
                            hasVehicle
                                ? "#111827"
                                : "#d92d20";


                        L.circle(
                            latlng,
                            {
                                radius:
                                    options.geofenceRadius,

                                color:
                                    stationColor,

                                weight:
                                    1,

                                fillColor:
                                    stationColor,

                                fillOpacity:
                                    hasVehicle
                                        ? 0.035
                                        : 0.065
                            }
                        )
                            .addTo(
                                stationLayer
                            );


                        var marker =
                            L.marker(
                                latlng,
                                {
                                    icon:
                                        dashboardStationIcon(
                                            station
                                        )
                                }
                            )
                                .bindPopup(
                                    stationPopupHtml(
                                        station
                                    ),
                                    {
                                        maxWidth:
                                            330
                                    }
                                )
                                .on(
                                    "dblclick",
                                    function (e) {

                                        if (
                                            e &&
                                            e.originalEvent
                                        ) {

                                            L.DomEvent.stop(
                                                e.originalEvent
                                            );
                                        }


                                        focusStation(
                                            station.PollingStationId,
                                            true,
                                            true
                                        );
                                    }
                                )
                                .addTo(
                                    stationLayer
                                );


                        stationMarkers[
                            String(
                                station.PollingStationId
                            )
                        ] = marker;
                    }
                );


            vehicles
                .forEach(
                    function (vehicle) {

                        if (
                            vehicle.Latitude == null ||
                            vehicle.Longitude == null
                        ) {
                            return;
                        }


                        var latlng = [
                            vehicle.Latitude,
                            vehicle.Longitude
                        ];


                        var url =
                            ET.replaceId(
                                options.vehicleDetailsUrl,
                                vehicle.VehicleAssignmentId
                            );


                        var marker =
                            L.marker(
                                latlng,
                                {
                                    icon:
                                        ET.vehicleIcon(
                                            vehicle
                                        ),

                                    rotationAngle:
                                        vehicle.HeadingDegrees || 0
                                }
                            )
                                .bindPopup(
                                    ET.vehiclePopup(
                                        vehicle,
                                        url
                                    )
                                )
                                .addTo(
                                    vehicleLayer
                                );


                        vehicleMarkers.push(
                            {
                                marker:
                                    marker,

                                stationId:
                                    vehicle.PollingStationId,

                                assignmentId:
                                    vehicle.VehicleAssignmentId
                            }
                        );
                    }
                );


            if (
                focusedStationId != null &&
                stationById(
                    focusedStationId
                )
            ) {

                applyMapFocusVisuals(
                    focusedStationId
                );

                updateFocusBar(
                    stationById(
                        focusedStationId
                    ),
                    vehiclesForStation(
                        focusedStationId
                    )
                );
            }
            else if (!initialMapFitDone) {

                fitAllMap(
                    false
                );

                initialMapFitDone =
                    true;
            }


            window.setTimeout(
                function () {

                    map.invalidateSize();
                },
                80
            );
        }


        /* =========================================================
           CHARTS
           ========================================================= */

        function renderTimeline(rows) {

            var labels =
                rows.map(
                    function (x) {

                        return ET.time(
                            x.BucketUtc
                        );
                    }
                );


            var active =
                rows.map(
                    function (x) {

                        return x.ActiveVehicles;
                    }
                );


            var trips =
                rows.map(
                    function (x) {

                        return x.Trips;
                    }
                );


            var requests =
                rows.map(
                    function (x) {

                        return x.Requests;
                    }
                );


            if (timelineChart) {

                timelineChart.destroy();
            }


            timelineChart =
                new Chart(
                    document.getElementById(
                        "et-timeline-chart"
                    ),
                    {
                        type:
                            "bar",

                        data:
                        {
                            labels:
                                labels,

                            datasets:
                                [
                                    {
                                        type:
                                            "line",

                                        label:
                                            "Active vehicles",

                                        data:
                                            active,

                                        borderWidth:
                                            2,

                                        tension:
                                            0.28,

                                        yAxisID:
                                            "y1"
                                    },

                                    {
                                        label:
                                            "Trips",

                                        data:
                                            trips,

                                        borderWidth:
                                            1,

                                        borderRadius:
                                            5
                                    },

                                    {
                                        label:
                                            "Requests",

                                        data:
                                            requests,

                                        borderWidth:
                                            1,

                                        borderRadius:
                                            5
                                    }
                                ]
                        },

                        options:
                        {
                            responsive:
                                true,

                            maintainAspectRatio:
                                false,

                            interaction:
                            {
                                mode:
                                    "index",

                                intersect:
                                    false
                            },

                            plugins:
                            {
                                legend:
                                {
                                    position:
                                        "bottom"
                                }
                            },

                            scales:
                            {
                                y:
                                {
                                    beginAtZero:
                                        true
                                },

                                y1:
                                {
                                    beginAtZero:
                                        true,

                                    position:
                                        "right",

                                    grid:
                                    {
                                        drawOnChartArea:
                                            false
                                    }
                                }
                            }
                        }
                    }
                );
        }


        function renderVehicleTypes(rows) {

            if (typeChart) {

                typeChart.destroy();
            }


            typeChart =
                new Chart(
                    document.getElementById(
                        "et-type-chart"
                    ),
                    {
                        type:
                            "doughnut",

                        data:
                        {
                            labels:
                                rows.map(
                                    function (x) {

                                        return (
                                            ET.emoji(
                                                x.IconKey
                                            ) +
                                            " " +
                                            x.VehicleType
                                        );
                                    }
                                ),

                            datasets:
                                [
                                    {
                                        data:
                                            rows.map(
                                                function (x) {

                                                    return x.Total;
                                                }
                                            ),

                                        borderWidth:
                                            2
                                    }
                                ]
                        },

                        options:
                        {
                            responsive:
                                true,

                            maintainAspectRatio:
                                false,

                            cutout:
                                "62%",

                            plugins:
                            {
                                legend:
                                {
                                    position:
                                        "bottom"
                                }
                            }
                        }
                    }
                );
        }


        /* =========================================================
           PROVIDERS
           ========================================================= */

        function renderProviders(rows) {

            var html =
                rows.length

                    ? rows.map(
                        function (x, i) {

                            var url =
                                ET.replaceId(
                                    options.providerDetailsUrl,
                                    x.ProviderId
                                );


                            return '' +

                                '<a class="et-provider-account-row" ' +
                                'href="' +
                                ET.attr(
                                    url
                                ) +
                                '">' +

                                '<div class="et-provider-rank">' +
                                (i + 1) +
                                '</div>' +

                                '<div class="et-provider-copy">' +

                                '<div class="et-provider-name">' +
                                ET.html(
                                    x.ProviderName
                                ) +
                                '</div>' +

                                '<div class="et-provider-meta">' +

                                ET.html(
                                    x.ProviderType ||
                                    x.PerformanceClass ||
                                    "Provider"
                                ) +

                                ' &middot; ' +

                                ET.number(
                                    x.OperationalVehicles
                                ) +

                                '/' +

                                ET.number(
                                    x.PromisedVehicles
                                ) +

                                ' operational' +

                                ' &middot; ' +

                                ET.number(
                                    x.Trips
                                ) +

                                ' trips' +

                                '</div>' +

                                '</div>' +

                                '<div class="et-provider-score">' +
                                ET.percent(
                                    x.EffectivenessScore
                                ) +
                                '</div>' +

                                '</a>';
                        }
                    )
                        .join("")

                    : '<div class="et-empty">' +
                    'No provider performance records.' +
                    '</div>';


            $("#et-provider-list")
                .html(
                    html
                );
        }


        /* =========================================================
           POLLING-STATION SERVICE GAPS
           ========================================================= */

        function renderStations(rows) {

            var selected =
                rows.slice()
                    .sort(
                        function (a, b) {

                            var aNoVehicle =
                                Number(
                                    a.AssignedVehicles || 0
                                ) === 0
                                    ? 0
                                    : 1;

                            var bNoVehicle =
                                Number(
                                    b.AssignedVehicles || 0
                                ) === 0
                                    ? 0
                                    : 1;


                            if (
                                aNoVehicle !==
                                bNoVehicle
                            ) {

                                return (
                                    aNoVehicle -
                                    bNoVehicle
                                );
                            }


                            var ar =
                                a.ServiceStatus === "Operational"
                                    ? 2
                                    : a.ServiceStatus === "Low Service"
                                        ? 1
                                        : 0;

                            var br =
                                b.ServiceStatus === "Operational"
                                    ? 2
                                    : b.ServiceStatus === "Low Service"
                                        ? 1
                                        : 0;


                            return (
                                ar -
                                br ||
                                Number(
                                    b.TotalVoters || 0
                                ) -
                                Number(
                                    a.TotalVoters || 0
                                )
                            );
                        }
                    )
                    .slice(
                        0,
                        12
                    );


            var html =
                selected.map(
                    function (x) {

                        var assigned =
                            Number(
                                x.AssignedVehicles || 0
                            );

                        var online =
                            Number(
                                x.OnlineVehicles || 0
                            );

                        var noVehicle =
                            assigned === 0;

                        var onlinePercent =
                            assigned > 0
                                ? Math.min(
                                    100,
                                    Math.round(
                                        (
                                            online /
                                            assigned
                                        ) *
                                        100
                                    )
                                )
                                : 0;


                        return '' +

                            '<tr class="et-station-gap-row ' +
                            (
                                noVehicle
                                    ? 'et-no-vehicle'
                                    : ''
                            ) +
                            '">' +

                            '<td>' +

                            '<div class="et-station-identity">' +

                            '<span class="et-station-ref">' +
                            'PS ' +
                            ET.html(
                                x.Sr
                            ) +
                            '</span>' +

                            '<div class="et-station-copy">' +

                            '<strong>' +
                            ET.html(
                                x.PollingStationName
                            ) +
                            '</strong>' +

                            '<span>' +
                            ET.html(
                                x.District ||
                                x.Tehsil ||
                                ""
                            ) +
                            '</span>' +

                            '</div>' +

                            '</div>' +

                            '</td>' +

                            '<td class="right">' +

                            '<span class="et-metric-strong">' +
                            ET.number(
                                x.TotalVoters
                            ) +
                            '</span>' +

                            '</td>' +

                            '<td class="right et-fleet-cell">' +

                            '<div class="et-fleet-numbers">' +

                            '<strong>' +
                            ET.number(
                                online
                            ) +
                            '</strong>' +

                            '<span>' +
                            '/ ' +
                            ET.number(
                                assigned
                            ) +
                            '</span>' +

                            '</div>' +

                            '<div class="et-mini-progress">' +

                            '<span style="width:' +
                            onlinePercent +
                            '%"></span>' +

                            '</div>' +

                            '</td>' +

                            '<td class="right">' +

                            '<span class="et-metric-strong">' +
                            ET.number(
                                x.ValidTrips
                            ) +
                            '</span>' +

                            '</td>' +

                            '<td>' +

                            ET.serviceBadge(
                                x.ServiceStatus
                            ) +

                            '</td>' +

                            '<td class="right">' +

                            '<button type="button" ' +
                            'class="et-station-map-action" ' +
                            'data-id="' +
                            ET.attr(
                                x.PollingStationId
                            ) +
                            '">' +
                            'View map' +
                            '</button>' +

                            '</td>' +

                            '</tr>';
                    }
                )
                    .join("");


            $("#et-station-gap-body")
                .html(
                    html ||
                    '<tr>' +
                    '<td colspan="6" class="et-empty">' +
                    'No station data.' +
                    '</td>' +
                    '</tr>'
                );
        }


        /* =========================================================
           REQUESTS
           ========================================================= */

        function renderRequests(rows) {

            var html =
                rows.map(
                    function (x) {

                        var waiting =
                            Number(
                                x.WaitingMinutes || 0
                            );

                        var waitClass =
                            waiting >= 30
                                ? "urgent"
                                : waiting >= 15
                                    ? "watch"
                                    : "good";

                        var priorityClass =
                            Number(
                                x.Priority || 0
                            ) <= 2
                                ? "priority"
                                : "";


                        var assistance =
                            x.AccessibilityCategory &&
                                String(
                                    x.AccessibilityCategory
                                ).toLowerCase() !== "general"

                                ? '<span class="et-request-assistance">' +
                                ET.html(
                                    x.AccessibilityCategory
                                ) +
                                '</span>'

                                : '';


                        return '' +

                            '<tr class="et-request-row ' +
                            priorityClass +
                            '">' +

                            '<td>' +

                            '<div class="et-request-identity">' +

                            '<strong>' +
                            ET.html(
                                x.RequestNo
                            ) +
                            '</strong>' +

                            '<span>' +
                            ET.html(
                                x.RequestedByName ||
                                "Requestor"
                            ) +
                            '</span>' +

                            assistance +

                            '</div>' +

                            '</td>' +

                            '<td>' +

                            '<div class="et-request-pickup">' +

                            '<strong>' +
                            ET.html(
                                x.PickupArea ||
                                "Pickup"
                            ) +
                            '</strong>' +

                            '<span>' +
                            ET.html(
                                x.PickupAddress ||
                                ""
                            ) +
                            '</span>' +

                            '</div>' +

                            '</td>' +

                            '<td class="right">' +

                            '<span class="et-passenger-pill">' +
                            ET.number(
                                x.PassengerCount
                            ) +
                            '</span>' +

                            '</td>' +

                            '<td>' +

                            ET.requestBadge(
                                x.RequestStatus
                            ) +

                            '</td>' +

                            '<td class="right">' +

                            '<span class="et-wait-pill ' +
                            waitClass +
                            '">' +

                            '<strong>' +
                            ET.number(
                                waiting
                            ) +
                            '</strong>' +

                            '<span>min</span>' +

                            '</span>' +

                            '</td>' +

                            '</tr>';
                    }
                )
                    .join("");


            $("#et-request-body")
                .html(
                    html ||
                    '<tr>' +
                    '<td colspan="5" class="et-empty">' +
                    'No open transport requests.' +
                    '</td>' +
                    '</tr>'
                );
        }


        /* =========================================================
           EXCEPTIONS
           ========================================================= */

        function renderExceptions(rows) {

            var html =
                rows.map(
                    function (x) {

                        return '' +

                            '<div class="et-exception-card">' +

                            '<div class="et-exception-symbol">' +
                            '!' +
                            '</div>' +

                            '<div class="et-exception-copy">' +

                            '<strong>' +

                            ET.html(
                                x.RegistrationNo
                            ) +

                            ' - ' +

                            ET.html(
                                x.ExceptionType
                            ) +

                            '</strong>' +

                            '<span>' +

                            ET.html(
                                x.ProviderName ||
                                "No provider"
                            ) +

                            ' &middot; ' +

                            ET.html(
                                x.Message ||
                                ""
                            ) +

                            '</span>' +

                            '</div>' +

                            '<div>' +

                            ET.statusBadge(
                                x.EffectiveStatus
                            ) +

                            '</div>' +

                            '</div>';
                    }
                )
                    .join("");


            $("#et-exception-list")
                .html(
                    html ||
                    '<div class="et-empty">' +
                    'No current exceptions.' +
                    '</div>'
                );
        }


        /* =========================================================
           DEMO
           ========================================================= */

        function tickDemo() {

            ET.post(
                options.simulationUrl,
                {
                    contextId:
                        options.contextId
                }
            )
                .fail(function () {

                    stopDemo();
                });
        }


        function startDemo() {

            if (demoHandle) {
                return;
            }


            $("#et-demo-toggle")
                .text(
                    "Stop dummy traffic"
                )
                .removeClass(
                    "green-light"
                )
                .addClass(
                    "danger"
                );


            tickDemo();


            demoHandle =
                window.setInterval(
                    tickDemo,
                    options.demoTickSeconds * 1000
                );


            ET.toast(
                "Dummy vehicle traffic started.",
                "success"
            );
        }


        function stopDemo() {

            if (demoHandle) {

                window.clearInterval(
                    demoHandle
                );
            }


            demoHandle =
                null;


            $("#et-demo-toggle")
                .text(
                    "Start dummy traffic"
                )
                .removeClass(
                    "danger"
                )
                .addClass(
                    "green-light"
                );
        }


        /* =========================================================
           EVENTS
           ========================================================= */

        $("#et-refresh-dashboard")
            .on(
                "click",
                load
            );


        $("#et-demo-toggle")
            .on(
                "click",
                function () {

                    demoHandle
                        ? stopDemo()
                        : startDemo();
                }
            );


        $("#et-seed-demo")
            .on(
                "click",
                function () {

                    if (
                        !window.confirm(
                            "Create/update demonstration vehicles, trips and home transport requests for this constituency?"
                        )
                    ) {
                        return;
                    }


                    ET.post(
                        options.seedUrl,
                        {
                            ElectionContextId:
                                options.contextId,

                            VehicleCount:
                                60,

                            RequestCount:
                                30
                        }
                    )
                        .done(function (r) {

                            r.success

                                ? (
                                    ET.toast(
                                        r.message,
                                        "success"
                                    ),
                                    load()
                                )

                                : ET.toast(
                                    r.message,
                                    "error"
                                );
                        })
                        .fail(function (xhr) {

                            ET.toast(
                                xhr.responseText ||
                                "Demo seed failed.",
                                "error"
                            );
                        });
                }
            );


        $("#et-dashboard-map")
            .on(
                "click",
                ".et-map-focus-station",
                function (e) {

                    e.preventDefault();
                    e.stopPropagation();


                    focusStation(
                        $(this).data("id"),
                        true,
                        true
                    );
                }
            );


        $("#et-station-gap-body")
            .on(
                "click",
                ".et-station-map-action",
                function () {

                    focusStation(
                        $(this).data("id"),
                        true,
                        true
                    );


                    var mapTop =
                        $("#et-dashboard-map")
                            .offset();

                    if (mapTop) {

                        $("html, body")
                            .animate(
                                {
                                    scrollTop:
                                        Math.max(
                                            0,
                                            mapTop.top - 110
                                        )
                                },
                                280
                            );
                    }
                }
            );


        $("#et-map-reset-focus")
            .on(
                "click",
                resetMapFocus
            );


        /* =========================================================
           INITIAL LOAD / REFRESH
           ========================================================= */

        load();


        refreshHandle =
            window.setInterval(
                load,
                options.refreshSeconds * 1000
            );


        $(window)
            .on(
                "beforeunload",
                function () {

                    if (refreshHandle) {

                        clearInterval(
                            refreshHandle
                        );
                    }


                    if (demoHandle) {

                        clearInterval(
                            demoHandle
                        );
                    }
                }
            );
    };

})(
    window,
    window.jQuery
);
