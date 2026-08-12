using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;

namespace NID.Areas.ElectionTransport.Services
{
    public partial class SqlTransportService
    {
        public async Task<VehicleDetailsDto> GetVehicleDetailsAsync(long vehicleAssignmentId)
        {
            VehicleDetailsDto result = new VehicleDetailsDto();

            using (SqlConnection connection = CreateConnection())
            {
                await connection.OpenAsync();

                const string vehicleSql = @"
SELECT *
FROM Transport.vw_LiveVehicle
WHERE VehicleAssignmentId = @VehicleAssignmentId;";

                using (SqlCommand command = new SqlCommand(vehicleSql, connection))
                {
                    AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId, SqlDbType.BigInt);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.Vehicle = MapLiveVehicle(reader);
                        }
                    }
                }

                if (result.Vehicle == null)
                {
                    return null;
                }

                const string statsSql = @"
SELECT
    ValidTrips = COUNT(CASE WHEN ValidationStatus = N'VALID' THEN 1 END),
    AverageTripKm = ISNULL(AVG(CASE WHEN ValidationStatus = N'VALID' THEN DistanceKm END), 0),
    AverageTripMinutes = ISNULL(AVG(CASE WHEN ValidationStatus = N'VALID' THEN DurationMinutes END), 0)
FROM Transport.Trip
WHERE VehicleAssignmentId = @VehicleAssignmentId;

SELECT
    FirstSeenAtUtc = MIN(RecordedAtUtc),
    LastSeenAtUtc = MAX(RecordedAtUtc)
FROM Transport.VehicleLocation
WHERE VehicleAssignmentId = @VehicleAssignmentId;";

