using NID.Models;
using Dapper;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity.Core.Common;
using System.Collections.Generic;
using System.Data;
using System.Web.UI.WebControls;

namespace NID.Controllers
{
    public class MetadataController : Controller
    {
        private readonly ElectionEntities db = new ElectionEntities();

        [HttpGet]
        public JsonResult Get(string table)
        {
            if (string.IsNullOrWhiteSpace(table))
            {
                return Json(new
                {
                    error = true,
                    message = "Table name is required",
                    table = table,
                    data = new List<field_metadata>()
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                using (var con = new SqlConnection(db.Database.Connection.ConnectionString))
                {
                    var sql = @"SELECT *
                    FROM field_metadata
                    WHERE table_name = @table
                    AND ISNULL(is_deleted, 0) = 0
                    ORDER BY ISNULL(section_number, 100),
                    ISNULL(sort_order, id)";

                    var data = con.Query<field_metadata>(sql, new { table }).ToList();

                    return Json(new
                    {
                        error = false,
                        message = "Successful",
                        table = table,
                        data = data
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = true,
                    message = ex.Message,
                    table = table,
                    data = new List<field_metadata>()
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetAllTables()
        {
            var tables = new List<string>();

            try
            {
                using (var con = db.Database.Connection)
                {
                    if (con.State != ConnectionState.Open)
                        con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT TABLE_NAME
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT LIKE 'zeleted%'
                        ORDER BY TABLE_NAME";

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                tables.Add(reader.GetString(0));
                        }
                    }
                }

                return Json(new { error = false, message= "Successful", data = tables }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: "+ex.Message, data = tables }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpGet]
        public JsonResult GetTableColumns(string table)
        {

            var columns = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(table))
                {
                    return Json(new { error = true, message = "Table name is required", data = columns }, JsonRequestBehavior.AllowGet);
                }
                using (var con = db.Database.Connection)
                {
                    if (con.State != ConnectionState.Open)
                        con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT COLUMN_NAME
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_NAME = @tableName
                        ORDER BY ORDINAL_POSITION";

                        var p = cmd.CreateParameter();
                        p.ParameterName = "@tableName";
                        p.Value = table;
                        cmd.Parameters.Add(p);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                columns.Add(reader.GetString(0));
                        }
                    }
                }
                return Json(new { error = false, message = "Successful", data = columns }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error: "+ex.Message, data = columns }, JsonRequestBehavior.AllowGet);
            }


        }


        

    }
}
