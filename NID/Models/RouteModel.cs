namespace NID.Models
{
    public class RouteModel
    {
        public int RouteId { get; set; }

        public string RouteName { get; set; }

        public string VehicleNo { get; set; }

        public string DriverName { get; set; }

        public string StartLocation { get; set; }

        public string EndLocation { get; set; }

        public int TotalTrips { get; set; }

        public string Status { get; set; }
    }
}
