using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NID.Models
{
    public class GenericDeleteRequest
    {
        public string table { get; set; }
        public string key { get; set; }
        public string id { get; set; }
    }
}