using Dapper;
using NID.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace NID.Controllers
{
    public class LookupItem
    {
        public string value { get; set; }
        public string text { get; set; }
    }
    public class LookupController : Controller
    {
        private readonly ElectionEntities db = new ElectionEntities();


        [HttpGet]
        public JsonResult Get(string table, string valueCol, string textCol, string where, string order)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(valueCol) )
                {
                    return Json(new { error = "Invalid parameters" },
                        JsonRequestBehavior.AllowGet);
                }

                var connectionString = db.Database.Connection.ConnectionString;

                using (var con = new SqlConnection(connectionString))
                {
                    if(textCol==null || textCol == "")
                    {
                        textCol = valueCol;
                    }

                    string wh = "";
                    if(where!=null && where != "")
                    {
                        wh = "WHERE " + where;
                    }
                    string or = "";
                    if(order!=null && order != "")
                    {
                        or = "ORDER BY " + order;
                    }




                    // ⚠️ Column names cannot be parameterized – validate them
                    var sql = $@"
                        SELECT 
                            {valueCol} AS value,
                            {textCol} AS text
                        FROM {table}
                        "+wh+" "+or;

                    var data = con.Query<LookupItem>(sql).ToList();

                    return Json(data, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
