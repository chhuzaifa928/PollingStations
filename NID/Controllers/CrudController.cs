using Dapper;
using NID.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace NID.Controllers
{
    public class CrudController : Controller
    {
        private readonly ElectionEntities db = new ElectionEntities();

        private SqlConnection GetConnection()
        {
            return new SqlConnection(db.Database.Connection.ConnectionString);
        }


        // ===================== HELPERS =====================

        private bool IsAllowedTable(SqlConnection con, string table)
        {
            return con.QueryFirst<int>(
                @"SELECT COUNT(1)
                  FROM field_metadata
                  WHERE table_name=@table",
                new { table }) > 0;
        }

        private List<field_metadata> GetFields(SqlConnection con, string table)
        {
            return con.Query<field_metadata>(
                @"SELECT *
                  FROM field_metadata
                  WHERE table_name=@table 
                  ORDER BY isnull(section_number,100), isnull(sort_order,id)",
                new { table }).ToList();
        }

        private List<string> ValidateForm(
            SqlConnection con,
            List<field_metadata> fields,
            FormCollection form,
            bool isInsert)
        {
            var errors = new List<string>();

            foreach (var f in fields)
            {
                if (isInsert && f.allow_insert != true) continue;
                if (!isInsert && f.allow_update != true) continue;

                if (f.is_computed == true || f.is_readonly == true)
                    continue;

                var value = form[f.column_name];

                if (f.is_required == true && string.IsNullOrWhiteSpace(value))
                    errors.Add($"{f.display_label} is required");

                if (f.min_length.HasValue && value?.Length < f.min_length)
                    errors.Add($"{f.display_label} is too short");

                if (f.max_length.HasValue && value?.Length > f.max_length)
                    errors.Add($"{f.display_label} is too long");

                if (!string.IsNullOrEmpty(f.regex_pattern) &&
                    !Regex.IsMatch(value ?? "", f.regex_pattern))
                    errors.Add($"{f.display_label} format is invalid");

                if (f.is_foreign_key == true && !string.IsNullOrEmpty(value))
                {
                    var fkExists = con.QueryFirst<int>(
                        $@"SELECT COUNT(1)
                           FROM [{f.dropdown_source_table}]
                           WHERE [{f.dropdown_value_column}]=@v",
                        new { v = value });

                    if (fkExists == 0)
                        errors.Add($"{f.display_label} is invalid");
                }
            }

            return errors;
        }

        private Dictionary<string, object> BuildData(
            List<field_metadata> fields,
            FormCollection form,
            bool isInsert)
        {
            return fields
                .Where(f =>
                    (isInsert ? f.allow_insert : f.allow_update) == true &&
                    f.is_computed != true &&
                    f.is_readonly != true &&
                    form.AllKeys.Contains(f.column_name))
                .ToDictionary(
                    f => f.column_name,
                    f => string.IsNullOrWhiteSpace(form[f.column_name])? (object)DBNull.Value: form[f.column_name]

                );
        }

        // ===================== INDEX =====================

        public ActionResult Index()
        {
            using (var con = GetConnection())
            {
                con.Open();

                // Get distinct table names
                var tableNames = con.Query<string>("SELECT DISTINCT table_name FROM field_metadata").ToList();

                var tableModels = new List<GenericTableModel>();

                foreach (var table in tableNames)
                {
                    // Get columns from field_metadata
                    var columns = con.Query<string>(
                        "SELECT column_name FROM field_metadata WHERE table_name = @table ORDER BY id",
                        new { table }).ToList();

                    // Get rows from the actual table
                    var rows = new List<GenericRow>();
                    try
                    {
                        var data = con.Query($"SELECT * FROM [{table}]").ToList();

                        foreach (var row in data)
                        {
                            var gr = new GenericRow();
                            foreach (var prop in (IDictionary<string, object>)row)
                            {
                                gr.Values[prop.Key] = prop.Value;
                            }
                            rows.Add(gr);
                        }
                    }
                    catch
                    {
                        // Table might not exist or no permission
                    }

                    tableModels.Add(new GenericTableModel
                    {
                        TableName = table,
                        TableDescription = "",
                        Columns = columns,
                        Rows = rows,
                        KeyColumn = columns.FirstOrDefault()
                    });
                }

                return View(tableModels);
            }
        }
        [Route("Crud/Table")]
        public ActionResult Table(string table)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(table))
                    return HttpNotFound();

                ViewBag.Table = table;
                return View();
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }


        // ===================== GET BY ID =====================

        [HttpGet]
        public JsonResult Get(string table, long id)
        {
            using (var con = GetConnection())
            {
                if (!IsAllowedTable(con, table))
                    return Json(new { error = "Table not allowed" }, JsonRequestBehavior.AllowGet);

                var sql = $@"SELECT *
                             FROM [{table}]
                             WHERE id=@id";

                var record = con.QuerySingleOrDefault(sql, new { id });

                if (record == null)
                    return Json(new { error = "Not found" }, JsonRequestBehavior.AllowGet);

                return Json(record, JsonRequestBehavior.AllowGet);
            }
        }

        // ===================== LIST =====================

        [HttpGet]
        public JsonResult List2(string table)
        {
            using (var con = GetConnection())
            {
                if (!IsAllowedTable(con, table))
                    return Json(new { error = "Table not allowed" }, JsonRequestBehavior.AllowGet);

                var sql = $@"SELECT *
                             FROM [{table}]
                             ";

                var data = con.Query(sql).ToList();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult List(string table)
        {
            using (var con = GetConnection())
            {
                // 1️⃣ Check if table is allowed
                if (!IsAllowedTable(con, table))
                    return Json(new { error = "Table not allowed" }, JsonRequestBehavior.AllowGet);

                // 2️⃣ Get metadata for the table
                var metadataSql = @"
                    SELECT *
                    FROM field_metadata
                    WHERE table_name = @Table
                ";
                var metadata = con.Query(metadataSql, new { Table = table }).ToList();

                if (!metadata.Any())
                    return Json(new { error = "No metadata found for table" }, JsonRequestBehavior.AllowGet);

                // 3️⃣ Build SELECT columns and JOINs dynamically
                var selectColumns = new List<string>();
                var joins = new List<string>();

                foreach (var field in metadata)
                {
                    if (field.is_foreign_key
                        && !string.IsNullOrEmpty(field.dropdown_source_table)
                        && !string.IsNullOrEmpty(field.dropdown_text_column)
                        && !string.IsNullOrEmpty(field.dropdown_value_column))
                    {
                        // Alias for the lookup table
                        var alias = $"{field.dropdown_source_table}_lookup";

                        // Build JOIN condition
                        var joinCondition = $"[{table}].[{field.column_name}] = {alias}.[{field.dropdown_value_column}]";

                        if (!string.IsNullOrEmpty(field.dropdown_where))
                            joinCondition += $" AND {field.dropdown_where}";

                        joins.Add($@"
                            LEFT JOIN [{field.dropdown_source_table}] {alias}
                            ON {joinCondition}
                        ");

                        // Add text column in SELECT instead of ID
                        selectColumns.Add($"{alias}.[{field.dropdown_text_column}] AS [{field.column_name}]");
                    }
                    else
                    {
                        // Regular column
                        selectColumns.Add($"[{table}].[{field.column_name}]");
                    }
                }

                // 4️⃣ Build final SQL
                var sql = $@"
            SELECT {string.Join(", ", selectColumns)}
            FROM [{table}]
            {string.Join("\n", joins)}
        ";

                // Optional: add ORDER BY if needed
                // sql += " ORDER BY ...";

                // 5️⃣ Execute and return
                var data = con.Query(sql).ToList();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }


        // ===================== INSERT =====================

        [HttpPost]
        public JsonResult Insert(string table, FormCollection form)
        {
            using (var con = GetConnection())
            {
                if (!IsAllowedTable(con, table))
                    return Json(new { error = "Table not allowed" });

                var fields = GetFields(con, table)
                    .Where(f => f.allow_insert == true).ToList();

                var errors = ValidateForm(con, fields, form, true);
                if (errors.Any())
                    return Json(new { error = true, errors });

                var data = BuildData(fields, form, true);
                if (!data.Any())
                    return Json(new { error = "No valid fields" });

                var sql = $@"
                    INSERT INTO [{table}]
                    ({string.Join(",", data.Keys.Select(k => $"[{k}]"))})
                    VALUES
                    ({string.Join(",", data.Keys.Select(k => "@" + k))})";

                con.Execute(sql, data);
                return Json(new { success = true });
            }
        }

        // ===================== UPDATE =====================

        [HttpPost]
        public JsonResult Update(string table, long id, FormCollection form)
        {
            using (var con = GetConnection())
            {
                if (!IsAllowedTable(con, table))
                    return Json(new { error = "Table not allowed" });

                var fields = GetFields(con, table)
                    .Where(f => f.allow_update == true).ToList();

                var errors = ValidateForm(con, fields, form, false);
                if (errors.Any())
                    return Json(new { error = true, errors });

                var data = BuildData(fields, form, false);
                if (!data.Any())
                    return Json(new { error = "Nothing to update" });

                data["id"] = id;

                var setClause = string.Join(",",
                    data.Keys.Where(k => k != "id")
                        .Select(k => $"[{k}]=@{k}"));

                var sql = $@"
                    UPDATE [{table}]
                    SET {setClause}
                    WHERE id=@id";

                con.Execute(sql, data);
                return Json(new { success = true });
            }
        }

        // ===================== DELETE (SOFT) =====================

        [HttpPost]
        public JsonResult Delete(string table, long id)
        {
            using (var con = GetConnection())
            {
                if (!IsAllowedTable(con, table))
                    return Json(new { error = "Table not allowed" });

                var sql = $@"
                    UPDATE [{table}]
                    SET is_deleted=1
                    WHERE id=@id";
                try
                {
                    con.Execute(sql, new { id });
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    sql = $@"Delete from [{table}]
                    WHERE id=@id";
                    con.Execute(sql, new { id });
                    return Json(new { success = true });
                }
                
            }
        }
    }
}
