using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;

namespace NID.Areas.ElectionTransport.Services
{
    public partial class SqlTransportService
    {
        public async Task<IList<ElectionContextDto>> GetContextsAsync()
        {
            const string sql = @"
SELECT ElectionContextId, Election, ElectionYear, Assembly, Seat,
       ElectionDate, IsDemoMode, GeofenceRadiusMeters,
       OfflineThresholdSeconds
FROM Transport.ElectionContext
WHERE IsActive = 1
ORDER BY ElectionYear DESC, Election, Assembly, Seat;";

            List<ElectionContextDto> result = new List<ElectionContextDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapContext(reader));
                    }
                }
            }

            return result;
        }

        public async Task<ElectionContextDto> GetContextAsync(long electionContextId)
        {
            const string sql = @"
SELECT ElectionContextId, Election, ElectionYear, Assembly, Seat,
       ElectionDate, IsDemoMode, GeofenceRadiusMeters,
       OfflineThresholdSeconds
FROM Transport.ElectionContext
WHERE ElectionContextId = @ElectionContextId;";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync() ? MapContext(reader) : null;
                }
            }
        }

        public async Task<TransportDashboardDto> GetDashboardAsync(long electionContextId)
        {
            TransportDashboardDto dashboard = new TransportDashboardDto
            {
                ServerTimeUtc = DateTime.UtcNow,
                Summary = await GetDashboardSummaryAsync(electionContextId)
            };

            dashboard.Vehicles = (await GetLiveVehiclesAsync(new VehicleMapFilterModel
            {
                ElectionContextId = electionContextId
            })).ToList();

            dashboard.PollingStations = (await GetPollingStationsAsync(electionContextId))
                .OrderByDescending(x => x.OnlineVehicles)
                .ThenBy(x => x.Sr)
                .ToList();

            dashboard.Providers = (await GetProvidersAsync(electionContextId))
                .OrderByDescending(x => x.EffectivenessScore)
                .Take(12)
                .ToList();

            dashboard.Requests = (await GetRequestsAsync(electionContextId, null))
                .Where(x => x.RequestStatus != "COMPLETED" && x.RequestStatus != "CANCELLED")
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.WaitingMinutes)
                .Take(12)
                .ToList();

            dashboard.Timeline = (await GetTimelineAsync(electionContextId, 24)).ToList();
            dashboard.VehicleTypes = (await GetVehicleTypeCountsAsync(electionContextId)).ToList();
            dashboard.Exceptions = (await GetExceptionsAsync(electionContextId)).Take(12).ToList();

            return dashboard;
        }

        public async Task<IList<LiveVehicleDto>> GetLiveVehiclesAsync(VehicleMapFilterModel filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            StringBuilder sql = new StringBuilder(@"
SELECT *
FROM Transport.vw_LiveVehicle
WHERE ElectionContextId = @ElectionContextId");

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                sql.Append(" AND EffectiveStatus = @Status");
            }

            if (!string.IsNullOrWhiteSpace(filter.VehicleTypeCode))
            {
                sql.Append(" AND VehicleTypeCode = @VehicleTypeCode");
            }

            if (filter.ProviderId.HasValue)
            {
                sql.Append(" AND ProviderId = @ProviderId");
            }

            if (filter.PollingStationId.HasValue)
            {
                sql.Append(" AND PollingStationId = @PollingStationId");
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                sql.Append(@" AND
(
    RegistrationNo LIKE @Search
    OR DriverName LIKE @Search
    OR DriverMobile LIKE @Search
    OR ProviderName LIKE @Search
    OR PollingStationName LIKE @Search
)");
            }

            sql.Append(" ORDER BY EffectiveIsOnline DESC, EffectiveStatus, RegistrationNo;");

            List<LiveVehicleDto> result = new List<LiveVehicleDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql.ToString(), connection))
            {
                AddParameter(command, "@ElectionContextId", filter.ElectionContextId, SqlDbType.BigInt);

                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    AddParameter(command, "@Status", filter.Status.Trim(), SqlDbType.NVarChar, 40);
                }

                if (!string.IsNullOrWhiteSpace(filter.VehicleTypeCode))
                {
                    AddParameter(command, "@VehicleTypeCode", filter.VehicleTypeCode.Trim(), SqlDbType.NVarChar, 30);
                }

                if (filter.ProviderId.HasValue)
                {
                    AddParameter(command, "@ProviderId", filter.ProviderId.Value, SqlDbType.BigInt);
                }

                if (filter.PollingStationId.HasValue)
                {
                    AddParameter(command, "@PollingStationId", filter.PollingStationId.Value, SqlDbType.BigInt);
                }

                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    AddParameter(command, "@Search", "%" + filter.Search.Trim() + "%", SqlDbType.NVarChar, 300);
                }

                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapLiveVehicle(reader));
                    }
                }
            }

            return result;
        }

        public async Task<IList<PollingStationOperationsDto>> GetPollingStationsAsync(long electionContextId)
        {
            const string sql = @"
SELECT *
FROM Transport.vw_PollingStationOperations
WHERE ElectionContextId = @ElectionContextId
ORDER BY Sr;";

            List<PollingStationOperationsDto> result = new List<PollingStationOperationsDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapPollingStation(reader));
                    }
                }
            }

            return result;
        }

        public async Task<IList<ProviderPerformanceDto>> GetProvidersAsync(long electionContextId)
        {
            const string sql = @"
SELECT *
FROM Transport.vw_ProviderPerformance
WHERE ElectionContextId = @ElectionContextId
ORDER BY EffectivenessScore DESC, ProviderName;";

            List<ProviderPerformanceDto> result = new List<ProviderPerformanceDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapProvider(reader));
                    }
                }
            }

            return result;
        }

        public async Task<IList<TransportRequestQueueDto>> GetRequestsAsync(long electionContextId, string status)
        {
            const string sql = @"
SELECT *
FROM Transport.vw_TransportRequestQueue
WHERE ElectionContextId = @ElectionContextId
  AND (@Status IS NULL OR RequestStatus = @Status)
ORDER BY Priority, RequestedAtUtc;";

            List<TransportRequestQueueDto> result = new List<TransportRequestQueueDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                AddParameter(command, "@Status", NormalizeStatus(status), SqlDbType.NVarChar, 40);
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

        public async Task<IList<TripDto>> GetTripsAsync(long electionContextId, int take)
        {
            take = Math.Max(1, Math.Min(5000, take));
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
INNER JOIN Transport.VehicleAssignment AS VA
    ON VA.VehicleAssignmentId = T.VehicleAssignmentId
INNER JOIN Transport.Vehicle AS V
    ON V.VehicleId = T.VehicleId
INNER JOIN Transport.VehicleType AS VT
    ON VT.VehicleTypeId = V.VehicleTypeId
INNER JOIN Transport.Driver AS D
    ON D.DriverId = VA.DriverId
LEFT JOIN Transport.Provider AS P
    ON P.ProviderId = VA.ProviderId
INNER JOIN Transport.PollingStation AS PS
    ON PS.PollingStationId = T.PollingStationId
WHERE VA.ElectionContextId = @ElectionContextId
ORDER BY COALESCE(T.CompletedAtUtc, T.ArrivedAtUtc, T.StartedAtUtc) DESC;";

            List<TripDto> result = new List<TripDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@Take", take, SqlDbType.Int);
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
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

        public async Task<IList<TransportExceptionDto>> GetExceptionsAsync(long electionContextId)
        {
            const string sql = @"
DECLARE @LongIdleMinutes INT =
(
    SELECT LongIdleMinutes
    FROM Transport.ElectionContext
    WHERE ElectionContextId = @ElectionContextId
);

SELECT
    ExceptionType =
        CASE
            WHEN LV.EffectiveStatus = N'NeverActivated' THEN N'Never Activated'
            WHEN LV.EffectiveStatus = N'Offline' THEN N'Offline'
            WHEN LV.EffectiveStatus = N'Idle'
             AND LV.LastSeenAtUtc IS NOT NULL
             AND LV.TodayTrips = 0 THEN N'Idle / No Trips'
            ELSE N'No Valid Trips'
        END,
    Severity =
        CASE
            WHEN LV.EffectiveStatus = N'NeverActivated' THEN N'Critical'
            WHEN LV.EffectiveStatus = N'Offline' THEN N'High'
            WHEN LV.TodayTrips = 0 THEN N'Medium'
            ELSE N'Low'
        END,
    LV.VehicleAssignmentId,
    LV.RegistrationNo,
    LV.VehicleType,
    LV.DriverName,
    LV.DriverMobile,
    LV.ProviderName,
    LV.PollingStationName,
    LV.EffectiveStatus,
    LV.LastSeenAtUtc,
    MinutesSinceLastSeen =
        CASE WHEN LV.LastSeenAtUtc IS NULL THEN NULL
             ELSE DATEDIFF(MINUTE, LV.LastSeenAtUtc, SYSUTCDATETIME()) END,
    LV.TodayTrips,
    Message =
        CASE
            WHEN LV.EffectiveStatus = N'NeverActivated'
                THEN N'Registered/promised vehicle has not transmitted any GPS location.'
            WHEN LV.EffectiveStatus = N'Offline'
                THEN N'Location feed is outside the configured online threshold.'
            WHEN LV.EffectiveStatus = N'Idle' AND LV.TodayTrips = 0
                THEN N'Vehicle is visible but has not completed a valid polling-station trip.'
            ELSE N'Operational vehicle has no valid trip recorded and requires review.'
        END
FROM Transport.vw_LiveVehicle AS LV
WHERE LV.ElectionContextId = @ElectionContextId
  AND
  (
      LV.EffectiveStatus IN (N'NeverActivated', N'Offline')
      OR (LV.TodayTrips = 0 AND LV.EffectiveStatus IN (N'Idle', N'Moving', N'Approaching', N'AtPollingStation'))
  )
ORDER BY
    CASE
        WHEN LV.EffectiveStatus = N'NeverActivated' THEN 1
        WHEN LV.EffectiveStatus = N'Offline' THEN 2
        ELSE 3
    END,
    LV.ProviderName,
    LV.RegistrationNo;";

            List<TransportExceptionDto> result = new List<TransportExceptionDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new TransportExceptionDto
                        {
                            ExceptionType = reader.GetStringSafe("ExceptionType"),
                            Severity = reader.GetStringSafe("Severity"),
                            VehicleAssignmentId = reader.GetInt64Safe("VehicleAssignmentId"),
                            RegistrationNo = reader.GetStringSafe("RegistrationNo"),
                            VehicleType = reader.GetStringSafe("VehicleType"),
                            DriverName = reader.GetStringSafe("DriverName"),
                            DriverMobile = reader.GetStringSafe("DriverMobile"),
                            ProviderName = reader.GetStringSafe("ProviderName"),
                            PollingStationName = reader.GetStringSafe("PollingStationName"),
                            EffectiveStatus = reader.GetStringSafe("EffectiveStatus"),
                            LastSeenAtUtc = reader.GetNullableDateTime("LastSeenAtUtc"),
                            MinutesSinceLastSeen = reader.GetNullableInt32("MinutesSinceLastSeen"),
                            TodayTrips = reader.GetInt32Safe("TodayTrips"),
                            Message = reader.GetStringSafe("Message")
                        });
                    }
                }
            }

            return result;
        }

        private async Task<TransportDashboardSummaryDto> GetDashboardSummaryAsync(long electionContextId)
        {
            const string sql = @"
SELECT
    PromisedVehicles = ISNULL
    (
        (SELECT SUM(PromisedQuantity)
         FROM Transport.ProviderCommitment
         WHERE ElectionContextId = @ElectionContextId
           AND CommitmentStatus <> N'Cancelled'), 0
    ),
    RegisteredVehicles =
    (
        SELECT COUNT(*)
        FROM Transport.VehicleAssignment
        WHERE ElectionContextId = @ElectionContextId
          AND IsActive = 1
    ),
    ActivatedVehicles =
    (
        SELECT COUNT(*)
        FROM Transport.vw_LiveVehicle
        WHERE ElectionContextId = @ElectionContextId
          AND LastSeenAtUtc IS NOT NULL
    ),
    ActiveVehicles =
    (
        SELECT COUNT(*)
        FROM Transport.vw_LiveVehicle
        WHERE ElectionContextId = @ElectionContextId
          AND EffectiveIsOnline = 1
    ),
    OfflineVehicles =
    (
        SELECT COUNT(*)
        FROM Transport.vw_LiveVehicle
        WHERE ElectionContextId = @ElectionContextId
          AND EffectiveStatus = N'Offline'
    ),
    NeverActivatedVehicles =
    (
        SELECT COUNT(*)
        FROM Transport.vw_LiveVehicle
        WHERE ElectionContextId = @ElectionContextId
          AND EffectiveStatus = N'NeverActivated'
    ),
    TripsCompleted =
    (
        SELECT COUNT(*)
        FROM Transport.Trip AS T
        INNER JOIN Transport.VehicleAssignment AS VA
            ON VA.VehicleAssignmentId = T.VehicleAssignmentId
        WHERE VA.ElectionContextId = @ElectionContextId
          AND T.ValidationStatus = N'VALID'
    ),
    DistanceKm = ISNULL
    (
        (SELECT SUM(T.DistanceKm)
         FROM Transport.Trip AS T
         INNER JOIN Transport.VehicleAssignment AS VA
            ON VA.VehicleAssignmentId = T.VehicleAssignmentId
         WHERE VA.ElectionContextId = @ElectionContextId
           AND T.ValidationStatus = N'VALID'), 0
    ),
    PollingStationsServed =
    (
        SELECT COUNT(DISTINCT T.PollingStationId)
        FROM Transport.Trip AS T
        INNER JOIN Transport.VehicleAssignment AS VA
            ON VA.VehicleAssignmentId = T.VehicleAssignmentId
        WHERE VA.ElectionContextId = @ElectionContextId
          AND T.ValidationStatus = N'VALID'
    ),
    TotalPollingStations =
    (
        SELECT COUNT(*)
        FROM Transport.PollingStation
        WHERE ElectionContextId = @ElectionContextId
          AND IsOperational = 1
    ),
    TotalRequests =
    (
        SELECT COUNT(*)
        FROM Transport.TransportRequest
        WHERE ElectionContextId = @ElectionContextId
    ),
    OpenRequests =
    (
        SELECT COUNT(*)
        FROM Transport.TransportRequest
        WHERE ElectionContextId = @ElectionContextId
          AND RequestStatus IN
              (N'NEW', N'ROUTING', N'OFFERED', N'ASSIGNED',
               N'DRIVER_EN_ROUTE', N'PICKED_UP', N'DROPPED_OFF')
    ),
    CompletedRequests =
    (
        SELECT COUNT(*)
        FROM Transport.TransportRequest
        WHERE ElectionContextId = @ElectionContextId
          AND RequestStatus = N'COMPLETED'
    ),
    ConfirmedPassengers = ISNULL
    (
        (SELECT SUM(T.ConfirmedPassengers)
         FROM Transport.Trip AS T
         INNER JOIN Transport.VehicleAssignment AS VA
            ON VA.VehicleAssignmentId = T.VehicleAssignmentId
         WHERE VA.ElectionContextId = @ElectionContextId
           AND T.ValidationStatus = N'VALID'), 0
    );";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return new TransportDashboardSummaryDto();
                    }

                    TransportDashboardSummaryDto result = new TransportDashboardSummaryDto
                    {
                        PromisedVehicles = reader.GetInt32Safe("PromisedVehicles"),
                        RegisteredVehicles = reader.GetInt32Safe("RegisteredVehicles"),
                        ActivatedVehicles = reader.GetInt32Safe("ActivatedVehicles"),
                        ActiveVehicles = reader.GetInt32Safe("ActiveVehicles"),
                        OfflineVehicles = reader.GetInt32Safe("OfflineVehicles"),
                        NeverActivatedVehicles = reader.GetInt32Safe("NeverActivatedVehicles"),
                        TripsCompleted = reader.GetInt32Safe("TripsCompleted"),
                        DistanceKm = reader.GetDecimalSafe("DistanceKm"),
                        PollingStationsServed = reader.GetInt32Safe("PollingStationsServed"),
                        TotalPollingStations = reader.GetInt32Safe("TotalPollingStations"),
                        TotalRequests = reader.GetInt32Safe("TotalRequests"),
                        OpenRequests = reader.GetInt32Safe("OpenRequests"),
                        CompletedRequests = reader.GetInt32Safe("CompletedRequests"),
                        ConfirmedPassengers = reader.GetInt32Safe("ConfirmedPassengers")
                    };

                    result.PromiseFulfilmentPercent = result.PromisedVehicles == 0
                        ? 0M
                        : Math.Round(result.ActiveVehicles * 100M / result.PromisedVehicles, 2);

                    result.PollingStationCoveragePercent = result.TotalPollingStations == 0
                        ? 0M
                        : Math.Round(result.PollingStationsServed * 100M / result.TotalPollingStations, 2);

                    return result;
                }
            }
        }

        private async Task<IList<TransportTimelinePointDto>> GetTimelineAsync(long electionContextId, int hours)
        {
            const string sql = @"
DECLARE @FromUtc DATETIME2(3) = DATEADD(HOUR, -@Hours, SYSUTCDATETIME());

WITH LocationBuckets AS
(
    SELECT
        BucketUtc = DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, L.RecordedAtUtc) / 15) * 15, 0),
        ActiveVehicles = COUNT(DISTINCT L.VehicleAssignmentId)
    FROM Transport.VehicleLocation AS L
    INNER JOIN Transport.VehicleAssignment AS VA
        ON VA.VehicleAssignmentId = L.VehicleAssignmentId
    WHERE VA.ElectionContextId = @ElectionContextId
      AND L.RecordedAtUtc >= @FromUtc
    GROUP BY DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, L.RecordedAtUtc) / 15) * 15, 0)
),
TripBuckets AS
(
    SELECT
        BucketUtc = DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, COALESCE(T.ArrivedAtUtc, T.CompletedAtUtc, T.StartedAtUtc)) / 15) * 15, 0),
        Trips = COUNT(*),
        DistanceKm = ISNULL(SUM(T.DistanceKm), 0)
    FROM Transport.Trip AS T
    INNER JOIN Transport.VehicleAssignment AS VA
        ON VA.VehicleAssignmentId = T.VehicleAssignmentId
    WHERE VA.ElectionContextId = @ElectionContextId
      AND COALESCE(T.ArrivedAtUtc, T.CompletedAtUtc, T.StartedAtUtc) >= @FromUtc
      AND T.ValidationStatus = N'VALID'
    GROUP BY DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, COALESCE(T.ArrivedAtUtc, T.CompletedAtUtc, T.StartedAtUtc)) / 15) * 15, 0)
),
RequestBuckets AS
(
    SELECT
        BucketUtc = DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, R.RequestedAtUtc) / 15) * 15, 0),
        Requests = COUNT(*)
    FROM Transport.TransportRequest AS R
    WHERE R.ElectionContextId = @ElectionContextId
      AND R.RequestedAtUtc >= @FromUtc
    GROUP BY DATEADD(MINUTE, (DATEDIFF(MINUTE, 0, R.RequestedAtUtc) / 15) * 15, 0)
),
AllBuckets AS
(
    SELECT BucketUtc FROM LocationBuckets
    UNION
    SELECT BucketUtc FROM TripBuckets
    UNION
    SELECT BucketUtc FROM RequestBuckets
)
SELECT
    B.BucketUtc,
    ActiveVehicles = ISNULL(L.ActiveVehicles, 0),
    Trips = ISNULL(T.Trips, 0),
    Requests = ISNULL(R.Requests, 0),
    DistanceKm = ISNULL(T.DistanceKm, 0)
