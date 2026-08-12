(function (window, $) {
    "use strict";

    var ET = window.ElectionTransport;
    ET.Requests = ET.Requests || {};

    ET.Requests.init = function (options) {
        var rows = [];
        var timer = null;

        function safe(value, fallback) {
            return value === null || value === undefined || value === ""
                ? (fallback === undefined ? "—" : fallback)
                : value;
        }

        function normalize(value) {
            return String(value === null || value === undefined ? "" : value)
                .toLowerCase()
                .replace(/\s+/g, " ")
                .trim();
        }

        function toTime(value) {
            if (!value) return 0;

            if (typeof value === "string") {
                var match = /\/Date\((\d+)/.exec(value);
                if (match) return Number(match[1]) || 0;
            }

            var parsed = Date.parse(value);
            return isNaN(parsed) ? 0 : parsed;
        }

        function statusMeta(status) {
            var map = {
                NEW: { label: "New", css: "new" },
                ROUTING: { label: "Finding vehicle", css: "routing" },
                OFFERED: { label: "Vehicle offered", css: "offered" },
                ASSIGNED: { label: "Assigned", css: "assigned" },
                DRIVER_EN_ROUTE: { label: "Driver en route", css: "enroute" },
                PICKED_UP: { label: "Picked up", css: "pickup" },
                DROPPED_OFF: { label: "Dropped off", css: "dropoff" },
                COMPLETED: { label: "Completed", css: "completed" },
                NO_VEHICLE: { label: "No vehicle", css: "problem" },
                CANCELLED: { label: "Cancelled", css: "cancelled" }
            };

            return map[status] || { label: status || "Unknown", css: "new" };
        }

        function itemClass(x) {
            if (Number(x.Priority) <= 2) return "priority";
            if (["ASSIGNED", "DRIVER_EN_ROUTE", "PICKED_UP", "DROPPED_OFF"].indexOf(x.RequestStatus) >= 0) return "inservice";
            if (x.RequestStatus === "COMPLETED") return "completed";
            if (x.RequestStatus === "NO_VEHICLE") return "problem";
            return "";
        }

        function isOpen(x) {
            return ["COMPLETED", "CANCELLED"].indexOf(x.RequestStatus) < 0;
        }

        function isPriority(x) {
            return Number(x.Priority) <= 2 ||
                !!x.RequiresWheelchair ||
                !!x.RequiresAttendant ||
                (x.AccessibilityCategory && normalize(x.AccessibilityCategory) !== "general");
        }

        function assistanceChips(x) {
            var chips = [];

            if (x.AccessibilityCategory && normalize(x.AccessibilityCategory) !== "general") {
                chips.push('<span class="et-rq-chip assist">' + ET.html(x.AccessibilityCategory) + '</span>');
            }
            if (x.RequiresWheelchair) chips.push('<span class="et-rq-chip assist">♿ Wheelchair</span>');
            if (x.RequiresAttendant) chips.push('<span class="et-rq-chip">+ Attendant</span>');
            if (x.IsRoundTripRequired) chips.push('<span class="et-rq-chip blue">↔ Round trip</span>');
            if (!chips.length) chips.push('<span class="et-rq-chip">General transport</span>');

            return chips.join("");
        }

        function requestActions(x) {
            var action = '<button class="et-btn small light et-request-details" type="button" data-id="' + x.TransportRequestId + '">Details</button>';

            if (["NEW", "ROUTING", "NO_VEHICLE"].indexOf(x.RequestStatus) >= 0) {
                action += '<button class="et-btn small primary et-route-request" type="button" data-id="' + x.TransportRequestId + '">Route vehicle</button>';
            }
            if (x.RequestStatus === "ASSIGNED") {
                action += '<button class="et-btn small blue et-request-status" type="button" data-id="' + x.TransportRequestId + '" data-status="DRIVER_EN_ROUTE">Driver en route</button>';
            }
            if (x.RequestStatus === "DRIVER_EN_ROUTE") {
                action += '<button class="et-btn small amber et-request-status" type="button" data-id="' + x.TransportRequestId + '" data-status="PICKED_UP">Picked up</button>';
            }
            if (x.RequestStatus === "PICKED_UP") {
                action += '<button class="et-btn small primary et-request-status" type="button" data-id="' + x.TransportRequestId + '" data-status="DROPPED_OFF">Dropped off</button>';
            }
            if (x.RequestStatus === "DROPPED_OFF") {
                action += '<button class="et-btn small primary et-request-status" type="button" data-id="' + x.TransportRequestId + '" data-status="COMPLETED">Complete</button>';
            }

            return action;
        }

        function renderStats() {
            var total = rows.length;
            var routing = rows.filter(function (x) {
                return ["NEW", "ROUTING", "NO_VEHICLE"].indexOf(x.RequestStatus) >= 0;
            }).length;
            var service = rows.filter(function (x) {
                return ["ASSIGNED", "DRIVER_EN_ROUTE", "PICKED_UP", "DROPPED_OFF"].indexOf(x.RequestStatus) >= 0;
            }).length;
            var completed = rows.filter(function (x) { return x.RequestStatus === "COMPLETED"; }).length;
            var priority = rows.filter(isPriority).length;
            var noVehicle = rows.filter(function (x) { return x.RequestStatus === "NO_VEHICLE"; }).length;
            var openRows = rows.filter(isOpen);
            var passengers = openRows.reduce(function (sum, x) { return sum + Number(x.PassengerCount || 0); }, 0);
            var avgWait = openRows.length
                ? openRows.reduce(function (sum, x) { return sum + Number(x.WaitingMinutes || 0); }, 0) / openRows.length
                : 0;

            $("#et-rq-stat-total").text(ET.number(total));
            $("#et-rq-stat-routing").text(ET.number(routing));
            $("#et-rq-stat-service").text(ET.number(service));
            $("#et-rq-stat-completed").text(ET.number(completed));
            $("#et-rq-stat-priority").text(ET.number(priority));
            $("#et-rq-stat-novehicle").text(ET.number(noVehicle));
            $("#et-rq-stat-passengers").text(ET.number(passengers));
            $("#et-rq-stat-wait").text(ET.number(avgWait, 0) + " min");
            $("#et-request-count").text(ET.number(total));
            $("#et-request-total-count").text(ET.number(total));
        }

        function filteredRows() {
            var status = $("#et-request-status-filter").val() || "";
            var query = normalize($("#et-request-search").val());
            var sort = $("#et-request-sort").val() || "waiting-desc";

            var result = rows.filter(function (x) {
                if (status && x.RequestStatus !== status) return false;
                if (!query) return true;

                var haystack = normalize([
                    x.RequestNo,
                    x.RequestedByName,
                    x.Mobile,
                    x.PickupArea,
                    x.PickupAddress,
                    x.PollingStationName,
                    x.RegistrationNo,
                    x.VehicleType,
                    x.DriverName,
                    x.DriverMobile,
                    x.ProviderName,
                    x.AccessibilityCategory,
                    x.RequestStatus
                ].join(" "));

                return haystack.indexOf(query) >= 0;
            });

            result.sort(function (a, b) {
                switch (sort) {
                    case "priority":
                        return Number(a.Priority || 99) - Number(b.Priority || 99) ||
                            Number(b.WaitingMinutes || 0) - Number(a.WaitingMinutes || 0);
                    case "newest":
                        return toTime(b.RequestedAtUtc) - toTime(a.RequestedAtUtc);
                    case "oldest":
                        return toTime(a.RequestedAtUtc) - toTime(b.RequestedAtUtc);
                    case "passengers-desc":
                        return Number(b.PassengerCount || 0) - Number(a.PassengerCount || 0) ||
                            Number(b.WaitingMinutes || 0) - Number(a.WaitingMinutes || 0);
                    case "status":
                        return String(a.RequestStatus || "").localeCompare(String(b.RequestStatus || "")) ||
                            Number(b.WaitingMinutes || 0) - Number(a.WaitingMinutes || 0);
                    case "waiting-desc":
                    default:
                        return Number(b.WaitingMinutes || 0) - Number(a.WaitingMinutes || 0);
                }
            });

            return result;
        }

        function renderRequest(x) {
            var meta = statusMeta(x.RequestStatus);
            var wait = Number(x.WaitingMinutes || 0);
            var waitClass = wait >= 30 ? "critical" : (wait >= 15 ? "warn" : "");
            var station = safe(x.PollingStationName, "Station not selected");
            var pickupTitle = safe(x.PickupArea, "Home pickup");
            var pickupAddress = safe(x.PickupAddress, "Pickup address not supplied");
            var assignmentTitle = x.RegistrationNo
                ? safe(x.RegistrationNo) + (x.VehicleType ? " · " + safe(x.VehicleType) : "")
                : "Awaiting vehicle assignment";
            var assignmentSub = x.RegistrationNo
                ? [x.DriverName, x.DriverMobile].filter(Boolean).join(" · ")
                : "Use Route Vehicle to find nearby eligible transport";
            var provider = x.ProviderName ? '<span class="et-rq-chip blue">Provider: ' + ET.html(x.ProviderName) + '</span>' : "";

            return '' +
                '<article class="et-rq-item ' + itemClass(x) + '">' +
                '<div class="et-rq-item-top">' +
                '<div class="et-rq-idline">' +
                '<strong>' + ET.html(safe(x.RequestNo, "#" + x.TransportRequestId)) + '</strong>' +
                '<span>' + ET.dateTime(x.RequestedAtUtc) + '</span>' +
                (Number(x.Priority) <= 2 ? '<span class="et-rq-priority">Priority ' + ET.number(x.Priority) + '</span>' : '') +
                '</div>' +
                '<span class="et-rq-status ' + meta.css + '">' + meta.label + '</span>' +
                '</div>' +

                '<div class="et-rq-item-main">' +
                '<div class="et-rq-block">' +
                '<span class="et-rq-label">Requestor</span>' +
                '<strong>' + ET.html(safe(x.RequestedByName, "Unnamed requestor")) + '</strong>' +
                '<small>' + ET.html(safe(x.Mobile)) + '</small>' +
                '</div>' +

                '<div class="et-rq-block">' +
                '<span class="et-rq-label">Transport route</span>' +
                '<div class="et-rq-route-line">' +
                '<div class="et-rq-route-point"><strong>' + ET.html(pickupTitle) + '</strong><small>' + ET.html(pickupAddress) + '</small></div>' +
                '<div class="et-rq-route-arrow">→</div>' +
                '<div class="et-rq-route-point"><strong>' + ET.html(station) + '</strong><small>' + (x.PollingStationSr ? 'Polling station #' + ET.html(x.PollingStationSr) : 'Destination polling station') + '</small></div>' +
                '</div>' +
                '</div>' +

                '<div class="et-rq-block et-rq-service">' +
                '<span class="et-rq-label">Service requirement</span>' +
                '<strong>' + ET.number(x.PassengerCount || 1) + ' passenger' + (Number(x.PassengerCount || 1) === 1 ? '' : 's') + '</strong>' +
                '<div class="et-rq-service-row">' + assistanceChips(x) + '</div>' +
                '</div>' +

                '<div class="et-rq-block et-rq-dispatch">' +
                '<span class="et-rq-label">Dispatch & waiting</span>' +
                '<strong>' + ET.html(assignmentTitle) + '</strong>' +
                '<small>' + ET.html(assignmentSub) + '</small>' +
                '<div class="et-rq-assignment-row">' + provider + '<span class="et-rq-wait ' + waitClass + '">' + ET.number(wait) + '<small>min wait</small></span></div>' +
                '</div>' +
                '</div>' +

                '<div class="et-rq-item-actions">' + requestActions(x) + '</div>' +
                '</article>';
        }

        function render() {
            renderStats();

            var visible = filteredRows();
            var html = visible.map(renderRequest).join("");

            $("#et-requests-body").html(html ||
                '<div class="et-rq-empty">' +
                '<strong>No requests found</strong>' +
                '<span>Try a different status, search term or sort option.</span>' +
                '</div>');

            $("#et-request-visible-count").text(ET.number(visible.length));
        }

        function load() {
            /* Always load the full constituency queue. Status/search/sort are local.
               This keeps KPI cards representative of the full request operation. */
            ET.get(options.requestsUrl, {
                contextId: options.contextId,
                status: null
            })
                .done(function (r) {
                    if (r && r.success) {
                        rows = r.data || [];
                        render();
                    } else {
                        ET.toast(r && r.message ? r.message : "Request queue could not be loaded.", "error");
                    }
                })
                .fail(function () {
                    ET.toast("Request queue could not be loaded.", "error");
                });
        }

        function drawerAssistance(q) {
            return assistanceChips(q);
        }

        function renderAssignedVehicle(q) {
            if (!q.RegistrationNo) {
                return '<div class="et-rqd-empty">No vehicle has been assigned to this request yet.</div>';
            }

            return '' +
                '<div class="et-rqd-assigned">' +
                '<div class="et-rqd-vehicle-icon">🚗</div>' +
                '<div>' +
                '<span>Assigned vehicle</span>' +
                '<strong>' + ET.html(q.RegistrationNo) + (q.VehicleType ? ' · ' + ET.html(q.VehicleType) : '') + '</strong>' +
                '<small>' + ET.html(safe(q.DriverName, "Driver")) + (q.DriverMobile ? ' · ' + ET.html(q.DriverMobile) : '') + '</small>' +
                '</div>' +
                (q.ProviderName
                    ? '<div class="et-rqd-provider"><span>Provider</span><strong>' + ET.html(q.ProviderName) + '</strong></div>'
                    : '') +
                '</div>';
        }

        function renderOffers(dispatches) {
            if (!dispatches.length) {
                return '<div class="et-rqd-empty">No current vehicle offers. Use Route Vehicle from the request card when routing is required.</div>';
            }

            return dispatches.map(function (x) {
                var distanceKm = Number(x.DriverDistanceMeters || 0) / 1000;
                var etaMin = Math.max(1, Math.ceil(Number(x.EstimatedArrivalSeconds || 0) / 60));

                return '' +
                    '<div class="et-rqd-offer">' +
                    '<div class="et-rqd-offer-vehicle">' +
                    '<span class="emoji">' + ET.emoji(x.IconKey) + '</span>' +
                    '<div>' +
                    '<strong>' + ET.html(safe(x.RegistrationNo, "Vehicle")) + (x.VehicleType ? ' · ' + ET.html(x.VehicleType) : '') + '</strong>' +
                    '<small>' + ET.html(safe(x.DriverName, "Driver")) + (x.ProviderName ? ' · ' + ET.html(x.ProviderName) : '') + '</small>' +
                    '</div>' +
                    '</div>' +
                    '<div class="et-rqd-offer-metric"><span>Distance</span><strong>' + ET.number(distanceKm, 1) + ' km</strong></div>' +
                    '<div class="et-rqd-offer-metric"><span>ETA</span><strong>' + ET.number(etaMin) + ' min</strong></div>' +
                    '<button class="et-btn small primary et-accept-dispatch" type="button" data-id="' + x.RequestDispatchId + '">Assign</button>' +
                    '</div>';
            }).join("");
        }

        function renderHistory(history) {
            if (!history.length) return '<div class="et-rqd-empty">No status history is available.</div>';

            return '<div class="et-rqd-history">' + history.map(function (x) {
                var meta = statusMeta(x.NewStatus);
                return '' +
                    '<div class="et-rqd-history-item">' +
                    '<i class="et-rqd-history-dot"></i>' +
                    '<strong>' + ET.html(meta.label) + '</strong>' +
                    '<span>' + ET.dateTime(x.ChangedAtUtc) + ' · ' + ET.html(safe(x.ChangedBy, "System")) + '</span>' +
                    (x.Remarks ? '<p>' + ET.html(x.Remarks) + '</p>' : '') +
                    '</div>';
            }).join("") + '</div>';
        }

        function showDetails(id) {
            ET.get(options.requestDetailsUrl, { id: id })
                .done(function (r) {
                    if (!r || !r.success || !r.data || !r.data.Request) {
                        ET.toast("Request details could not be loaded.", "error");
                        return;
                    }

                    var d = r.data;
                    var q = d.Request;
                    var dispatches = d.Dispatches || [];
                    var history = d.StatusHistory || [];
                    var meta = statusMeta(q.RequestStatus);
                    var wait = Number(q.WaitingMinutes || 0);

                    var html = '' +
                        '<div class="et-rqd">' +
                        '<div class="et-rqd-summary">' +
                        '<div class="et-rqd-stat"><span>Status</span><strong><span class="et-rq-status ' + meta.css + '">' + ET.html(meta.label) + '</span></strong></div>' +
                        '<div class="et-rqd-stat"><span>Passengers</span><strong>' + ET.number(q.PassengerCount || 0) + '</strong></div>' +
                        '<div class="et-rqd-stat"><span>Priority</span><strong>' + ET.number(q.Priority || 0) + '</strong></div>' +
                        '<div class="et-rqd-stat"><span>Waiting</span><strong>' + ET.number(wait) + ' min</strong></div>' +
                        '</div>' +

                        '<section class="et-rqd-section">' +
                        '<div class="et-rqd-section-title"><span>Requestor & assistance</span><small>' + ET.dateTime(q.RequestedAtUtc) + '</small></div>' +
                        '<div class="et-rqd-person-grid">' +
                        '<div class="et-rqd-field"><span>Requestor</span><strong>' + ET.html(safe(q.RequestedByName)) + '</strong></div>' +
                        '<div class="et-rqd-field"><span>Mobile</span><strong>' + ET.html(safe(q.Mobile)) + '</strong></div>' +
                        '<div class="et-rqd-field"><span>Passengers</span><strong>' + ET.number(q.PassengerCount || 0) + '</strong></div>' +
                        '<div class="et-rqd-field"><span>Requested pickup</span><strong>' + (q.RequestedPickupAtUtc ? ET.dateTime(q.RequestedPickupAtUtc) : 'Not specified') + '</strong></div>' +
                        '</div>' +
                        '<div class="et-rqd-assistance">' + drawerAssistance(q) + '</div>' +
                        '</section>' +

                        '<section class="et-rqd-section">' +
                        '<div class="et-rqd-section-title"><span>Pickup → polling station</span></div>' +
                        '<div class="et-rqd-route">' +
                        '<div class="et-rqd-route-box"><span>Pickup location</span><strong>' + ET.html(safe(q.PickupArea, "Home pickup")) + '</strong><small>' + ET.html(safe(q.PickupAddress)) + '</small></div>' +
                        '<div class="et-rqd-route-arrow">→</div>' +
                        '<div class="et-rqd-route-box"><span>Polling station</span><strong>' + ET.html(safe(q.PollingStationName, "Not selected")) + '</strong><small>' + (q.PollingStationSr ? 'Polling station #' + ET.html(q.PollingStationSr) : 'Destination') + '</small></div>' +
                        '</div>' +
                        '</section>' +

                        '<section class="et-rqd-section">' +
                        '<div class="et-rqd-section-title"><span>Current vehicle assignment</span></div>' +
                        renderAssignedVehicle(q) +
                        '</section>' +

                        '<section class="et-rqd-section">' +
                        '<div class="et-rqd-section-title"><span>Dispatch offers</span><small>' + ET.number(dispatches.length) + ' candidate vehicles</small></div>' +
                        '<div class="et-rqd-offers">' + renderOffers(dispatches) + '</div>' +
                        '</section>' +

                        '<section class="et-rqd-section">' +
                        '<div class="et-rqd-section-title"><span>Status history</span><small>' + ET.number(history.length) + ' events</small></div>' +
                        renderHistory(history) +
                        '</section>' +
                        '</div>';

                    ET.openDrawer(html, "Request " + safe(q.RequestNo, "#" + q.TransportRequestId));
                })
                .fail(function () {
                    ET.toast("Request details could not be loaded.", "error");
                });
        }

        function route(id) {
            ET.post(options.routeUrl, { TransportRequestId: id, OfferCount: 5 })
                .done(function (r) {
                    if (r && r.success) {
                        var offers = r.data || [];
                        ET.toast(offers.length + " nearby vehicle" + (offers.length === 1 ? "" : "s") + " offered.", offers.length ? "success" : "warning");
                        load();
                        showDetails(id);
                    } else {
                        ET.toast(r && r.message ? r.message : "Routing failed.", "error");
                    }
                })
                .fail(function () { ET.toast("Routing failed.", "error"); });
        }

        function accept(id) {
            ET.post(options.acceptUrl, { RequestDispatchId: id })
                .done(function (r) {
                    if (r && r.success) {
                        ET.toast(r.message || "Vehicle assigned.", "success");
                        ET.closeDrawer();
                        load();
                    } else {
                        ET.toast(r && r.message ? r.message : "Assignment failed.", "error");
                    }
                })
                .fail(function () { ET.toast("Assignment failed.", "error"); });
        }

        function update(id, status) {
            ET.post(options.statusUrl, {
                TransportRequestId: id,
                NewStatus: status,
                Remarks: "Updated from election transport command centre."
            })
                .done(function (r) {
                    if (r && r.success) {
                        ET.toast(r.message || "Request status updated.", "success");
                        load();
                    } else {
                        ET.toast(r && r.message ? r.message : "Status update failed.", "error");
                    }
                })
                .fail(function () { ET.toast("Status update failed.", "error"); });
        }

        /* Card actions */
        $("#et-requests-body").on("click", ".et-request-details", function () {
            showDetails(Number($(this).data("id")));
        });

        $("#et-requests-body").on("click", ".et-route-request", function () {
            route(Number($(this).data("id")));
        });

        $("#et-requests-body").on("click", ".et-request-status", function () {
            update(Number($(this).data("id")), $(this).data("status"));
        });

        $(document).on("click", ".et-accept-dispatch", function () {
            accept(Number($(this).data("id")));
        });

        /* Local controls - no server round-trip required. */
        $("#et-request-status-filter, #et-request-sort").on("change", render);
        $("#et-request-search").on("input", render);
        $("#et-refresh-requests").on("click", load);

        load();
        timer = window.setInterval(load, options.refreshSeconds * 1000);

        $(window).on("beforeunload", function () {
            if (timer) window.clearInterval(timer);
        });
    };

})(window, window.jQuery);