                using (SqlCommand command = new SqlCommand(statsSql, connection))
                {
                    AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId, SqlDbType.BigInt);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.ValidTrips = reader.GetInt32Safe("ValidTrips");
                            result.AverageTripKm = reader.GetDecimalSafe("AverageTripKm");
                            result.AverageTripMinutes = reader.GetDecimalSafe("AverageTripMinutes");
                        }

                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            result.FirstSeenAtUtc = reader.GetNullableDateTime("FirstSeenAtUtc");
                            result.LastSeenAtUtc = reader.GetNullableDateTime("LastSeenAtUtc");

                            if (result.FirstSeenAtUtc.HasValue && result.LastSeenAtUtc.HasValue)
                            {
                                result.OperationalMinutes = Math.Max(
                                    0,
                                    Convert.ToInt32((result.LastSeenAtUtc.Value - result.FirstSeenAtUtc.Value).TotalMinutes));
                            }
                        }
                    }
                }
            }

            result.RecentTrips = (await GetVehicleTripsAsync(vehicleAssignmentId, 25)).ToList();
            result.RecentRequests = (await GetVehicleRequestsAsync(vehicleAssignmentId, 20)).ToList();
            return result;
        }

        public async Task<IList<VehicleTrailPointDto>> GetVehicleTrailAsync(long vehicleAssignmentId, int minutes)
        {
            minutes = Math.Max(5, Math.Min(720, minutes));
            const string sql = @"
SELECT TOP (3000)
    VehicleLocationId,
    RecordedAtUtc,
    Latitude,
    Longitude,
    SpeedKph,
    HeadingDegrees,
    AccuracyMeters,
    LocationSource
FROM Transport.VehicleLocation
WHERE VehicleAssignmentId = @VehicleAssignmentId
  AND RecordedAtUtc >= DATEADD(MINUTE, -@Minutes, SYSUTCDATETIME())
  AND IsValid = 1
ORDER BY RecordedAtUtc;";

            List<VehicleTrailPointDto> result = new List<VehicleTrailPointDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId, SqlDbType.BigInt);
                AddParameter(command, "@Minutes", minutes, SqlDbType.Int);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new VehicleTrailPointDto
                        {
                            VehicleLocationId = reader.GetInt64Safe("VehicleLocationId"),
                            RecordedAtUtc = reader.GetNullableDateTime("RecordedAtUtc") ?? DateTime.MinValue,
                            Latitude = reader.GetNullableDouble("Latitude") ?? 0D,
                            Longitude = reader.GetNullableDouble("Longitude") ?? 0D,
                            SpeedKph = reader.GetNullableDecimal("SpeedKph"),
                            HeadingDegrees = reader.GetNullableDecimal("HeadingDegrees"),
                            AccuracyMeters = reader.GetNullableDecimal("AccuracyMeters"),
                            LocationSource = reader.GetStringSafe("LocationSource")
                        });
                    }
                }
            }

            return result;
        }

        public async Task<PollingStationDetailsDto> GetPollingStationDetailsAsync(long pollingStationId)
        {
            PollingStationDetailsDto result = new PollingStationDetailsDto();
            long contextId = 0;

            const string stationSql = @"
SELECT *
FROM Transport.vw_PollingStationOperations
WHERE PollingStationId = @PollingStationId;";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(stationSql, connection))
            {
                AddParameter(command, "@PollingStationId", pollingStationId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        result.PollingStation = MapPollingStation(reader);
                        contextId = result.PollingStation.ElectionContextId;
                    }
                }
            }

            if (result.PollingStation == null)
            {
                return null;
            }

            result.Vehicles = (await GetLiveVehiclesAsync(new VehicleMapFilterModel
            {
                ElectionContextId = contextId,
                PollingStationId = pollingStationId
            })).ToList();

            result.RecentTrips = (await GetStationTripsAsync(pollingStationId, 40)).ToList();
            result.Requests = (await GetRequestsAsync(contextId, null))
                .Where(x => x.PollingStationId == pollingStationId)
                .OrderByDescending(x => x.RequestedAtUtc)
                .Take(30)
                .ToList();

            return result;
        }

        public async Task<ProviderDetailsDto> GetProviderDetailsAsync(long providerId)
        {
            ProviderDetailsDto result = new ProviderDetailsDto();
            long contextId = 0;

            const string providerSql = @"
SELECT *
FROM Transport.vw_ProviderPerformance
WHERE ProviderId = @ProviderId;";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(providerSql, connection))
            {
                AddParameter(command, "@ProviderId", providerId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        result.Provider = MapProvider(reader);
                        contextId = result.Provider.ElectionContextId;
                    }
                }
            }

            if (result.Provider == null)
            {
                return null;
            }

            result.Vehicles = (await GetLiveVehiclesAsync(new VehicleMapFilterModel
            {
                ElectionContextId = contextId,
                ProviderId = providerId
            })).ToList();

            result.Commitments = (await GetProviderCommitmentsAsync(providerId)).ToList();
            return result;
        }

        public async Task<TransportRequestDetailsDto> GetRequestDetailsAsync(long transportRequestId)
        {
            TransportRequestDetailsDto result = new TransportRequestDetailsDto();

            const string requestSql = @"
SELECT *
FROM Transport.vw_TransportRequestQueue
WHERE TransportRequestId = @TransportRequestId;

SELECT
    RD.RequestDispatchId,
    RD.TransportRequestId,
    RD.VehicleAssignmentId,
    RD.DriverDistanceMeters,
    RD.EstimatedArrivalSeconds,
    RD.RoutingScore,
    RD.ExpiresAtUtc,
    V.RegistrationNo,
    VT.Name AS VehicleType,
    VT.IconKey,
    V.Color,
    V.SeatingCapacity,
    D.DriverName,
    D.Mobile AS DriverMobile,
    P.ProviderName,
    PS.PollingStationName
FROM Transport.RequestDispatch AS RD
INNER JOIN Transport.VehicleAssignment AS VA
    ON VA.VehicleAssignmentId = RD.VehicleAssignmentId
INNER JOIN Transport.Vehicle AS V
    ON V.VehicleId = VA.VehicleId
INNER JOIN Transport.VehicleType AS VT
    ON VT.VehicleTypeId = V.VehicleTypeId
INNER JOIN Transport.Driver AS D
    ON D.DriverId = VA.DriverId
LEFT JOIN Transport.Provider AS P
    ON P.ProviderId = VA.ProviderId
LEFT JOIN Transport.PollingStation AS PS
    ON PS.PollingStationId = VA.AssignedPollingStationId
WHERE RD.TransportRequestId = @TransportRequestId
ORDER BY RD.RoutingScore;

SELECT PreviousStatus, NewStatus, ChangedAtUtc, ChangedBy, Remarks
FROM Transport.RequestStatusHistory
WHERE TransportRequestId = @TransportRequestId
ORDER BY ChangedAtUtc;";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(requestSql, connection))
            {
                AddParameter(command, "@TransportRequestId", transportRequestId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        result.Request = MapRequest(reader);
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Dispatches.Add(MapDispatch(reader));
                        }
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.StatusHistory.Add(new RequestStatusHistoryDto
                            {
                                PreviousStatus = reader.GetStringSafe("PreviousStatus"),
                                NewStatus = reader.GetStringSafe("NewStatus"),
                                ChangedAtUtc = reader.GetNullableDateTime("ChangedAtUtc") ?? DateTime.MinValue,
                                ChangedBy = reader.GetStringSafe("ChangedBy"),
                                Remarks = reader.GetStringSafe("Remarks")
                            });
                        }
                    }
                }
            }

            return result.Request == null ? null : result;
        }

        public async Task<PublicRequestStatusDto> GetPublicRequestStatusAsync(string requestNo, string mobileLast4)
        {
            if (string.IsNullOrWhiteSpace(requestNo) || string.IsNullOrWhiteSpace(mobileLast4))
            {
                return null;
            }

            const string sql = @"
SELECT
    R.RequestNo,
    R.RequestStatus,
    PS.PollingStationName,
    V.RegistrationNo,
    VT.Name AS VehicleType,
    D.DriverName,
    D.Mobile AS DriverMobile,
    R.RequestedAtUtc,
    R.AssignedAtUtc,
    R.PickedUpAtUtc,
    R.DroppedOffAtUtc
FROM Transport.TransportRequest AS R
LEFT JOIN Transport.PollingStation AS PS
    ON PS.PollingStationId = R.PollingStationId
LEFT JOIN Transport.VehicleAssignment AS VA
    ON VA.VehicleAssignmentId = R.AssignedVehicleAssignmentId
LEFT JOIN Transport.Vehicle AS V
    ON V.VehicleId = VA.VehicleId
LEFT JOIN Transport.VehicleType AS VT
    ON VT.VehicleTypeId = V.VehicleTypeId
LEFT JOIN Transport.Driver AS D
    ON D.DriverId = VA.DriverId
WHERE R.RequestNo = @RequestNo
  AND RIGHT(REPLACE(REPLACE(R.Mobile, N'-', N''), N' ', N''), 4) = @MobileLast4;";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@RequestNo", requestNo.Trim(), SqlDbType.NVarChar, 30);
                AddParameter(command, "@MobileLast4", mobileLast4.Trim(), SqlDbType.NVarChar, 4);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return null;
                    }

                    string driverMobile = reader.GetStringSafe("DriverMobile");
                    return new PublicRequestStatusDto
                    {
                        RequestNo = reader.GetStringSafe("RequestNo"),
                        RequestStatus = reader.GetStringSafe("RequestStatus"),
                        PollingStationName = reader.GetStringSafe("PollingStationName"),
                        VehicleRegistrationNo = reader.GetStringSafe("RegistrationNo"),
                        VehicleType = reader.GetStringSafe("VehicleType"),
                        DriverName = reader.GetStringSafe("DriverName"),
                        DriverMobileMasked = MaskMobile(driverMobile),
                        RequestedAtUtc = reader.GetNullableDateTime("RequestedAtUtc") ?? DateTime.MinValue,
                        AssignedAtUtc = reader.GetNullableDateTime("AssignedAtUtc"),
                        PickedUpAtUtc = reader.GetNullableDateTime("PickedUpAtUtc"),
                        DroppedOffAtUtc = reader.GetNullableDateTime("DroppedOffAtUtc"),
                        PublicMessage = BuildPublicStatusMessage(reader.GetStringSafe("RequestStatus"))
                    };
                }
            }
        }

        private async Task<IList<TripDto>> GetVehicleTripsAsync(long vehicleAssignmentId, int take)
        {
            const string sql = @"
SELECT TOP (@Take)
    T.TripId,
    T.VehicleAssignmentId,
    V.RegistrationNo,
    VT.Name AS VehicleType,
    D.DriverName,
    P.ProviderName,
    T.PollingStationId,
    PS.PollingStationName,
    T.TripNumber,
    T.TripDirection,
    T.TripStatus,
    T.ValidationStatus,
    T.StartedAtUtc,
    T.ArrivedAtUtc,
    T.DepartedAtUtc,
    T.CompletedAtUtc,
    T.DistanceKm,
    T.DurationMinutes,
    T.DwellSeconds,
    T.ConfirmedPassengers,
    T.DetectedBy
FROM Transport.Trip AS T
INNER JOIN Transport.VehicleAssignment AS VA ON VA.VehicleAssignmentId = T.VehicleAssignmentId
INNER JOIN Transport.Vehicle AS V ON V.VehicleId = T.VehicleId
INNER JOIN Transport.VehicleType AS VT ON VT.VehicleTypeId = V.VehicleTypeId
INNER JOIN Transport.Driver AS D ON D.DriverId = VA.DriverId
LEFT JOIN Transport.Provider AS P ON P.ProviderId = VA.ProviderId
INNER JOIN Transport.PollingStation AS PS ON PS.PollingStationId = T.PollingStationId
WHERE T.VehicleAssignmentId = @VehicleAssignmentId
ORDER BY COALESCE(T.CompletedAtUtc, T.ArrivedAtUtc, T.StartedAtUtc) DESC;";

            return await ReadTripsAsync(sql, delegate(SqlCommand command)
            {
                AddParameter(command, "@Take", take, SqlDbType.Int);
                AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId, SqlDbType.BigInt);
            });
        }

        private async Task<IList<TripDto>> GetStationTripsAsync(long pollingStationId, int take)
        {
            const string sql = @"
SELECT TOP (@Take)
    T.TripId,
    T.VehicleAssignmentId,
    V.RegistrationNo,
    VT.Name AS VehicleType,
    D.DriverName,
    P.ProviderName,
    T.PollingStationId,
    PS.PollingStationName,
    T.TripNumber,
    T.TripDirection,
    T.TripStatus,
    T.ValidationStatus,
    T.StartedAtUtc,
    T.ArrivedAtUtc,
    T.DepartedAtUtc,
    T.CompletedAtUtc,
    T.DistanceKm,
    T.DurationMinutes,
    T.DwellSeconds,
    T.ConfirmedPassengers,
    T.DetectedBy
FROM Transport.Trip AS T
INNER JOIN Transport.VehicleAssignment AS VA ON VA.VehicleAssignmentId = T.VehicleAssignmentId
INNER JOIN Transport.Vehicle AS V ON V.VehicleId = T.VehicleId
INNER JOIN Transport.VehicleType AS VT ON VT.VehicleTypeId = V.VehicleTypeId
INNER JOIN Transport.Driver AS D ON D.DriverId = VA.DriverId
LEFT JOIN Transport.Provider AS P ON P.ProviderId = VA.ProviderId
INNER JOIN Transport.PollingStation AS PS ON PS.PollingStationId = T.PollingStationId
WHERE T.PollingStationId = @PollingStationId
ORDER BY COALESCE(T.CompletedAtUtc, T.ArrivedAtUtc, T.StartedAtUtc) DESC;";

            return await ReadTripsAsync(sql, delegate(SqlCommand command)
            {
                AddParameter(command, "@Take", take, SqlDbType.Int);
                AddParameter(command, "@PollingStationId", pollingStationId, SqlDbType.BigInt);
            });
        }

        private async Task<IList<TripDto>> ReadTripsAsync(string sql, Action<SqlCommand> configure)
        {
            List<TripDto> result = new List<TripDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                configure(command);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapTrip(reader));
                    }
                }
            }

            return result;
        }

        private async Task<IList<TransportRequestQueueDto>> GetVehicleRequestsAsync(long vehicleAssignmentId, int take)
        {
            const string sql = @"
SELECT TOP (@Take) *
FROM Transport.vw_TransportRequestQueue
WHERE AssignedVehicleAssignmentId = @VehicleAssignmentId
ORDER BY RequestedAtUtc DESC;";

            List<TransportRequestQueueDto> result = new List<TransportRequestQueueDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@Take", take, SqlDbType.Int);
                AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapRequest(reader));
                    }
                }
            }

            return result;
        }

        private async Task<IList<ProviderCommitmentDto>> GetProviderCommitmentsAsync(long providerId)
        {
            const string sql = @"
SELECT
    PC.ProviderCommitmentId,
    PS.PollingStationName,
    VT.Name AS VehicleType,
    PC.PromisedQuantity,
    PC.CommitmentStatus,
    PC.CommitmentDate,
    PC.Remarks
FROM Transport.ProviderCommitment AS PC
LEFT JOIN Transport.PollingStation AS PS
    ON PS.PollingStationId = PC.PollingStationId
LEFT JOIN Transport.VehicleType AS VT
    ON VT.VehicleTypeId = PC.VehicleTypeId
WHERE PC.ProviderId = @ProviderId
ORDER BY PC.CommitmentDate DESC, PC.ProviderCommitmentId DESC;";

            List<ProviderCommitmentDto> result = new List<ProviderCommitmentDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ProviderId", providerId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new ProviderCommitmentDto
                        {
                            ProviderCommitmentId = reader.GetInt64Safe("ProviderCommitmentId"),
                            PollingStationName = reader.GetStringSafe("PollingStationName"),
                            VehicleType = reader.GetStringSafe("VehicleType"),
                            PromisedQuantity = reader.GetInt32Safe("PromisedQuantity"),
                            CommitmentStatus = reader.GetStringSafe("CommitmentStatus"),
                            CommitmentDate = reader.GetNullableDateTime("CommitmentDate"),
                            Remarks = reader.GetStringSafe("Remarks")
                        });
                    }
                }
            }

            return result;
        }

        private static DispatchOfferDto MapDispatch(SqlDataReader reader)
        {
            return new DispatchOfferDto
            {
                RequestDispatchId = reader.GetInt64Safe("RequestDispatchId"),
                TransportRequestId = reader.GetInt64Safe("TransportRequestId"),
                VehicleAssignmentId = reader.GetInt64Safe("VehicleAssignmentId"),
                DriverDistanceMeters = reader.GetDecimalSafe("DriverDistanceMeters"),
                EstimatedArrivalSeconds = reader.GetInt32Safe("EstimatedArrivalSeconds"),
                RoutingScore = reader.GetDecimalSafe("RoutingScore"),
                ExpiresAtUtc = reader.GetNullableDateTime("ExpiresAtUtc"),
                RegistrationNo = reader.GetStringSafe("RegistrationNo"),
                VehicleType = reader.GetStringSafe("VehicleType"),
                IconKey = reader.GetStringSafe("IconKey"),
                Color = reader.GetStringSafe("Color"),
                SeatingCapacity = reader.GetInt32Safe("SeatingCapacity"),
                DriverName = reader.GetStringSafe("DriverName"),
                DriverMobile = reader.GetStringSafe("DriverMobile"),
                ProviderName = reader.GetStringSafe("ProviderName"),
                PollingStationName = reader.GetStringSafe("PollingStationName")
            };
        }

        private static string MaskMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 4)
            {
                return null;
            }

            return new string('*', Math.Max(0, mobile.Length - 4)) + mobile.Substring(mobile.Length - 4);
        }

        private static string BuildPublicStatusMessage(string status)
        {
            switch ((status ?? string.Empty).ToUpperInvariant())
            {
                case "NEW":
                case "ROUTING":
                    return "Your request has been received and nearby eligible vehicles are being checked.";
                case "OFFERED":
                    return "Your request has been offered to nearby available drivers.";
                case "ASSIGNED":
                    return "A vehicle has been assigned to your request.";
                case "DRIVER_EN_ROUTE":
                    return "The assigned driver is travelling to your pickup location.";
                case "PICKED_UP":
                    return "Pickup has been recorded. The vehicle is travelling toward the polling station.";
                case "DROPPED_OFF":
                    return "Drop-off at the polling station has been recorded.";
                case "COMPLETED":
                    return "The transport request has been completed.";
                case "NO_VEHICLE":
                    return "No eligible nearby vehicle is currently available. The coordination team may retry routing.";
                case "CANCELLED":
                    return "The request has been cancelled.";
                default:
                    return "The request is being processed.";
            }
        }
    }
}
