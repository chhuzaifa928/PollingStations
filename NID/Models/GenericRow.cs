using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NID.Models
{
    public class GenericRow
    {
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
    }

    public class GenericTableModel
    {
        public string TableName { get; set; }
        public string TableDescription { get; set; }
        public List<string> Columns { get; set; }
        public List<GenericRow> Rows { get; set; }
        public string KeyColumn { get; set; }
    }
    public class GenericTableStats
    {
        public string TableName { get; set; }
        public string TableDescription { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
    }

}