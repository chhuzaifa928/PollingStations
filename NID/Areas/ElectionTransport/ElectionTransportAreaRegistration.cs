using System.Web.Mvc;

namespace NID.Areas.ElectionTransport
{
    public class ElectionTransportAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "ElectionTransport"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "ElectionTransport_Public",
                "ElectionTransport/Public/{action}/{id}",
                new { controller = "PublicTransport", action = "Request", id = UrlParameter.Optional },
                new[] { "NID.Areas.ElectionTransport.Controllers" });

            context.MapRoute(
                "ElectionTransport_Integration",
                "ElectionTransport/Integration/{action}/{id}",
                new { controller = "TransportIntegration", action = "PushLocation", id = UrlParameter.Optional },
                new[] { "NID.Areas.ElectionTransport.Controllers" });

            context.MapRoute(
                "ElectionTransport_Api",
                "ElectionTransport/Api/{action}/{id}",
                new { controller = "TransportApi", action = "Dashboard", id = UrlParameter.Optional },
                new[] { "NID.Areas.ElectionTransport.Controllers" });

            context.MapRoute(
                "ElectionTransport_Default",
                "ElectionTransport/{action}/{id}",
                new { controller = "CommandCentre", action = "Index", id = UrlParameter.Optional },
                new[] { "NID.Areas.ElectionTransport.Controllers" });
        }
    }
}
