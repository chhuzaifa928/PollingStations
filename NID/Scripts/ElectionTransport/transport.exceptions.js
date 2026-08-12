(function (window, $) {
    "use strict";

    var ET = window.ElectionTransport;
    ET.Exceptions = ET.Exceptions || {};

    ET.Exceptions.init = function (options) {
        var rows = [];

        function txt(v) {
            return v === null || v === undefined ? "" : String(v);
        }

        function upper(v) {
            return txt(v).toUpperCase();
        }

        function num(v) {
            var n = Number(v);
            return isNaN(n) ? 0 : n;
        }

        function severityRank(v) {
            switch (upper(v)) {
                case "CRITICAL": return 4;
                case "HIGH": return 3;
                case "MEDIUM": return 2;
                case "LOW": return 1;
                default: return 0;
            }
        }

        function severityClass(v) {
            switch (upper(v)) {
                case "CRITICAL": return "critical";
                case "HIGH": return "high";
                case "MEDIUM": return "medium";
                case "LOW": return "low";
                default: return "neutral";
            }
        }

        function exceptionType(x) {
            /* Prefer server classification if present. */
            if (x.ExceptionType) return txt(x.ExceptionType);

            var status = upper(x.EffectiveStatus);
            var message = upper(x.Message);

            if (status.indexOf("NEVER") >= 0 || message.indexOf("NEVER ACTIV") >= 0)
                return "Never activated";

            if (status.indexOf("OFFLINE") >= 0 || message.indexOf("OFFLINE") >= 0 ||
                message.indexOf("GPS") >= 0 || message.indexOf("LOCATION") >= 0)
                return "Offline / GPS gap";

            if (status.indexOf("IDLE") >= 0 || message.indexOf("IDLE") >= 0)
                return "Long idle";

            if (num(x.TodayTrips) === 0 || message.indexOf("NO TRIP") >= 0 || message.indexOf("WITHOUT TRIP") >= 0)
                return "Visible with no trips";

            if (message.indexOf("PROMIS") >= 0 || message.indexOf("MISSING") >= 0)
                return "Commitment gap";

            return "Other operational gap";
        }

        function distinctCount(selector) {
            var seen = {};
            rows.forEach(function (x) {
                var v = selector(x);
                if ($.trim(txt(v)) !== "") seen[txt(v)] = true;
            });
            return Object.keys(seen).length;
        }

        function load() {
            $("#et-ex-data-state").text("Loading live exception feed...");

            ET.get(options.url, { contextId: options.contextId })
                .done(function (r) {
                    if (!r || !r.success) {
                        ET.toast((r && r.message) || "Exception data could not be loaded.", "error");
                        return;
                    }

                    /* Supports either a direct array or { items: [] } safely. */
                    if ($.isArray(r.data)) {
                        rows = r.data;
                    } else if (r.data && $.isArray(r.data.Items)) {
                        rows = r.data.Items;
                    } else if (r.data && $.isArray(r.data.items)) {
                        rows = r.data.items;
                    } else {
                        rows = [];
                    }

                    $("#et-ex-data-state").text(
                        ET.number(rows.length) + " live exception record" + (rows.length === 1 ? "" : "s") + " loaded"
                    );

                    renderAll();
                })
                .fail(function (xhr) {
                    $("#et-ex-data-state").text("Exception feed failed to load");
                    ET.toast(xhr.responseText || "Exception centre could not be loaded.", "error");
                });
        }

        function renderAll() {
            renderKpis();
            renderBrief();
            renderSeverityVisual();
            renderCauseVisual();
            renderProviderVisual();
            populateFilters();
            renderTable();
        }

        function renderKpis() {
            var total = rows.length;
            var criticalHigh = 0;
            var offline = 0;
            var neverActivated = 0;
            var noTrips = 0;
            var silenceTotal = 0;
            var silenceCount = 0;

            rows.forEach(function (x) {
                if (severityRank(x.Severity) >= 3) criticalHigh++;

                var type = exceptionType(x).toLowerCase();
                if (type.indexOf("offline") >= 0 || type.indexOf("gps") >= 0) offline++;
                if (type.indexOf("never") >= 0 && type.indexOf("activ") >= 0) neverActivated++;
                if (type.indexOf("no trip") >= 0 || type.indexOf("without trip") >= 0) noTrips++;

                if (x.MinutesSinceLastSeen !== null && x.MinutesSinceLastSeen !== undefined && !isNaN(Number(x.MinutesSinceLastSeen))) {
                    silenceTotal += Number(x.MinutesSinceLastSeen);
                    silenceCount++;
                }
            });

            var providers = distinctCount(function (x) { return x.ProviderName; });
            var stations = distinctCount(function (x) { return x.PollingStationName; });
            var avgSilence = silenceCount ? Math.round(silenceTotal / silenceCount) : 0;
            var riskRate = total ? Math.round((criticalHigh / total) * 100) : 0;

            $("#et-ex-kpi-total").text(ET.number(total));
            $("#et-ex-kpi-critical").text(ET.number(criticalHigh));
            $("#et-ex-kpi-offline").text(ET.number(offline));
            $("#et-ex-kpi-never").text(ET.number(neverActivated));
            $("#et-ex-kpi-notrips").text(ET.number(noTrips));
            $("#et-ex-kpi-providers").text(ET.number(providers));
            $("#et-ex-kpi-stations").text(ET.number(stations));
            $("#et-ex-kpi-silence").text(ET.number(avgSilence) + " min");
            $("#et-ex-critical-rate").text(ET.number(riskRate) + "% high-priority");
            $("#et-exception-count").text(ET.number(total) + " exceptions");
        }

        function renderBrief() {
            if (!rows.length) {
                $("#et-ex-brief-title").text("No active operational exceptions");
                $("#et-ex-brief-copy").text("The current live exception feed contains no vehicle requiring command-centre intervention.");
                return;
            }

            var sorted = rows.slice().sort(function (a, b) {
                return severityRank(b.Severity) - severityRank(a.Severity) ||
                    num(b.MinutesSinceLastSeen) - num(a.MinutesSinceLastSeen);
            });

            var providerCounts = {};
            rows.forEach(function (x) {
                var name = x.ProviderName || "Unassigned provider";
                providerCounts[name] = (providerCounts[name] || 0) + 1;
            });

            var topProvider = Object.keys(providerCounts).sort(function (a, b) {
                return providerCounts[b] - providerCounts[a];
            })[0];

            var criticalHigh = rows.filter(function (x) { return severityRank(x.Severity) >= 3; }).length;
            var top = sorted[0];

            $("#et-ex-brief-title").text(
                criticalHigh
                    ? ET.number(criticalHigh) + " high-priority exception" + (criticalHigh === 1 ? "" : "s") + " require attention"
                    : ET.number(rows.length) + " operational exception" + (rows.length === 1 ? "" : "s") + " under review"
            );

            $("#et-ex-brief-copy").text(
                "Highest provider exposure: " + (topProvider || "Unassigned") +
                " with " + ET.number(providerCounts[topProvider] || 0) + " exception(s). " +
                "Most urgent vehicle: " + (top.RegistrationNo || "Unknown") +
                ". " + (top.Message || exceptionType(top))
            );
        }

        function severityCounts() {
            var counts = { Critical: 0, High: 0, Medium: 0, Low: 0, Other: 0 };
            rows.forEach(function (x) {
                var key = txt(x.Severity);
                if (!counts.hasOwnProperty(key)) key = "Other";
                counts[key]++;
            });
            return counts;
        }

        function renderSeverityVisual() {
            var counts = severityCounts();
            var total = rows.length || 1;
            var criticalPct = (counts.Critical / total) * 100;
            var highPct = (counts.High / total) * 100;
            var mediumPct = (counts.Medium / total) * 100;
            var lowPct = (counts.Low / total) * 100;

            var a = criticalPct;
            var b = a + highPct;
            var c = b + mediumPct;
            var d = c + lowPct;

            $("#et-ex-severity-donut").css(
                "background",
                "conic-gradient(#b42318 0 " + a + "%, #e35d53 " + a + "% " + b + "%, #dc6803 " + b + "% " + c + "%, #2e6fd0 " + c + "% " + d + "%, #d0d5dd " + d + "% 100%)"
            );

            $("#et-ex-severity-total").text(ET.number(rows.length));

            var legend = [
                ["Critical", counts.Critical, "#b42318"],
                ["High", counts.High, "#e35d53"],
                ["Medium", counts.Medium, "#dc6803"],
                ["Low", counts.Low, "#2e6fd0"],
                ["Other", counts.Other, "#98a2b3"]
            ].map(function (x) {
                return '<div class="et-ex-viz-legend-row"><span class="dot" style="background:' + x[2] + '"></span><span>' + ET.html(x[0]) + '</span><strong>' + ET.number(x[1]) + '</strong></div>';
            }).join("");

            $("#et-ex-severity-legend").html(legend);
        }

        function makeCounts(selector) {
            var counts = {};
            rows.forEach(function (x) {
                var key = selector(x) || "Other";
                counts[key] = (counts[key] || 0) + 1;
            });
            return counts;
        }

        function renderBarList(target, counts, limit) {
            var labels = Object.keys(counts).sort(function (a, b) { return counts[b] - counts[a]; });
            if (limit) labels = labels.slice(0, limit);
            var max = labels.length ? counts[labels[0]] : 1;

            var html = labels.map(function (label, i) {
                var value = counts[label];
                var width = max ? Math.max(5, (value / max) * 100) : 0;
                return '<div class="et-ex-viz-bar-row">' +
                    '<div class="et-ex-viz-bar-head"><span>' + ET.html(label) + '</span><strong>' + ET.number(value) + '</strong></div>' +
                    '<div class="et-ex-viz-track"><span style="width:' + width.toFixed(1) + '%"></span></div>' +
                '</div>';
            }).join("");

            $(target).html(html || '<div class="et-empty">No data available.</div>');
        }

        function renderCauseVisual() {
            renderBarList("#et-ex-cause-bars", makeCounts(function (x) { return exceptionType(x); }), 8);
        }

        function renderProviderVisual() {
            renderBarList("#et-ex-provider-bars", makeCounts(function (x) { return x.ProviderName || "Unassigned"; }), 8);
        }

        function populateFilters() {
            var selectedSeverity = $("#et-exception-severity").val();
            var selectedType = $("#et-exception-type").val();
            var severities = {};
            var types = {};

            rows.forEach(function (x) {
                if (x.Severity) severities[txt(x.Severity)] = true;
                types[exceptionType(x)] = true;
            });

            $("#et-exception-severity option[data-dynamic='1']").remove();
            Object.keys(severities).sort().forEach(function (x) {
                $("#et-exception-severity").append('<option data-dynamic="1" value="' + ET.attr(x) + '">' + ET.html(x) + '</option>');
            });
            $("#et-exception-severity").val(selectedSeverity || "");

            $("#et-exception-type option[data-dynamic='1']").remove();
            Object.keys(types).sort().forEach(function (x) {
                $("#et-exception-type").append('<option data-dynamic="1" value="' + ET.attr(x) + '">' + ET.html(x) + '</option>');
            });
            $("#et-exception-type").val(selectedType || "");
        }

        function filteredRows() {
            var search = $.trim($("#et-exception-search").val()).toLowerCase();
            var severity = $("#et-exception-severity").val();
            var type = $("#et-exception-type").val();
            var sort = $("#et-exception-sort").val() || "priority";

            var result = rows.filter(function (x) {
                if (severity && txt(x.Severity) !== severity) return false;
                if (type && exceptionType(x) !== type) return false;

                if (search) {
                    var haystack = [
                        x.RegistrationNo, x.VehicleType, x.DriverName, x.ProviderName,
                        x.PollingStationName, x.EffectiveStatus, x.Message, x.Severity,
                        exceptionType(x)
                    ].join(" ").toLowerCase();
                    if (haystack.indexOf(search) < 0) return false;
                }
                return true;
            });

            result.sort(function (a, b) {
                if (sort === "lastseen") return num(b.MinutesSinceLastSeen) - num(a.MinutesSinceLastSeen);
                if (sort === "trips") return num(a.TodayTrips) - num(b.TodayTrips);
                if (sort === "provider") return txt(a.ProviderName).localeCompare(txt(b.ProviderName));
                if (sort === "station") return txt(a.PollingStationName).localeCompare(txt(b.PollingStationName));
                return severityRank(b.Severity) - severityRank(a.Severity) || num(b.MinutesSinceLastSeen) - num(a.MinutesSinceLastSeen);
            });

            return result;
        }

        function renderTable() {
            var filtered = filteredRows();

            var html = filtered.map(function (x) {
                var sevClass = severityClass(x.Severity);
                var type = exceptionType(x);
                var minutes = (x.MinutesSinceLastSeen === null || x.MinutesSinceLastSeen === undefined)
                    ? null : Number(x.MinutesSinceLastSeen);
                var ageClass = minutes === null ? "neutral" : minutes >= 60 ? "urgent" : minutes >= 20 ? "watch" : "recent";
                var trips = num(x.TodayTrips);

                return '<tr class="et-ex-row et-ex-' + sevClass + '">' +
                    '<td><div class="et-ex-severity-block ' + sevClass + '"><strong>' + ET.html(x.Severity || "Other") + '</strong><span>' + ET.html(type) + '</span></div></td>' +
                    '<td><div class="et-ex-vehicle"><strong>' + ET.html(x.RegistrationNo || "Unregistered") + '</strong><span>' + ET.html((x.VehicleType || "Vehicle") + (x.DriverName ? " - " + x.DriverName : "")) + '</span></div></td>' +
                    '<td><div class="et-ex-main-text">' + ET.html(x.ProviderName || "Unassigned") + '</div></td>' +
                    '<td><div class="et-ex-station">' + ET.html(x.PollingStationName || "Unassigned") + '</div></td>' +
                    '<td>' + ET.statusBadge(x.EffectiveStatus) + '</td>' +
                    '<td class="right"><span class="et-ex-age ' + ageClass + '">' + (minutes === null ? "No GPS" : ET.number(minutes) + " min") + '</span></td>' +
                    '<td class="right"><span class="et-ex-trip-count ' + (trips === 0 ? "zero" : "") + '">' + ET.number(trips) + '</span></td>' +
                    '<td><div class="et-ex-reason"><strong>' + ET.html(type) + '</strong><span>' + ET.html(x.Message || "Operational exception requires review.") + '</span></div></td>' +
                    '<td class="nowrap"><a class="et-btn small navy" href="' + ET.attr(ET.replaceId(options.vehicleDetailsUrl, x.VehicleAssignmentId)) + '">Review vehicle</a></td>' +
                '</tr>';
            }).join("");

            $("#et-exceptions-body").html(html || '<tr><td colspan="9" class="et-empty">No exceptions match the selected filters.</td></tr>');
            $("#et-exception-visible-count").text(ET.number(filtered.length));
            $("#et-exception-total-count").text(ET.number(rows.length));
        }

        $("#et-refresh-exceptions").on("click", load);
        $("#et-exception-search").on("input", renderTable);
        $("#et-exception-severity,#et-exception-type,#et-exception-sort").on("change", renderTable);
        $("#et-exception-clear").on("click", function () {
            $("#et-exception-search").val("");
            $("#et-exception-severity,#et-exception-type").val("");
            $("#et-exception-sort").val("priority");
            renderTable();
        });

        load();
    };

})(window, window.jQuery);
