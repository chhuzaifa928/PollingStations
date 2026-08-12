using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;
using NID.Areas.ElectionTransport.Services;

namespace NID.Areas.ElectionTransport.Controllers
{
    [AllowAnonymous]
    public class TransportIntegrationController : Controller
    {
        private readonly ITransportService _service;

        public TransportIntegrationController()
            : this(new SqlTransportService())
        {
        }

        public TransportIntegrationController(ITransportService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<JsonResult> PushLocation(LocationPushInputModel model)
        {
            if (!IsApiAuthorized())
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return Json(new { success = false, message = "Invalid integration API key." });
            }

            if (!ModelState.IsValid)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, message = "Invalid location payload." });
            }

            try
            {
                LocationPushResultDto result = await _service.RecordLocationByVehicleCodeAsync(model);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult Health()
        {
            return Json(new
            {
                success = true,
                service = "Election Day Transport Integration",
                serverTimeUtc = DateTime.UtcNow
            }, JsonRequestBehavior.AllowGet);
        }

        private bool IsApiAuthorized()
        {
            string expected = TransportModuleOptions.IntegrationApiKey;
            string supplied = Request.Headers["X-Transport-Api-Key"];
            return !string.IsNullOrWhiteSpace(expected)
                && TransportSecurity.FixedTimeEquals(expected, supplied);
        }
    }
}
