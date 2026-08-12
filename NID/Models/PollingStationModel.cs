namespace NID.Models
{
    public class PollingStationModel
    {
        public int PollingStationId { get; set; }

        public string StationCode { get; set; }

        public string StationName { get; set; }

        public string Constituency { get; set; }

        public int ExpectedVoters { get; set; }

        public int PickedVoters { get; set; }

        public int WaitingVoters { get; set; }

        public string Status { get; set; }
    }
}
