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
using System.Data.Entity.Core.Common;
using System.Text.RegularExpressions;
using System.Text;
using System.Reflection;
using Dapper;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace NID.Controllers
{
    // Request model for creating a table
    public class TableCreateRequest
    {
        public string TableName { get; set; }
        public string TableDescription { get; set; }
        public List<field_metadata> Columns { get; set; }
    }

    // Request model for updating a column
    public class UpdateColumnRequest
    {
        public List<field_metadata> Column { get; set; }
        public string TableName { get; set; }
    }

    
    public class SuperController : Controller
    {
        // Use your EF context
        ElectionEntities db = new ElectionEntities();
        private SqlConnection GetConnection()
        {
            return new SqlConnection(db.Database.Connection.ConnectionString);
        }

        public ActionResult Index()
        {
            using (var con = GetConnection())
            {
                con.Open();

                // Get distinct table names
                var tableNames = con.Query<string>("SELECT DISTINCT table_name FROM field_metadata WHERE ISNULL(is_deleted,0) = 0").ToList();

                var tableStats = new List<GenericTableStats>();

                foreach (var table in tableNames)
                {
                    // Get column count
                    int columnCount = con.Query<int>("SELECT COUNT(*) FROM field_metadata WHERE table_name = @table AND ISNULL(is_deleted,0) = 0",
                        new { table }
                    ).FirstOrDefault();

                    // Get row count (safely, ignore if table doesn't exist)
                    int rowCount = 0;
                    try
                    {
                        rowCount = con.Query<int>($"SELECT COUNT(*) FROM [{table}]").FirstOrDefault();
                    }
                    catch
                    {
                        rowCount = 0;
                    }

                    string desc = con.Query<string>(@"
                    SELECT CAST(value AS NVARCHAR(MAX)) 
                    FROM sys.extended_properties ep
                    INNER JOIN sys.tables t ON t.object_id = ep.major_id
                    WHERE ep.name = 'MS_Description' AND t.name = @table
                    ", new { table }).FirstOrDefault() ?? "";

                    tableStats.Add(
                        new GenericTableStats 
                        { TableName = table, 
                            TableDescription = desc,
                            Columns = columnCount,
                            Rows = rowCount
                        });
                }
                return View(tableStats);
            }
        }

        // Load generic CRUD page
        public ActionResult Create()
        {
            return View((object)"field_metadata");
        }
        public ActionResult Alter(string table)
        {
            ViewBag.table = table;
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

            using (var con = db.Database.Connection)
            {
                if (con.State != ConnectionState.Open)
                    con.Open();

                // Case-insensitive key column match
                var keyCol = row.Values.Keys
                    .FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

                bool isUpdate =
                    keyCol != null &&
                    row.Values.ContainsKey(keyCol) &&
                    row.Values[keyCol] != null &&
                    !string.IsNullOrWhiteSpace(row.Values[keyCol].ToString());

                try
                {
                    if (isUpdate)
                    {
                        // ---------- UPDATE ----------
                        string setClause = string.Join(",",
                            row.Values
                                .Where(x => !string.Equals(x.Key, keyCol, StringComparison.OrdinalIgnoreCase))
                                .Select(x => $"[{x.Key}] = @{x.Key}"));

                        using (var cmd = con.CreateCommand())
                        {
                            cmd.CommandText = $"UPDATE [{table}] SET {setClause} WHERE [{keyCol}] = @{keyCol}";

                            foreach (var p in row.Values)
                            {
                                var param = cmd.CreateParameter();
                                param.ParameterName = "@" + p.Key;
                                param.Value = NormalizeValue(p.Value);
                                cmd.Parameters.Add(param);
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // ---------- INSERT ----------
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
                                param.Value = NormalizeValue(p.Value);
                                cmd.Parameters.Add(param);
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SQL ERROR: " + ex.Message);
                    return Json("Error: " + ex.Message);
                }
            }

            return Json("Success");
        }


        private static object NormalizeValue(object value)
        {
            if (value == null)
                return DBNull.Value;

            var str = value.ToString();

            if (string.IsNullOrWhiteSpace(str))
                return DBNull.Value;

            return value;
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

        [HttpPost]
        public JsonResult CreateTable(TableCreateRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TableName) || request.Columns == null || !request.Columns.Any())
                return Json("Invalid request data");

            // Validate table and column names
            if (!Regex.IsMatch(request.TableName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                return Json("Invalid table name");

            foreach (var col in request.Columns)
                if (!Regex.IsMatch(col.column_name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                    return Json($"Invalid column name: {col.column_name}");

            // 1️⃣ Build CREATE TABLE SQL
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE [{request.TableName}] (");

            foreach (var col in request.Columns)
            {
                sb.Append($"[{col.column_name}] {col.sql_type}");

                if (col.max_length.HasValue && col.sql_type.ToLower().Contains("char"))
                    sb.Append($"({col.max_length})");

                if (col.decimal_places.HasValue && col.sql_type.ToLower() == "decimal")
                    sb.Append($"({col.max_length ?? 18}, {col.decimal_places})");

                sb.Append(col.is_required == true ? " NOT NULL" : " NULL");

                if (!string.IsNullOrWhiteSpace(col.default_value))
                    sb.Append($" DEFAULT '{col.default_value}'");

                sb.Append(", ");
            }

            sb.Length -= 2;
            sb.Append(")");

            try
            {
                db.Database.ExecuteSqlCommand(sb.ToString()); // EF handles connection automatically
            }
            catch (Exception ex)
            {
                return Json("Error creating table: " + ex.Message);
            }
            
            try
            {
                if (!string.IsNullOrWhiteSpace(request.TableDescription))
                {
                    var addDescSql = @"
                    EXEC sp_addextendedproperty 
                        @name = N'MS_Description', 
                        @value = @desc, 
                        @level0type = N'SCHEMA', @level0name = N'dbo', 
                        @level1type = N'TABLE', @level1name = @table;
                    ";

                    db.Database.ExecuteSqlCommand(
                        addDescSql,
                        new SqlParameter("@table", request.TableName),
                        new SqlParameter("@desc", request.TableDescription)
                    );
                }

            }
            catch (Exception ex)
            {
                return Json("Error Specifying table: " + ex.Message);
            }

            foreach (var col in request.Columns)
            {
                var metadata = new field_metadata();
                var metaProps = typeof(field_metadata).GetProperties();

                foreach (var prop in metaProps)
                {
                    if (prop.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var colProp = col.GetType().GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    //if (colProp == null) continue;

                    var rawValue = colProp.GetValue(col);
                    if (rawValue == null) continue;

                    try
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var strVal = rawValue.ToString().Trim();

                        if (targetType == typeof(bool))
                        {
                            bool? boolValue = null;

                            if (rawValue is bool b)
                            {
                                boolValue = b;
                            }
                            else if (strVal == "1")
                            {
                                boolValue = true;
                            }
                            else if (strVal == "0")
                            {
                                boolValue = false;
                            }
                            else if (strVal.Equals("true", StringComparison.OrdinalIgnoreCase))
                            {
                                boolValue = true;
                            }
                            else if (strVal.Equals("false", StringComparison.OrdinalIgnoreCase))
                            {
                                boolValue = false;
                            }

                            if (boolValue.HasValue)
                            {
                                prop.SetValue(metadata, boolValue.Value);
                            }
                            // else: leave unset → DB default applies
                        }
                        else
                        {
                            object safeValue = Convert.ChangeType(rawValue, targetType);
                            prop.SetValue(metadata, safeValue);
                        }
                    }
                    catch
                    {
                        // Ignore conversion errors
                    }
                }

                metadata.table_name = request.TableName;
                metadata.created_by = User.Identity.Name;
                metadata.created_at = DateTime.Now;

                db.field_metadata.Add(metadata);
            }

            
            db.SaveChanges();

            return Json("Table created and metadata saved successfully ");
        }

        /*
        [HttpPost]
        public JsonResult UpdateColumn()
        {
            // Read JSON from request body
            Request.InputStream.Position = 0;
            var json = new StreamReader(Request.InputStream).ReadToEnd();

            // Deserialize JSON
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateColumnRequest>(json);
            var column = request.Column;
            var tableName = request.TableName;

            if (string.IsNullOrWhiteSpace(tableName) || column == null || string.IsNullOrWhiteSpace(column.column_name))
                return Json("Table and column names are required");

            try
            {
                using (var con = db.Database.Connection)
                {
                    if (con.State != ConnectionState.Open)
                        con.Open();

                    var sb = new System.Text.StringBuilder();
                    sb.Append($"ALTER TABLE [{tableName}] ALTER COLUMN [{column.column_name}] {column.sql_type}");

                    if (!string.IsNullOrWhiteSpace(column.max_length) &&
                        column.sql_type.ToLower().Contains("char"))
                    {
                        sb.Append($"({column.max_length})");
                    }

                    if (!string.IsNullOrWhiteSpace(column.decimal_places) && column.sql_type.ToLower() == "decimal")
                    {
                        sb.Append($"({column.max_length ?? "18"}, {column.decimal_places})");
                    }

                    sb.Append(column.is_required == "1" ? " NOT NULL" : " NULL");

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = sb.ToString();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error updating column: " + ex.Message);
            }

            return Json("Column updated successfully");
        }
        */

        

       
        [HttpPost]
        public JsonResult RenameTable(string oldtable, string table)
        {
            if (string.IsNullOrWhiteSpace(oldtable) || string.IsNullOrWhiteSpace(table))
                return Json(new { error = true, message = "Table names are required" });

            // Validate table names (prevent SQL injection)
            if (!Regex.IsMatch(oldtable, @"^[a-zA-Z_][a-zA-Z0-9_]*$") ||
                !Regex.IsMatch(table, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                return Json(new { error = true, message = "Invalid table name" });
            try
            {
                using (var con = db.Database.Connection)
                {
                    if (con.State != ConnectionState.Open)
                        con.Open();

                    using (var cmd = con.CreateCommand())
                    {
                        cmd.CommandText = $"EXEC sp_rename @oldName, @newName";
                        cmd.Parameters.Add(new SqlParameter("@oldName", oldtable));
                        cmd.Parameters.Add(new SqlParameter("@newName", table));
                        cmd.ExecuteNonQuery();
                    }

                    // Update metadata table if needed
                    db.Database.ExecuteSqlCommand(
                        "UPDATE field_metadata SET table_name = @newName WHERE table_name = @oldName",
                        new SqlParameter("@newName", table),
                        new SqlParameter("@oldName", oldtable)
                    );
                    // Update metadata forieng key table if needed
                    db.Database.ExecuteSqlCommand(
                        "UPDATE field_metadata SET dropdown_source_table = @newName WHERE dropdown_source_table = @oldName",
                        new SqlParameter("@newName", table),
                        new SqlParameter("@oldName", oldtable)
                    );
                }
                return Json(new { error = false, message = "Table renamed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error renaming table: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DropTable(string table)
        {
            if (string.IsNullOrWhiteSpace(table))
                return Json(new { error = true, message = "Table name is required" });

            // Validate table name
            if (!Regex.IsMatch(table, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                return Json(new { error = true, message = "Invalid table name" });

            try
            {
                using (var con = db.Database.Connection)
                {
                    if (con.State != ConnectionState.Open)
                        con.Open();

                    // Step 0: Check if table is referenced as dropdown_source_table in active metadata
                    string checkSql = @"SELECT COUNT(1) 
                    FROM field_metadata 
                    WHERE dropdown_source_table = @table 
                    AND ISNULL(is_deleted, 0) = 0";

                    using (var cmdCheck = con.CreateCommand())
                    {
                        cmdCheck.CommandText = checkSql;
                        cmdCheck.Parameters.Add(new SqlParameter("@table", table));

                        int refCount = (int)cmdCheck.ExecuteScalar();
                        if (refCount > 0)
                            return Json(new { error = true, message = $"Cannot drop '{table}' because it is referenced in dropdown_source_table." });
                    }

                    // Step 1: Mark metadata as deleted
                    db.Database.ExecuteSqlCommand(
                        "UPDATE field_metadata SET is_deleted = 1 WHERE table_name = @table",
                        new SqlParameter("@table", table)
                    );

                    // Step 2: Backup table with timestamp
                    string backupTableName = $"zeleted_{table}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    using (var cmdBackup = con.CreateCommand())
                    {
                        cmdBackup.CommandText = $"SELECT * INTO [{backupTableName}] FROM [{table}]";
                        cmdBackup.ExecuteNonQuery();
                    }

                    // Step 3: Drop original table
                    using (var cmdDrop = con.CreateCommand())
                    {
                        cmdDrop.CommandText = $"DROP TABLE [{table}]";
                        cmdDrop.ExecuteNonQuery();
                    }

                    return Json(new { error = false, message = $"Table '{table}' backed up as '{backupTableName}' and dropped successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = "Error dropping table: " + ex.Message });
            }
        }


    }

}