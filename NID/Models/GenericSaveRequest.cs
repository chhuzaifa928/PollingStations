using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NID.Models
{
    public class GenericSaveRequest
    {
        public string table { get; set; }
        public GenericRow row { get; set; }
        public string key { get; set; }
    }

}