using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using NID.Areas.ElectionTransport.Models;
using NID.Areas.ElectionTransport.Services;

namespace NID.Areas.ElectionTransport.Controllers
{
    [AllowAnonymous]
    public class PublicTransportController : Controller
    {
        private readonly ITransportService _service;

        public PublicTransportController()
            : this(new SqlTransportService())
        {
        }

        public PublicTransportController(ITransportService service)
        {
            _service = service;
        }

        [HttpGet]
        [ActionName("Request")]
        public async Task<ActionResult> RequestTransport(
            long? contextId,
            long? stationId,
            long? partyId,
            long? candidateId)
        {
            var contexts = await _service.GetContextsAsync();
            var context = contextId.HasValue
                ? contexts.FirstOrDefault(x => x.ElectionContextId == contextId.Value)
                : contexts.FirstOrDefault();

            if (context == null)
            {
                return new HttpStatusCodeResult(503, "No active election transport context is available.");
            }

            PublicTransportRequestViewModel model = await _service.BuildPublicRequestFormAsync(
                context.ElectionContextId,
                stationId,
                partyId,
                candidateId);
            ViewBag.Context = context;
            return View("Request", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Request")]
        public async Task<ActionResult> RequestTransport(PublicTransportRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PublicTransportRequestViewModel populated = await _service.BuildPublicRequestFormAsync(
                    model.ElectionContextId,
                    model.PollingStationId,
                    model.ServicePartyId,
                    model.ServiceCandidateId);
                CopyRequestInput(model, populated);
                ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
                return View("Request", populated);
            }

            try
            {
                PublicRequestConfirmationViewModel confirmation =
                    await _service.CreatePublicRequestAsync(model, "Public Web Portal");
                TempData["PublicTransport.Confirmation"] = confirmation;
                return RedirectToAction("Confirmation", new { id = confirmation.RequestNo });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PublicTransportRequestViewModel populated = await _service.BuildPublicRequestFormAsync(
                    model.ElectionContextId,
                    model.PollingStationId,
                    model.ServicePartyId,
                    model.ServiceCandidateId);
                CopyRequestInput(model, populated);
                ViewBag.Context = await _service.GetContextAsync(model.ElectionContextId);
                return View("Request", populated);
            }
        }

        [HttpGet]
        public ActionResult Confirmation(string id)
        {
            PublicRequestConfirmationViewModel model =
                TempData["PublicTransport.Confirmation"] as PublicRequestConfirmationViewModel;

            if (model == null)
            {
                model = new PublicRequestConfirmationViewModel
                {
                    RequestNo = id,
                    RequestStatus = "RECEIVED",
                    Message = "Your request has been received. Use the request number and the last four digits of your mobile to track it."
                };
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Track()
        {
            return View(new PublicRequestTrackViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Track(PublicRequestTrackViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Status = await _service.GetPublicRequestStatusAsync(model.RequestNo, model.MobileLast4);
                if (model.Status == null)
                {
                    ModelState.AddModelError(string.Empty,
                        "The request could not be found using the supplied request number and mobile digits.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> Status(string requestNo, string mobileLast4)
        {
            PublicRequestStatusDto status =
                await _service.GetPublicRequestStatusAsync(requestNo, mobileLast4);
            return Json(new { success = status != null, data = status }, JsonRequestBehavior.AllowGet);
        }

        private static void CopyRequestInput(
            PublicTransportRequestViewModel source,
            PublicTransportRequestViewModel target)
        {
            target.ElectionContextId = source.ElectionContextId;
            target.ServicePartyId = source.ServicePartyId;
            target.ServiceCandidateId = source.ServiceCandidateId;
            target.ServicePoolName = source.ServicePoolName;
            target.PollingStationId = source.PollingStationId;
            target.RequestedByName = source.RequestedByName;
            target.Mobile = source.Mobile;
            target.AlternateMobile = source.AlternateMobile;
            target.PickupAddress = source.PickupAddress;
            target.PickupArea = source.PickupArea;
            target.Latitude = source.Latitude;
            target.Longitude = source.Longitude;
            target.PassengerCount = source.PassengerCount;
            target.AccessibilityCategory = source.AccessibilityCategory;
            target.RequiresWheelchair = source.RequiresWheelchair;
            target.RequiresAttendant = source.RequiresAttendant;
            target.IsRoundTripRequired = source.IsRoundTripRequired;
            target.RequestedPickupLocal = source.RequestedPickupLocal;
            target.Notes = source.Notes;
            target.PrivacyConsent = source.PrivacyConsent;
        }
    }
}