FROM AllBuckets AS B
LEFT JOIN LocationBuckets AS L ON L.BucketUtc = B.BucketUtc
LEFT JOIN TripBuckets AS T ON T.BucketUtc = B.BucketUtc
LEFT JOIN RequestBuckets AS R ON R.BucketUtc = B.BucketUtc
ORDER BY B.BucketUtc;";

            List<TransportTimelinePointDto> result = new List<TransportTimelinePointDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                AddParameter(command, "@Hours", Math.Max(1, Math.Min(72, hours)), SqlDbType.Int);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new TransportTimelinePointDto
                        {
                            BucketUtc = reader.GetNullableDateTime("BucketUtc") ?? DateTime.MinValue,
                            ActiveVehicles = reader.GetInt32Safe("ActiveVehicles"),
                            Trips = reader.GetInt32Safe("Trips"),
                            Requests = reader.GetInt32Safe("Requests"),
                            DistanceKm = reader.GetDecimalSafe("DistanceKm")
                        });
                    }
                }
            }

            return result;
        }

        private async Task<IList<VehicleTypeCountDto>> GetVehicleTypeCountsAsync(long electionContextId)
        {
            const string sql = @"
SELECT
    VehicleTypeCode,
    VehicleType,
    IconKey,
    Total = COUNT(*),
    Active = SUM(CASE WHEN EffectiveIsOnline = 1 THEN 1 ELSE 0 END),
    Trips = SUM(TodayTrips)
FROM Transport.vw_LiveVehicle
WHERE ElectionContextId = @ElectionContextId
GROUP BY VehicleTypeCode, VehicleType, IconKey
ORDER BY Total DESC, VehicleType;";

            List<VehicleTypeCountDto> result = new List<VehicleTypeCountDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new VehicleTypeCountDto
                        {
                            VehicleTypeCode = reader.GetStringSafe("VehicleTypeCode"),
                            VehicleType = reader.GetStringSafe("VehicleType"),
                            IconKey = reader.GetStringSafe("IconKey"),
                            Total = reader.GetInt32Safe("Total"),
                            Active = reader.GetInt32Safe("Active"),
                            Trips = reader.GetInt32Safe("Trips")
                        });
                    }
                }
            }

            return result;
        }

        private static ElectionContextDto MapContext(SqlDataReader reader)
        {
            return new ElectionContextDto
            {
                ElectionContextId = reader.GetInt64Safe("ElectionContextId"),
                Election = reader.GetStringSafe("Election"),
                ElectionYear = Convert.ToInt16(reader.GetInt32Safe("ElectionYear")),
                Assembly = reader.GetStringSafe("Assembly"),
                Seat = reader.GetStringSafe("Seat"),
                ElectionDate = reader.GetNullableDateTime("ElectionDate"),
                IsDemoMode = reader.GetBooleanSafe("IsDemoMode"),
                GeofenceRadiusMeters = reader.GetInt32Safe("GeofenceRadiusMeters"),
                OfflineThresholdSeconds = reader.GetInt32Safe("OfflineThresholdSeconds")
            };
        }
    }
}
