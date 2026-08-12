using System;

namespace NID.Models
{
    public class IncidentModel
    {
        public int IncidentId { get; set; }

        public string PollingStation { get; set; }

        public string IncidentType { get; set; }

        public string Severity { get; set; }

        public string Description { get; set; }

        public string ReportedBy { get; set; }

        public DateTime ReportedTime { get; set; }

        public string Status { get; set; }
    }
}
