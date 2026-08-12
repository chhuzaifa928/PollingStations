using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace NID.Areas.ElectionTransport.Models
{
    public class ElectionContextDto
    {
        public long ElectionContextId { get; set; }
        public string Election { get; set; }
        public short ElectionYear { get; set; }
        public string Assembly { get; set; }
        public string Seat { get; set; }
        public DateTime? ElectionDate { get; set; }
        public bool IsDemoMode { get; set; }
        public int GeofenceRadiusMeters { get; set; }
        public int OfflineThresholdSeconds { get; set; }

        public string DisplayText
        {
            get
            {
                return ElectionYear + " • " + Election + " • " + Assembly + " • " + Seat;
            }
        }
    }

    public class TransportDashboardSummaryDto
    {
        public int PromisedVehicles { get; set; }
        public int RegisteredVehicles { get; set; }
        public int ActivatedVehicles { get; set; }
        public int ActiveVehicles { get; set; }
        public int OfflineVehicles { get; set; }
        public int NeverActivatedVehicles { get; set; }
        public int TripsCompleted { get; set; }
        public decimal DistanceKm { get; set; }
        public int PollingStationsServed { get; set; }
        public int TotalPollingStations { get; set; }
        public int TotalRequests { get; set; }
        public int OpenRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int ConfirmedPassengers { get; set; }
        public decimal PromiseFulfilmentPercent { get; set; }
        public decimal PollingStationCoveragePercent { get; set; }
    }

    public class TransportDashboardDto
    {
        public TransportDashboardDto()
        {
            Vehicles = new List<LiveVehicleDto>();
            PollingStations = new List<PollingStationOperationsDto>();
            Providers = new List<ProviderPerformanceDto>();
            Requests = new List<TransportRequestQueueDto>();
            Timeline = new List<TransportTimelinePointDto>();
            VehicleTypes = new List<VehicleTypeCountDto>();
            Exceptions = new List<TransportExceptionDto>();
        }

        public DateTime ServerTimeUtc { get; set; }
        public TransportDashboardSummaryDto Summary { get; set; }
        public IList<LiveVehicleDto> Vehicles { get; set; }
        public IList<PollingStationOperationsDto> PollingStations { get; set; }
        public IList<ProviderPerformanceDto> Providers { get; set; }
        public IList<TransportRequestQueueDto> Requests { get; set; }
        public IList<TransportTimelinePointDto> Timeline { get; set; }
        public IList<VehicleTypeCountDto> VehicleTypes { get; set; }
        public IList<TransportExceptionDto> Exceptions { get; set; }
    }

    public class LiveVehicleDto
    {
        public long VehicleAssignmentId { get; set; }
        public long ElectionContextId { get; set; }
        public long VehicleId { get; set; }
        public string RegistrationNo { get; set; }
        public string DisplayName { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public short? ModelYear { get; set; }
        public string Color { get; set; }
        public int SeatingCapacity { get; set; }
        public string VehicleTypeCode { get; set; }
        public string VehicleType { get; set; }
        public string IconKey { get; set; }
        public string MarkerColorHex { get; set; }
        public long DriverId { get; set; }
        public string DriverName { get; set; }
        public string DriverMobile { get; set; }
        public string DriverAddress { get; set; }
        public long? ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string ProviderType { get; set; }
        public long? CandidateId { get; set; }
        public string CandidateName { get; set; }
        public long? PartyId { get; set; }
        public string PartyName { get; set; }
        public string PartyAbbreviation { get; set; }
        public long? PollingStationId { get; set; }
        public int? PollingStationSr { get; set; }
        public string PollingStationName { get; set; }
        public string District { get; set; }
        public string Tehsil { get; set; }
        public int TotalVoters { get; set; }
        public double? PollingStationLatitude { get; set; }
        public double? PollingStationLongitude { get; set; }
        public string AssignmentStatus { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
        public DateTime? LastSeenAtUtc { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal? SpeedKph { get; set; }
        public decimal? HeadingDegrees { get; set; }
        public decimal? AccuracyMeters { get; set; }
        public decimal? DistanceToStationMeters { get; set; }
        public int TodayTrips { get; set; }
        public decimal TodayDistanceKm { get; set; }
        public decimal TotalDistanceKm { get; set; }
        public long? CurrentTripId { get; set; }
        public string EffectiveStatus { get; set; }
        public bool EffectiveIsOnline { get; set; }
        public int? SecondsSinceLastSeen { get; set; }
    }

    public class PollingStationOperationsDto
    {
        public long ElectionContextId { get; set; }
        public long PollingStationId { get; set; }
        public int Sr { get; set; }
        public string PollingStationName { get; set; }
        public string District { get; set; }
        public string Tehsil { get; set; }
        public string Category { get; set; }
        public int TotalVoters { get; set; }
        public int TotalBooths { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int PromisedVehicles { get; set; }
        public int AssignedVehicles { get; set; }
        public int OnlineVehicles { get; set; }
        public int ApproachingVehicles { get; set; }
        public int AtPollingStationVehicles { get; set; }
        public int IdleVehicles { get; set; }
        public int OfflineVehicles { get; set; }
        public int NeverActivatedVehicles { get; set; }
        public int ValidTrips { get; set; }
        public decimal DistanceKm { get; set; }
        public int ConfirmedPassengers { get; set; }
        public int TotalTransportRequests { get; set; }
        public int OpenTransportRequests { get; set; }
        public int CompletedTransportRequests { get; set; }
        public string ServiceStatus { get; set; }
    }

    public class ProviderPerformanceDto
    {
        public long ElectionContextId { get; set; }
        public long ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string ProviderType { get; set; }
        public string Mobile { get; set; }
        public string Area { get; set; }
        public string CandidateName { get; set; }
        public string PartyName { get; set; }
        public int PromisedVehicles { get; set; }
        public int RegisteredVehicles { get; set; }
        public int ActivatedVehicles { get; set; }
        public int OperationalVehicles { get; set; }
        public int Trips { get; set; }
        public decimal DistanceKm { get; set; }
        public int PollingStationsServed { get; set; }
        public decimal PromiseFulfilmentPercent { get; set; }
        public decimal EffectivenessScore { get; set; }
        public string PerformanceClass { get; set; }
    }

    public class TransportRequestQueueDto
    {
        public long TransportRequestId { get; set; }
        public string RequestNo { get; set; }
        public long ElectionContextId { get; set; }
        public long? PollingStationId { get; set; }
        public int? PollingStationSr { get; set; }
        public string PollingStationName { get; set; }
        public string RequestedByName { get; set; }
        public string Mobile { get; set; }
        public string PickupAddress { get; set; }
        public string PickupArea { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int PassengerCount { get; set; }
        public string AccessibilityCategory { get; set; }
        public bool RequiresWheelchair { get; set; }
        public bool RequiresAttendant { get; set; }
        public bool IsRoundTripRequired { get; set; }
        public int Priority { get; set; }
        public string RequestStatus { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? RequestedPickupAtUtc { get; set; }
        public long? AssignedVehicleAssignmentId { get; set; }
        public string RegistrationNo { get; set; }
        public string VehicleType { get; set; }
        public string DriverName { get; set; }
        public string DriverMobile { get; set; }
        public string ProviderName { get; set; }
        public int WaitingMinutes { get; set; }
    }

    public class TransportTimelinePointDto
    {
        public DateTime BucketUtc { get; set; }
        public int ActiveVehicles { get; set; }
        public int Trips { get; set; }
        public int Requests { get; set; }
        public decimal DistanceKm { get; set; }
    }

    public class VehicleTypeCountDto
    {
        public string VehicleTypeCode { get; set; }
        public string VehicleType { get; set; }
        public string IconKey { get; set; }
        public int Total { get; set; }
        public int Active { get; set; }
        public int Trips { get; set; }
    }

    public class TransportExceptionDto
    {
        public string ExceptionType { get; set; }
        public string Severity { get; set; }
        public long VehicleAssignmentId { get; set; }
        public string RegistrationNo { get; set; }
        public string VehicleType { get; set; }
        public string DriverName { get; set; }
        public string DriverMobile { get; set; }
        public string ProviderName { get; set; }
        public string PollingStationName { get; set; }
        public string EffectiveStatus { get; set; }
        public DateTime? LastSeenAtUtc { get; set; }
        public int? MinutesSinceLastSeen { get; set; }
        public int TodayTrips { get; set; }
        public string Message { get; set; }
    }

    public class VehicleTrailPointDto
    {
        public long VehicleLocationId { get; set; }
        public DateTime RecordedAtUtc { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal? SpeedKph { get; set; }
        public decimal? HeadingDegrees { get; set; }
        public decimal? AccuracyMeters { get; set; }
        public string LocationSource { get; set; }
    }

    public class TripDto
    {
        public long TripId { get; set; }
        public long VehicleAssignmentId { get; set; }
        public string RegistrationNo { get; set; }
        public string VehicleType { get; set; }
        public string DriverName { get; set; }
        public string ProviderName { get; set; }
        public long PollingStationId { get; set; }
        public string PollingStationName { get; set; }
        public int TripNumber { get; set; }
        public string TripDirection { get; set; }
        public string TripStatus { get; set; }
        public string ValidationStatus { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? ArrivedAtUtc { get; set; }
        public DateTime? DepartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public decimal? DistanceKm { get; set; }
        public decimal? DurationMinutes { get; set; }
        public int? DwellSeconds { get; set; }
        public int? ConfirmedPassengers { get; set; }
        public string DetectedBy { get; set; }
    }

    public class VehicleDetailsDto
    {
        public VehicleDetailsDto()
        {
            RecentTrips = new List<TripDto>();
            RecentRequests = new List<TransportRequestQueueDto>();
        }

        public LiveVehicleDto Vehicle { get; set; }
        public IList<TripDto> RecentTrips { get; set; }
        public IList<TransportRequestQueueDto> RecentRequests { get; set; }
        public decimal AverageTripKm { get; set; }
        public decimal AverageTripMinutes { get; set; }
        public int ValidTrips { get; set; }
        public DateTime? FirstSeenAtUtc { get; set; }
        public DateTime? LastSeenAtUtc { get; set; }
        public int OperationalMinutes { get; set; }
    }

    public class PollingStationDetailsDto
    {
        public PollingStationDetailsDto()
        {
            Vehicles = new List<LiveVehicleDto>();
            RecentTrips = new List<TripDto>();
            Requests = new List<TransportRequestQueueDto>();
        }

        public PollingStationOperationsDto PollingStation { get; set; }
        public IList<LiveVehicleDto> Vehicles { get; set; }
        public IList<TripDto> RecentTrips { get; set; }
        public IList<TransportRequestQueueDto> Requests { get; set; }
    }

    public class ProviderDetailsDto
    {
        public ProviderDetailsDto()
        {
            Vehicles = new List<LiveVehicleDto>();
            Commitments = new List<ProviderCommitmentDto>();
        }

        public ProviderPerformanceDto Provider { get; set; }
        public IList<LiveVehicleDto> Vehicles { get; set; }
        public IList<ProviderCommitmentDto> Commitments { get; set; }
    }

    public class ProviderCommitmentDto
    {
        public long ProviderCommitmentId { get; set; }
        public string PollingStationName { get; set; }
        public string VehicleType { get; set; }
        public int PromisedQuantity { get; set; }
        public string CommitmentStatus { get; set; }
        public DateTime? CommitmentDate { get; set; }
        public string Remarks { get; set; }
    }

    public class DispatchOfferDto
    {
        public long RequestDispatchId { get; set; }
        public long TransportRequestId { get; set; }
        public long VehicleAssignmentId { get; set; }
        public decimal DriverDistanceMeters { get; set; }
        public int EstimatedArrivalSeconds { get; set; }
        public decimal RoutingScore { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string RegistrationNo { get; set; }
        public string VehicleType { get; set; }
        public string IconKey { get; set; }
        public string Color { get; set; }
        public int SeatingCapacity { get; set; }
        public string DriverName { get; set; }
        public string DriverMobile { get; set; }
        public string ProviderName { get; set; }
        public string PollingStationName { get; set; }
    }

    public class RequestStatusHistoryDto
    {
        public string PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTime ChangedAtUtc { get; set; }
        public string ChangedBy { get; set; }
        public string Remarks { get; set; }
    }

    public class TransportRequestDetailsDto
    {
        public TransportRequestDetailsDto()
        {
            Dispatches = new List<DispatchOfferDto>();
            StatusHistory = new List<RequestStatusHistoryDto>();
        }

        public TransportRequestQueueDto Request { get; set; }
        public IList<DispatchOfferDto> Dispatches { get; set; }
        public IList<RequestStatusHistoryDto> StatusHistory { get; set; }
    }

    public class PublicRequestStatusDto
    {
        public string RequestNo { get; set; }
        public string RequestStatus { get; set; }
        public string PollingStationName { get; set; }
        public string VehicleRegistrationNo { get; set; }
        public string VehicleType { get; set; }
        public string DriverName { get; set; }
        public string DriverMobileMasked { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? AssignedAtUtc { get; set; }
        public DateTime? PickedUpAtUtc { get; set; }
        public DateTime? DroppedOffAtUtc { get; set; }
        public string PublicMessage { get; set; }
    }

    public class SimulationTickResultDto
    {
        public int RoutesPrepared { get; set; }
        public int VehiclesMoved { get; set; }
        public int LocationsRecorded { get; set; }
        public DateTime TickUtc { get; set; }
    }

    public class LocationPushResultDto
    {
        public long VehicleLocationId { get; set; }
        public long VehicleAssignmentId { get; set; }
        public long VehicleId { get; set; }
        public string CurrentStatus { get; set; }
        public decimal? DistanceToStationMeters { get; set; }
        public decimal? SegmentDistanceKm { get; set; }
        public bool IsInsidePollingStationBuffer { get; set; }
        public long? NewTripId { get; set; }
    }
}
