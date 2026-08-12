using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace NID.Areas.ElectionTransport.Models
{
    public class TransportPageViewModel
    {
        public TransportPageViewModel()
        {
            Contexts = new List<ElectionContextDto>();
        }

        public string PageTitle { get; set; }
        public string ActivePage { get; set; }
        public long ElectionContextId { get; set; }
        public ElectionContextDto SelectedContext { get; set; }
        public IList<ElectionContextDto> Contexts { get; set; }
        public int RefreshSeconds { get; set; }
        public int DemoTickSeconds { get; set; }
        public int TrailMinutes { get; set; }
        public bool AllowDemoAdministration { get; set; }
    }

    public class VehicleMapFilterModel
    {
        public long ElectionContextId { get; set; }
        public string Status { get; set; }
        public string VehicleTypeCode { get; set; }
        public long? ProviderId { get; set; }
        public long? PollingStationId { get; set; }
        public string Search { get; set; }
    }

    public class VehicleManageViewModel
    {
        public VehicleManageViewModel()
        {
            VehicleTypes = new List<SelectListItem>();
            PollingStations = new List<SelectListItem>();
            Providers = new List<SelectListItem>();
            Candidates = new List<SelectListItem>();
            Parties = new List<SelectListItem>();
        }

        public long? VehicleAssignmentId { get; set; }

        [Required]
        public long ElectionContextId { get; set; }

        [Required]
        [Display(Name = "Vehicle type")]
        public short VehicleTypeId { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Registration number")]
        public string RegistrationNo { get; set; }

        [StringLength(200)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [StringLength(100)]
        public string Make { get; set; }

        [StringLength(100)]
        public string Model { get; set; }

        [Range(1950, 2200)]
        [Display(Name = "Model year")]
        public short? ModelYear { get; set; }

        [StringLength(100)]
        public string Color { get; set; }

        [Range(1, 100)]
        [Display(Name = "Seating capacity")]
        public short SeatingCapacity { get; set; }

        [StringLength(250)]
        [Display(Name = "Owner name")]
        public string OwnerName { get; set; }

        [StringLength(30)]
        [Display(Name = "Owner mobile")]
        public string OwnerMobile { get; set; }

        [Required, StringLength(250)]
        [Display(Name = "Driver name")]
        public string DriverName { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Driver mobile")]
        public string DriverMobile { get; set; }

        [StringLength(1000)]
        [Display(Name = "Driver address")]
        public string DriverAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "Driving licence number")]
        public string DrivingLicenseNo { get; set; }

        [Display(Name = "Provider / influencer")]
        public long? ProviderId { get; set; }

        [Display(Name = "Candidate")]
        public long? CandidateId { get; set; }

        [Display(Name = "Party")]
        public long? PartyId { get; set; }

        [Display(Name = "Assigned polling station")]
        public long? AssignedPollingStationId { get; set; }

        [Required]
        [Display(Name = "Assignment status")]
        public string AssignmentStatus { get; set; }

        [Range(0.5, 100)]
        [Display(Name = "Maximum service radius (km)")]
        public decimal? MaxServiceRadiusKm { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; }

        public bool IsActive { get; set; }

        public IList<SelectListItem> VehicleTypes { get; set; }
        public IList<SelectListItem> PollingStations { get; set; }
        public IList<SelectListItem> Providers { get; set; }
        public IList<SelectListItem> Candidates { get; set; }
        public IList<SelectListItem> Parties { get; set; }
    }

    public class ProviderManageViewModel
    {
        public ProviderManageViewModel()
        {
            Candidates = new List<SelectListItem>();
            Parties = new List<SelectListItem>();
            PollingStations = new List<SelectListItem>();
            VehicleTypes = new List<SelectListItem>();
        }

        public long? ProviderId { get; set; }

        [Required]
        public long ElectionContextId { get; set; }

        [Required, StringLength(250)]
        [Display(Name = "Provider / influencer name")]
        public string ProviderName { get; set; }

        [Required]
        [Display(Name = "Provider type")]
        public string ProviderType { get; set; }

        [StringLength(30)]
        public string Mobile { get; set; }

        [StringLength(30)]
        [Display(Name = "Alternate mobile")]
        public string AlternateMobile { get; set; }

        [StringLength(1000)]
        public string Address { get; set; }

        [StringLength(300)]
        public string Area { get; set; }

        public long? CandidateId { get; set; }
        public long? PartyId { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Promised vehicles")]
        public int PromisedQuantity { get; set; }

        [Display(Name = "Commitment polling station")]
        public long? CommitmentPollingStationId { get; set; }

        [Display(Name = "Promised vehicle type")]
        public short? CommitmentVehicleTypeId { get; set; }

        [StringLength(1000)]
        public string Remarks { get; set; }

        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }

        public IList<SelectListItem> Candidates { get; set; }
        public IList<SelectListItem> Parties { get; set; }
        public IList<SelectListItem> PollingStations { get; set; }
        public IList<SelectListItem> VehicleTypes { get; set; }
    }

    public class PublicTransportRequestViewModel
    {
        public PublicTransportRequestViewModel()
        {
            PollingStations = new List<SelectListItem>();
        }

        [Required]
        public long ElectionContextId { get; set; }

        public long? ServicePartyId { get; set; }
        public long? ServiceCandidateId { get; set; }
        public string ServicePoolName { get; set; }

        [Display(Name = "Polling station")]
        public long? PollingStationId { get; set; }

        [Required, StringLength(250)]
        [Display(Name = "Requestor name")]
        public string RequestedByName { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Mobile number")]
        public string Mobile { get; set; }

        [StringLength(30)]
        [Display(Name = "Alternate mobile")]
        public string AlternateMobile { get; set; }

        [Required, StringLength(1000)]
        [Display(Name = "Pickup address")]
        public string PickupAddress { get; set; }

        [StringLength(300)]
        [Display(Name = "Area / locality")]
        public string PickupArea { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Range(1, 20)]
        [Display(Name = "Passengers")]
        public short PassengerCount { get; set; }

        [Display(Name = "Assistance category")]
        public string AccessibilityCategory { get; set; }

        [Display(Name = "Wheelchair space required")]
        public bool RequiresWheelchair { get; set; }

        [Display(Name = "Attendant required")]
        public bool RequiresAttendant { get; set; }

        [Display(Name = "Return transport also required")]
        public bool IsRoundTripRequired { get; set; }

        [Display(Name = "Preferred pickup time")]
        public DateTime? RequestedPickupLocal { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Privacy consent is required.")]
        [Display(Name = "I consent to the use of these details only for election-day transport coordination.")]
        public bool PrivacyConsent { get; set; }

        public IList<SelectListItem> PollingStations { get; set; }
    }

    public class PublicRequestConfirmationViewModel
    {
        public string RequestNo { get; set; }
        public string RequestStatus { get; set; }
        public string PollingStationName { get; set; }
        public string Message { get; set; }
    }

    public class PublicRequestTrackViewModel
    {
        [Required, StringLength(30)]
        [Display(Name = "Request number")]
        public string RequestNo { get; set; }

        [Required, StringLength(4, MinimumLength = 4)]
        [Display(Name = "Last four digits of mobile")]
        public string MobileLast4 { get; set; }

        public PublicRequestStatusDto Status { get; set; }
    }

    public class LocationPushInputModel
    {
        [Required]
        public Guid VehicleAppCode { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        public DateTime? RecordedAtUtc { get; set; }

        [Range(0, 250)]
        public decimal? SpeedKph { get; set; }

        [Range(0, 360)]
        public decimal? HeadingDegrees { get; set; }

        [Range(0, 10000)]
        public decimal? AccuracyMeters { get; set; }

        [Range(0, 100)]
        public byte? BatteryPercent { get; set; }

        [StringLength(30)]
        public string NetworkType { get; set; }

        public bool IsMockLocation { get; set; }
    }

    public class RouteRequestInputModel
    {
        [Required]
        public long TransportRequestId { get; set; }

        [Range(1, 20)]
        public int OfferCount { get; set; }
    }

    public class DispatchAcceptInputModel
    {
        [Required]
        public long RequestDispatchId { get; set; }
    }

    public class RequestStatusInputModel
    {
        [Required]
        public long TransportRequestId { get; set; }

        [Required]
        public string NewStatus { get; set; }

        public string Remarks { get; set; }
    }

    public class DemoSeedInputModel
    {
        [Required]
        public long ElectionContextId { get; set; }

        [Range(5, 500)]
        public int VehicleCount { get; set; }

        [Range(0, 500)]
        public int RequestCount { get; set; }
    }
}

namespace NID.Areas.ElectionTransport.Models
{
    public class TransportDetailPageViewModel<T> : TransportPageViewModel
    {
        public T Data { get; set; }
    }

    public class TransportListPageViewModel<T> : TransportPageViewModel
    {
        public TransportListPageViewModel()
        {
            Items = new List<T>();
        }

        public IList<T> Items { get; set; }
    }
}
