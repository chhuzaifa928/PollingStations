namespace NID.Models
{
    public class VehicleModel
    {
        public int VehicleId { get; set; }

        public string VehicleNo { get; set; }

        public string DriverName { get; set; }

        public string RouteName { get; set; }

        public string PollingStation { get; set; }

        public int Capacity { get; set; }

        public int Passengers { get; set; }

        public int CompletedTrips { get; set; }

        public string Status { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }
    }
}
