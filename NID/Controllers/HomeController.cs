using NID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NID.Controllers
{
    public class HomeController : Controller
    {
        ElectionEntities ds = new ElectionEntities();

        public ActionResult Index()
        {
            ViewData["Title"] = "Home";
            return View();
        }
        public ActionResult Result()
        {
            ViewData["Title"] = "Result";
            return View();
        }
        public ActionResult Villages()
        {
            ViewData["Title"] = "Result";
            return View();
        }
        public ActionResult Notable()
        {
            ViewData["Title"] = "Notables";
            return View();
        }
        public ActionResult Forms()
        {
            ViewData["Title"] = "Election Related Forms";
            return View();
        }
        public ActionResult Station()
        {
            ViewData["Title"] = "Polling Station Result";
            return View();
        }
        public ActionResult Polling()
        {
            ViewData["Title"] = "Polling Station";
            return View();
        }
        public ActionResult Dashboard()
        {
            ViewData["Title"] = "Dashboard";
            return View();
        }
        public ActionResult Transport()
        {
            ViewData["Title"] = "Transport";
            return View();
        }

        public JsonResult GetConstituency(String assembly)
        {
            var data = ds.get_constituency(assembly).ToList();
            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetVoters(String dated, string assembly, int constituency)
        {
            var data = ds.get_voters(null,assembly,constituency).ToList();
            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCandidates(String type, int year, string assembly, int constituency)
        {
            var data = ds.get_candidate_votes(type, year,assembly,constituency).ToList();
            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPollingStations(int? year, string election, string assembly, string seat)
        {
            var data = ds.get_polling_stations(2026,null,null,null).ToList();
            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPollingScheme(int? year, string election, string assembly, string seat, int sr)
        {
            var data = ds.get_polling_scheme(2026,null,null,null,sr).ToList();
            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

    }
}