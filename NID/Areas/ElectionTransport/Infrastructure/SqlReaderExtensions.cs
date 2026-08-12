using System;
using System.Data.SqlClient;

namespace NID.Areas.ElectionTransport.Infrastructure
{
    internal static class SqlReaderExtensions
    {
        public static string GetStringSafe(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public static int GetInt32Safe(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        public static long GetInt64Safe(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
        }

        public static long? GetNullableInt64(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (long?)null : Convert.ToInt64(reader.GetValue(ordinal));
        }

        public static int? GetNullableInt32(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        public static double? GetNullableDouble(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (double?)null : Convert.ToDouble(reader.GetValue(ordinal));
        }

        public static decimal GetDecimalSafe(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0M : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        public static decimal? GetNullableDecimal(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        public static bool GetBooleanSafe(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
        }

        public static DateTime? GetNullableDateTime(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }

        public static Guid GetGuidSafe(this SqlDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? Guid.Empty : (Guid)reader.GetValue(ordinal);
        }
    }
}
