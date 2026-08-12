using NID.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Common;
using System.IO;

namespace NID.Controllers
{
    public class GenericController : Controller
    {
        // Use your EF context
        ElectionEntities db = new ElectionEntities();


        // Load generic CRUD page
        public ActionResult Index(string table)
        {
            return View((object)table);
        }

        public JsonResult LoadTable(string table)
        {
            using (var con = db.Database.Connection)
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                // --- 1. Read table description ---
                string tableDescription = null;
                using (var cmdDesc = con.CreateCommand())
                {
                    cmdDesc.CommandText = @"SELECT ep.value AS TableDescription
                    FROM sys.tables t LEFT JOIN sys.extended_properties ep
                    ON ep.major_id = t.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
                    WHERE t.name = @tableName";

                    var param = cmdDesc.CreateParameter();
                    param.ParameterName = "@tableName";
                    param.Value = table;
                    cmdDesc.Parameters.Add(param);

                    tableDescription = cmdDesc.ExecuteScalar()?.ToString();
                }

                // --- 2. Read DB schema ---
                DataTable schema = con.GetSchema("Columns", new[] { null, null, table, null });
                var columns = schema.AsEnumerable()
                    .Select(r => r["COLUMN_NAME"].ToString())
                    .ToList();

                // --- 3. Get primary key ---
                string keyColumn = GetPrimaryKey(con, table);

                // --- 4. Load rows ---
                var cmd = con.CreateCommand();
                cmd.CommandText = $"SELECT * FROM [{table}]";

                var reader = cmd.ExecuteReader();

                List<GenericRow> rows = new List<GenericRow>();

                while (reader.Read())
                {
                    var row = new GenericRow();
                    foreach (var col in columns)
                        row.Values[col] = reader[col];

                    rows.Add(row);
                }
                reader.Close();

                // --- 5. Return JSON including table description ---
                return Json(new GenericTableModel
                {
                    TableName = table,
                    TableDescription = tableDescription,  // <-- added this
                    Columns = columns,
                    Rows = rows,
                    KeyColumn = keyColumn
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Load table (rows + schema)
        public JsonResult LoadTable1(string table)
        {
            using (var con = db.Database.Connection)
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                // Read DB schema
                DataTable schema = con.GetSchema("Columns", new[] { null, null, table, null });
                var columns = schema.AsEnumerable()
                    .Select(r => r["COLUMN_NAME"].ToString())
                    .ToList();

                // Get primary key
                string keyColumn = GetPrimaryKey(con, table);

                // Load rows
                var cmd = con.CreateCommand();
                cmd.CommandText = $"SELECT * FROM [{table}]";

                var reader = cmd.ExecuteReader();

                List<GenericRow> rows = new List<GenericRow>();

                while (reader.Read())
                {
                    var row = new GenericRow();
                    foreach (var col in columns)
                        row.Values[col] = reader[col];

                    rows.Add(row);
                }
                reader.Close();

                return Json(new GenericTableModel
                {
                    TableName = table,
                    Columns = columns,
                    Rows = rows,
                    KeyColumn = keyColumn
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public JsonResult Save()
        {
            // Read JSON from request body
            Request.InputStream.Position = 0;
            var json = new StreamReader(Request.InputStream).ReadToEnd();

            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericSaveRequest>(json);

            string table = model.table;
            GenericRow row = model.row;
            string key = model.key;

            // Use EF connection
            using (var con = db.Database.Connection)
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                // Case-insensitive key column match
                var keyCol = row.Values.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

                // Determine if it's an update (key exists and has value)
                bool isUpdate = keyCol != null &&
                                row.Values[keyCol] != null &&
                                !string.IsNullOrEmpty(row.Values[keyCol].ToString());

                try
                {
                    if (isUpdate)
                    {
                        // UPDATE logic
                        string setClause = string.Join(",",
                            row.Values
                                .Where(x => !string.Equals(x.Key, keyCol, StringComparison.OrdinalIgnoreCase))
                                .Select(x => $"[{x.Key}]=@{x.Key}"));

                        using (var cmd = con.CreateCommand())
                        {
                            cmd.CommandText = $"UPDATE [{table}] SET {setClause} WHERE [{keyCol}]=@{keyCol}";
                            foreach (var p in row.Values)
                            {
                                var param = cmd.CreateParameter();
                                param.ParameterName = "@" + p.Key;
                                param.Value = p.Value ?? DBNull.Value;
                                cmd.Parameters.Add(param);
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // INSERT logic: skip identity PK
                        var insertCols = row.Values
                            .Where(x => !string.Equals(x.Key, keyCol, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        string cols = string.Join(",", insertCols.Select(c => $"[{c.Key}]"));
                        string vals = string.Join(",", insertCols.Select(c => "@" + c.Key));

                        using (var cmd = con.CreateCommand())
                        {
                            cmd.CommandText = $"INSERT INTO [{table}] ({cols}) VALUES ({vals})";

                            foreach (var p in insertCols)
                            {
                                var param = cmd.CreateParameter();
                                param.ParameterName = "@" + p.Key;
                                param.Value = p.Value ?? DBNull.Value;
                                cmd.Parameters.Add(param);
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SQL ERROR: " + ex.Message);
                    return Json("Error: " + ex.Message+"\n"+ex.ToString());
                }
            }

            return Json("Success");
        }



        [HttpPost]
        public JsonResult Delete()
        {
            // Read JSON from request body
            Request.InputStream.Position = 0;
            var json = new StreamReader(Request.InputStream).ReadToEnd();

            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericDeleteRequest>(json);

            string table = model.table;
            string key = model.key;
            string id = model.id;

            try
            {
                using (var con = db.Database.Connection)
                {
                    if (con.State != System.Data.ConnectionState.Open)
                        con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = $"DELETE FROM [{table}] WHERE [{key}]=@id";
                        var param = cmd.CreateParameter();
                        param.ParameterName = "@id";
                        param.Value = id;
                        cmd.Parameters.Add(param);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json("Deleted");
            }
            catch (Exception ex)
            {
                return Json("Error: " + ex.Message);
            }
        }


        // Extract primary key
        private string GetPrimaryKey(DbConnection con, string table)
        {
            DataTable schema = con.GetSchema("IndexColumns", new[] { null, null, null, null, table });

            foreach (DataRow row in schema.Rows)
            {
                if (row["PRIMARY_KEY"].ToString() == "True")
                    return row["COLUMN_NAME"].ToString();
            }

            return "id"; // fallback
        }


        // parameter helper
        private void AddParam(DbCommand cmd, string key, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = "@" + key;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }
    }

}