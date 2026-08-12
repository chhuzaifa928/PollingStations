using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;

namespace NID.Areas.ElectionTransport.Services
{
    public partial class SqlTransportService : ITransportService
    {
        private readonly ITransportConnectionFactory _connectionFactory;

        public SqlTransportService()
            : this(new TransportConnectionFactory())
        {
        }

        public SqlTransportService(ITransportConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            _connectionFactory = connectionFactory;
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.Create();
        }

        private static void AddParameter(SqlCommand command, string name, object value, SqlDbType type)
        {
            SqlParameter parameter = command.Parameters.Add(name, type);
            parameter.Value = value ?? DBNull.Value;
        }

        private static void AddParameter(SqlCommand command, string name, object value, SqlDbType type, int size)
        {
            SqlParameter parameter = command.Parameters.Add(name, type, size);
            parameter.Value = value ?? DBNull.Value;
        }

        private static LiveVehicleDto MapLiveVehicle(SqlDataReader reader)
        {
            return new LiveVehicleDto
            {
                VehicleAssignmentId = reader.GetInt64Safe("VehicleAssignmentId"),
                ElectionContextId = reader.GetInt64Safe("ElectionContextId"),
                VehicleId = reader.GetInt64Safe("VehicleId"),
                RegistrationNo = reader.GetStringSafe("RegistrationNo"),
                DisplayName = reader.GetStringSafe("DisplayName"),
                Make = reader.GetStringSafe("Make"),
                Model = reader.GetStringSafe("Model"),
                ModelYear = reader.GetNullableInt32("ModelYear").HasValue
                    ? (short?)reader.GetNullableInt32("ModelYear").Value
                    : null,
                Color = reader.GetStringSafe("Color"),
                SeatingCapacity = reader.GetInt32Safe("SeatingCapacity"),
                VehicleTypeCode = reader.GetStringSafe("VehicleTypeCode"),
                VehicleType = reader.GetStringSafe("VehicleType"),
                IconKey = reader.GetStringSafe("IconKey"),
                MarkerColorHex = reader.GetStringSafe("MarkerColorHex"),
                DriverId = reader.GetInt64Safe("DriverId"),
                DriverName = reader.GetStringSafe("DriverName"),
                DriverMobile = reader.GetStringSafe("DriverMobile"),
                DriverAddress = reader.GetStringSafe("DriverAddress"),
                ProviderId = reader.GetNullableInt64("ProviderId"),
                ProviderName = reader.GetStringSafe("ProviderName"),
                ProviderType = reader.GetStringSafe("ProviderType"),
                CandidateId = reader.GetNullableInt64("CandidateId"),
                CandidateName = reader.GetStringSafe("CandidateName"),
                PartyId = reader.GetNullableInt64("PartyId"),
                PartyName = reader.GetStringSafe("PartyName"),
                PartyAbbreviation = reader.GetStringSafe("PartyAbbreviation"),
                PollingStationId = reader.GetNullableInt64("PollingStationId"),
                PollingStationSr = reader.GetNullableInt32("PollingStationSr"),
                PollingStationName = reader.GetStringSafe("PollingStationName"),
                District = reader.GetStringSafe("District"),
                Tehsil = reader.GetStringSafe("Tehsil"),
                TotalVoters = reader.GetInt32Safe("TotalVoters"),
                PollingStationLatitude = reader.GetNullableDouble("PollingStationLatitude"),
                PollingStationLongitude = reader.GetNullableDouble("PollingStationLongitude"),
                AssignmentStatus = reader.GetStringSafe("AssignmentStatus"),
                ActivatedAtUtc = reader.GetNullableDateTime("ActivatedAtUtc"),
                LastSeenAtUtc = reader.GetNullableDateTime("LastSeenAtUtc"),
                Latitude = reader.GetNullableDouble("Latitude"),
                Longitude = reader.GetNullableDouble("Longitude"),
                SpeedKph = reader.GetNullableDecimal("SpeedKph"),
                HeadingDegrees = reader.GetNullableDecimal("HeadingDegrees"),
                AccuracyMeters = reader.GetNullableDecimal("AccuracyMeters"),
                DistanceToStationMeters = reader.GetNullableDecimal("DistanceToStationMeters"),
                TodayTrips = reader.GetInt32Safe("TodayTrips"),
                TodayDistanceKm = reader.GetDecimalSafe("TodayDistanceKm"),
                TotalDistanceKm = reader.GetDecimalSafe("TotalDistanceKm"),
                CurrentTripId = reader.GetNullableInt64("CurrentTripId"),
                EffectiveStatus = reader.GetStringSafe("EffectiveStatus"),
                EffectiveIsOnline = reader.GetBooleanSafe("EffectiveIsOnline"),
                SecondsSinceLastSeen = reader.GetNullableInt32("SecondsSinceLastSeen")
            };
        }

