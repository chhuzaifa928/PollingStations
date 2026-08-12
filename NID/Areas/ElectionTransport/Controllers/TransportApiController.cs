using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;
using NID.Areas.ElectionTransport.Services;

namespace NID.Areas.ElectionTransport.Controllers
{
    public class TransportApiController : Controller
    {
        private readonly ITransportService _service;
        private readonly IDummyTransportSimulator _simulator;

        public TransportApiController()
            : this(new SqlTransportService(), new DummyTransportSimulator())
        {
        }

        public TransportApiController(ITransportService service, IDummyTransportSimulator simulator)
        {
            _service = service;
            _simulator = simulator;
        }

        [HttpGet]
        public async Task<JsonResult> Dashboard(long contextId)
        {
            return LargeJson(new { success = true, data = await _service.GetDashboardAsync(contextId) });
        }

        [HttpGet]
        public async Task<JsonResult> Map(VehicleMapFilterModel filter)
        {
            return LargeJson(new
            {
                success = true,
                serverTimeUtc = DateTime.UtcNow,
                vehicles = await _service.GetLiveVehiclesAsync(filter),
                pollingStations = await _service.GetPollingStationsAsync(filter.ElectionContextId)
            });
        }

        [HttpGet]
        public async Task<JsonResult> Vehicles(VehicleMapFilterModel filter)
        {
            return LargeJson(new { success = true, data = await _service.GetLiveVehiclesAsync(filter) });
        }

        [HttpGet]
        public async Task<JsonResult> Vehicle(long id)
        {
            return LargeJson(new { success = true, data = await _service.GetVehicleDetailsAsync(id) });
        }

        [HttpGet]
        public async Task<JsonResult> VehicleTrail(long id, int? minutes)
        {
            return LargeJson(new
            {
                success = true,
                data = await _service.GetVehicleTrailAsync(id, minutes ?? TransportModuleOptions.TrailMinutes)
            });
        }

        [HttpGet]
        public async Task<JsonResult> PollingStations(long contextId)
        {
            return LargeJson(new { success = true, data = await _service.GetPollingStationsAsync(contextId) });
        }

        [HttpGet]
        public async Task<JsonResult> PollingStation(long id)
        {
            return LargeJson(new { success = true, data = await _service.GetPollingStationDetailsAsync(id) });
        }

        [HttpGet]
        public async Task<JsonResult> Providers(long contextId)
        {
            return LargeJson(new { success = true, data = await _service.GetProvidersAsync(contextId) });
        }

        [HttpGet]
        public async Task<JsonResult> Provider(long id)
        {
            return LargeJson(new { success = true, data = await _service.GetProviderDetailsAsync(id) });
        }

        [HttpGet]
        public async Task<JsonResult> Requests(long contextId, string status)
        {
            return LargeJson(new { success = true, data = await _service.GetRequestsAsync(contextId, status) });
        }

        [HttpGet]
        public async Task<JsonResult> RequestDetails(long id)
        {
            return LargeJson(new { success = true, data = await _service.GetRequestDetailsAsync(id) });
        }

        [HttpGet]
        public async Task<JsonResult> Trips(long contextId, int? take)
        {
            return LargeJson(new { success = true, data = await _service.GetTripsAsync(contextId, take ?? 1000) });
        }

        [HttpGet]
        public async Task<JsonResult> Exceptions(long contextId)
        {
            return LargeJson(new { success = true, data = await _service.GetExceptionsAsync(contextId) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RouteRequest(RouteRequestInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid routing request." });
            }

            try
            {
                var offers = await _service.RouteRequestAsync(
                    model.TransportRequestId,
                    model.OfferCount <= 0 ? 5 : model.OfferCount,
                    CurrentUserName());
                return LargeJson(new { success = true, data = offers });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AcceptDispatch(DispatchAcceptInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid dispatch offer." });
            }

            try
            {
                await _service.AcceptDispatchAsync(model.RequestDispatchId, CurrentUserName());
                return Json(new { success = true, message = "Vehicle assigned successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateRequestStatus(RequestStatusInputModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request status update." });
            }

            try
            {
                await _service.UpdateRequestStatusAsync(
                    model.TransportRequestId,
                    model.NewStatus,
                    model.Remarks,
                    CurrentUserName());
                return Json(new { success = true, message = "Request status updated." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SimulationTick(long contextId)
        {
            if (!TransportModuleOptions.AllowDemoAdministration)
            {
                return Json(new { success = false, message = "Demo administration is disabled." });
            }

            try
            {
                SimulationTickResultDto result = await _simulator.TickAsync(contextId);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SeedDemo(DemoSeedInputModel model)
        {
            if (!TransportModuleOptions.AllowDemoAdministration)
            {
                return Json(new { success = false, message = "Demo administration is disabled." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid demonstration seed values." });
            }

            try
            {
                await _service.SeedDemoDataAsync(model.ElectionContextId, model.VehicleCount, model.RequestCount);
                int routes = await _simulator.EnsureRoutesAsync(model.ElectionContextId);
                return Json(new
                {
                    success = true,
                    message = "Demonstration data prepared successfully.",
                    routesPrepared = routes
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RefreshOffline(long? contextId)
        {
            await _service.RefreshOfflineStatesAsync(contextId);
            return Json(new { success = true });
        }

        private JsonResult LargeJson(object data)
        {
            JsonResult result = Json(data, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            result.RecursionLimit = 200;
            return result;
        }

        private string CurrentUserName()
        {
            return User != null && User.Identity != null && User.Identity.IsAuthenticated
                ? User.Identity.Name
                : "Election Transport User";
        }
    }
}
