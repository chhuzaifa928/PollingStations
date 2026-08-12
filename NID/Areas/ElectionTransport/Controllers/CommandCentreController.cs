using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;
using NID.Areas.ElectionTransport.Services;

namespace NID.Areas.ElectionTransport.Controllers
{
    public class CommandCentreController : Controller
    {
        private readonly ITransportService _service;

        public CommandCentreController()
            : this(new SqlTransportService())
        {
        }

        public CommandCentreController(ITransportService service)
        {
            _service = service;
        }

        public async Task<ActionResult> Index(long? contextId)
        {
            return View(await BuildPageAsync("Transport Command Centre", "dashboard", contextId));
        }

        public async Task<ActionResult> LiveMap(long? contextId)
        {
            return View(await BuildPageAsync("Live Vehicle Map", "map", contextId));
        }

        public async Task<ActionResult> Vehicles(long? contextId)
        {
            return View(await BuildPageAsync("Vehicles", "vehicles", contextId));
        }

        public async Task<ActionResult> Vehicle(long id)
        {
            VehicleDetailsDto details = await _service.GetVehicleDetailsAsync(id);
            if (details == null || details.Vehicle == null)
            {
                return HttpNotFound();
            }

            TransportDetailPageViewModel<VehicleDetailsDto> model =
                await BuildDetailPageAsync("Vehicle Details", "vehicles", details.Vehicle.ElectionContextId, details);
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> ManageVehicle(long contextId, long? id)
        {
            VehicleManageViewModel model = await _service.BuildVehicleFormAsync(contextId, id);
            ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageVehicle(VehicleManageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                VehicleManageViewModel populated =
                    await _service.BuildVehicleFormAsync(model.ElectionContextId, model.VehicleAssignmentId);
                CopyVehicleInput(model, populated);
                ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
                return View(populated);
            }

            try
            {
                long id = await _service.SaveVehicleAsync(model, CurrentUserName());
                TempData["Transport.Success"] = "Vehicle and driver assignment saved successfully.";
                return RedirectToAction("Vehicle", new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                VehicleManageViewModel populated =
                    await _service.BuildVehicleFormAsync(model.ElectionContextId, model.VehicleAssignmentId);
                CopyVehicleInput(model, populated);
                ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
                return View(populated);
            }
        }

        public async Task<ActionResult> PollingStations(long? contextId)
        {
            return View(await BuildPageAsync("Polling Station Transport", "stations", contextId));
        }

        public async Task<ActionResult> PollingStation(long id)
        {
            PollingStationDetailsDto details = await _service.GetPollingStationDetailsAsync(id);
            if (details == null || details.PollingStation == null)
            {
                return HttpNotFound();
            }

            TransportDetailPageViewModel<PollingStationDetailsDto> model =
                await BuildDetailPageAsync("Polling Station Details", "stations", details.PollingStation.ElectionContextId, details);
            return View(model);
        }

        public async Task<ActionResult> Providers(long? contextId)
        {
            return View(await BuildPageAsync("Provider Accountability", "providers", contextId));
        }

        public async Task<ActionResult> Provider(long id)
        {
            ProviderDetailsDto details = await _service.GetProviderDetailsAsync(id);
            if (details == null || details.Provider == null)
            {
                return HttpNotFound();
            }

            TransportDetailPageViewModel<ProviderDetailsDto> model =
                await BuildDetailPageAsync("Provider Details", "providers", details.Provider.ElectionContextId, details);
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> ManageProvider(long contextId, long? id)
        {
            ProviderManageViewModel model = await _service.BuildProviderFormAsync(contextId, id);
            ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManageProvider(ProviderManageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ProviderManageViewModel populated =
                    await _service.BuildProviderFormAsync(model.ElectionContextId, model.ProviderId);
                CopyProviderInput(model, populated);
                ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
                return View(populated);
            }

            try
            {
                long id = await _service.SaveProviderAsync(model, CurrentUserName());
                TempData["Transport.Success"] = "Provider and commitment saved successfully.";
                return RedirectToAction("Provider", new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ProviderManageViewModel populated =
                    await _service.BuildProviderFormAsync(model.ElectionContextId, model.ProviderId);
                CopyProviderInput(model, populated);
                ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
                return View(populated);
            }
        }

        public async Task<ActionResult> Requests(long? contextId)
        {
            return View(await BuildPageAsync("Home & Accessibility Requests", "requests", contextId));
        }

        public async Task<ActionResult> Trips(long? contextId)
        {
            return View(await BuildPageAsync("Trip Verification", "trips", contextId));
        }

        public async Task<ActionResult> Exceptions(long? contextId)
        {
            return View(await BuildPageAsync("Operational Exceptions", "exceptions", contextId));
        }

        public async Task<ActionResult> Analytics(long? contextId)
        {
            return View(await BuildPageAsync("Transport Analytics", "analytics", contextId));
        }

        private async Task<TransportPageViewModel> BuildPageAsync(
            string title,
            string activePage,
            long? contextId)
        {
            var contexts = await _service.GetContextsAsync();
            ElectionContextDto selected = contextId.HasValue
                ? contexts.FirstOrDefault(x => x.ElectionContextId == contextId.Value)
                : contexts.FirstOrDefault();

            if (selected == null)
            {
                throw new InvalidOperationException(
                    "No active election transport context exists. Run Transport.usp_SyncPollingStations first.");
            }

            return new TransportPageViewModel
            {
                PageTitle = title,
                ActivePage = activePage,
                ElectionContextId = selected.ElectionContextId,
                SelectedContext = selected,
                Contexts = contexts,
                RefreshSeconds = TransportModuleOptions.DashboardRefreshSeconds,
                DemoTickSeconds = TransportModuleOptions.DemoTickSeconds,
                TrailMinutes = TransportModuleOptions.TrailMinutes,
                AllowDemoAdministration = TransportModuleOptions.AllowDemoAdministration
            };
        }

        private async Task<TransportDetailPageViewModel<T>> BuildDetailPageAsync<T>(
            string title,
            string activePage,
            long contextId,
            T data)
        {
            TransportPageViewModel page = await BuildPageAsync(title, activePage, contextId);
            return new TransportDetailPageViewModel<T>
            {
                PageTitle = page.PageTitle,
                ActivePage = page.ActivePage,
                ElectionContextId = page.ElectionContextId,
                SelectedContext = page.SelectedContext,
                Contexts = page.Contexts,
                RefreshSeconds = page.RefreshSeconds,
                DemoTickSeconds = page.DemoTickSeconds,
                TrailMinutes = page.TrailMinutes,
                AllowDemoAdministration = page.AllowDemoAdministration,
                Data = data
            };
        }

        private string CurrentUserName()
        {
            return User != null && User.Identity != null && User.Identity.IsAuthenticated
                ? User.Identity.Name
                : "Election Transport User";
        }

        private static void CopyVehicleInput(VehicleManageViewModel source, VehicleManageViewModel target)
        {
            target.VehicleAssignmentId = source.VehicleAssignmentId;
            target.ElectionContextId = source.ElectionContextId;
            target.VehicleTypeId = source.VehicleTypeId;
            target.RegistrationNo = source.RegistrationNo;
            target.DisplayName = source.DisplayName;
            target.Make = source.Make;
            target.Model = source.Model;
            target.ModelYear = source.ModelYear;
            target.Color = source.Color;
            target.SeatingCapacity = source.SeatingCapacity;
            target.OwnerName = source.OwnerName;
            target.OwnerMobile = source.OwnerMobile;
            target.DriverName = source.DriverName;
            target.DriverMobile = source.DriverMobile;
            target.DriverAddress = source.DriverAddress;
            target.DrivingLicenseNo = source.DrivingLicenseNo;
            target.ProviderId = source.ProviderId;
            target.CandidateId = source.CandidateId;
            target.PartyId = source.PartyId;
            target.AssignedPollingStationId = source.AssignedPollingStationId;
            target.AssignmentStatus = source.AssignmentStatus;
            target.MaxServiceRadiusKm = source.MaxServiceRadiusKm;
            target.Remarks = source.Remarks;
            target.IsActive = source.IsActive;
        }

        private static void CopyProviderInput(ProviderManageViewModel source, ProviderManageViewModel target)
        {
            target.ProviderId = source.ProviderId;
            target.ElectionContextId = source.ElectionContextId;
            target.ProviderName = source.ProviderName;
            target.ProviderType = source.ProviderType;
            target.Mobile = source.Mobile;
            target.AlternateMobile = source.AlternateMobile;
            target.Address = source.Address;
            target.Area = source.Area;
            target.CandidateId = source.CandidateId;
            target.PartyId = source.PartyId;
            target.PromisedQuantity = source.PromisedQuantity;
            target.CommitmentPollingStationId = source.CommitmentPollingStationId;
            target.CommitmentVehicleTypeId = source.CommitmentVehicleTypeId;
            target.Remarks = source.Remarks;
            target.IsVerified = source.IsVerified;
            target.IsActive = source.IsActive;
        }
    }
}