        private static PollingStationOperationsDto MapPollingStation(SqlDataReader reader)
        {
            return new PollingStationOperationsDto
            {
                ElectionContextId = reader.GetInt64Safe("ElectionContextId"),
                PollingStationId = reader.GetInt64Safe("PollingStationId"),
                Sr = reader.GetInt32Safe("Sr"),
                PollingStationName = reader.GetStringSafe("PollingStationName"),
                District = reader.GetStringSafe("District"),
                Tehsil = reader.GetStringSafe("Tehsil"),
                Category = reader.GetStringSafe("Category"),
                TotalVoters = reader.GetInt32Safe("TotalVoters"),
                TotalBooths = reader.GetInt32Safe("TotalBooths"),
                Latitude = reader.GetNullableDouble("Latitude"),
                Longitude = reader.GetNullableDouble("Longitude"),
                PromisedVehicles = reader.GetInt32Safe("PromisedVehicles"),
                AssignedVehicles = reader.GetInt32Safe("AssignedVehicles"),
                OnlineVehicles = reader.GetInt32Safe("OnlineVehicles"),
                ApproachingVehicles = reader.GetInt32Safe("ApproachingVehicles"),
                AtPollingStationVehicles = reader.GetInt32Safe("AtPollingStationVehicles"),
                IdleVehicles = reader.GetInt32Safe("IdleVehicles"),
                OfflineVehicles = reader.GetInt32Safe("OfflineVehicles"),
                NeverActivatedVehicles = reader.GetInt32Safe("NeverActivatedVehicles"),
                ValidTrips = reader.GetInt32Safe("ValidTrips"),
                DistanceKm = reader.GetDecimalSafe("DistanceKm"),
                ConfirmedPassengers = reader.GetInt32Safe("ConfirmedPassengers"),
                TotalTransportRequests = reader.GetInt32Safe("TotalTransportRequests"),
                OpenTransportRequests = reader.GetInt32Safe("OpenTransportRequests"),
                CompletedTransportRequests = reader.GetInt32Safe("CompletedTransportRequests"),
                ServiceStatus = reader.GetStringSafe("ServiceStatus")
            };
        }

        private static ProviderPerformanceDto MapProvider(SqlDataReader reader)
        {
            return new ProviderPerformanceDto
            {
                ElectionContextId = reader.GetInt64Safe("ElectionContextId"),
                ProviderId = reader.GetInt64Safe("ProviderId"),
                ProviderName = reader.GetStringSafe("ProviderName"),
                ProviderType = reader.GetStringSafe("ProviderType"),
                Mobile = reader.GetStringSafe("Mobile"),
                Area = reader.GetStringSafe("Area"),
                CandidateName = reader.GetStringSafe("CandidateName"),
                PartyName = reader.GetStringSafe("PartyName"),
                PromisedVehicles = reader.GetInt32Safe("PromisedVehicles"),
                RegisteredVehicles = reader.GetInt32Safe("RegisteredVehicles"),
                ActivatedVehicles = reader.GetInt32Safe("ActivatedVehicles"),
                OperationalVehicles = reader.GetInt32Safe("OperationalVehicles"),
                Trips = reader.GetInt32Safe("Trips"),
                DistanceKm = reader.GetDecimalSafe("DistanceKm"),
                PollingStationsServed = reader.GetInt32Safe("PollingStationsServed"),
                PromiseFulfilmentPercent = reader.GetDecimalSafe("PromiseFulfilmentPercent"),
                EffectivenessScore = reader.GetDecimalSafe("EffectivenessScore"),
                PerformanceClass = reader.GetStringSafe("PerformanceClass")
            };
        }

