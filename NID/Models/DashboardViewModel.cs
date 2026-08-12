using NID.Models;
using System.Collections.Generic;

namespace NID.Models
{
    public class DashboardViewModel
    {
        public int TotalVehicles { get; set; }

        public int ActiveVehicles { get; set; }

        public int TotalPollingStations { get; set; }

        public int TotalIncidents { get; set; }

        public int CompletedTrips { get; set; }

        public int TotalPickedVoters { get; set; }

        public List<VehicleModel> Vehicles { get; set; }

        public List<PollingStationModel> PollingStations { get; set; }

        public List<IncidentModel> Incidents { get; set; }

        public List<RouteModel> Routes { get; set; }

        public List<DriverModel> Drivers { get; set; }
    }
}