        private static TransportRequestQueueDto MapRequest(SqlDataReader reader)
        {
            return new TransportRequestQueueDto
            {
                TransportRequestId = reader.GetInt64Safe("TransportRequestId"),
                RequestNo = reader.GetStringSafe("RequestNo"),
                ElectionContextId = reader.GetInt64Safe("ElectionContextId"),
                PollingStationId = reader.GetNullableInt64("PollingStationId"),
                PollingStationSr = reader.GetNullableInt32("PollingStationSr"),
                PollingStationName = reader.GetStringSafe("PollingStationName"),
                RequestedByName = reader.GetStringSafe("RequestedByName"),
                Mobile = reader.GetStringSafe("Mobile"),
                PickupAddress = reader.GetStringSafe("PickupAddress"),
                PickupArea = reader.GetStringSafe("PickupArea"),
                Latitude = reader.GetNullableDouble("Latitude") ?? 0D,
                Longitude = reader.GetNullableDouble("Longitude") ?? 0D,
                PassengerCount = reader.GetInt32Safe("PassengerCount"),
                AccessibilityCategory = reader.GetStringSafe("AccessibilityCategory"),
                RequiresWheelchair = reader.GetBooleanSafe("RequiresWheelchair"),
                RequiresAttendant = reader.GetBooleanSafe("RequiresAttendant"),
                IsRoundTripRequired = reader.GetBooleanSafe("IsRoundTripRequired"),
                Priority = reader.GetInt32Safe("Priority"),
                RequestStatus = reader.GetStringSafe("RequestStatus"),
                RequestedAtUtc = reader.GetNullableDateTime("RequestedAtUtc") ?? DateTime.MinValue,
                RequestedPickupAtUtc = reader.GetNullableDateTime("RequestedPickupAtUtc"),
                AssignedVehicleAssignmentId = reader.GetNullableInt64("AssignedVehicleAssignmentId"),
                RegistrationNo = reader.GetStringSafe("RegistrationNo"),
                VehicleType = reader.GetStringSafe("VehicleType"),
                DriverName = reader.GetStringSafe("DriverName"),
                DriverMobile = reader.GetStringSafe("DriverMobile"),
                ProviderName = reader.GetStringSafe("ProviderName"),
                WaitingMinutes = reader.GetInt32Safe("WaitingMinutes")
            };
        }

        private static TripDto MapTrip(SqlDataReader reader)
        {
            return new TripDto
            {
                TripId = reader.GetInt64Safe("TripId"),
                VehicleAssignmentId = reader.GetInt64Safe("VehicleAssignmentId"),
                RegistrationNo = reader.GetStringSafe("RegistrationNo"),
                VehicleType = reader.GetStringSafe("VehicleType"),
                DriverName = reader.GetStringSafe("DriverName"),
                ProviderName = reader.GetStringSafe("ProviderName"),
                PollingStationId = reader.GetInt64Safe("PollingStationId"),
                PollingStationName = reader.GetStringSafe("PollingStationName"),
                TripNumber = reader.GetInt32Safe("TripNumber"),
                TripDirection = reader.GetStringSafe("TripDirection"),
                TripStatus = reader.GetStringSafe("TripStatus"),
                ValidationStatus = reader.GetStringSafe("ValidationStatus"),
                StartedAtUtc = reader.GetNullableDateTime("StartedAtUtc"),
                ArrivedAtUtc = reader.GetNullableDateTime("ArrivedAtUtc"),
                DepartedAtUtc = reader.GetNullableDateTime("DepartedAtUtc"),
                CompletedAtUtc = reader.GetNullableDateTime("CompletedAtUtc"),
                DistanceKm = reader.GetNullableDecimal("DistanceKm"),
                DurationMinutes = reader.GetNullableDecimal("DurationMinutes"),
                DwellSeconds = reader.GetNullableInt32("DwellSeconds"),
                ConfirmedPassengers = reader.GetNullableInt32("ConfirmedPassengers"),
                DetectedBy = reader.GetStringSafe("DetectedBy")
            };
        }

        private static string NormalizeStatus(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        }

        private static DateTime ToUtcFromPakistan(DateTime local)
        {
            try
            {
                TimeZoneInfo pakistan = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), pakistan);
            }
            catch
            {
                return local.ToUniversalTime();
            }
        }
    }
}
