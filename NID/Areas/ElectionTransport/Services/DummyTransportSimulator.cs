using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using NID.Areas.ElectionTransport.Infrastructure;
using NID.Areas.ElectionTransport.Models;

namespace NID.Areas.ElectionTransport.Services
{
    public sealed class DummyTransportSimulator : IDummyTransportSimulator
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private readonly ITransportConnectionFactory _connectionFactory;
        private readonly SqlTransportService _transportService;

        public DummyTransportSimulator()
            : this(new TransportConnectionFactory())
        {
        }

        public DummyTransportSimulator(ITransportConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _transportService = new SqlTransportService(connectionFactory);
        }

        public async Task<int> EnsureRoutesAsync(long electionContextId)
        {
            List<RouteDefinition> routes = new List<RouteDefinition>();

            const string selectSql = @"
SELECT
    R.DummyRouteId,
    R.StopAtPickupSeconds,
    R.StopAtStationSeconds,
    StationLatitude = PS.Latitude,
    StationLongitude = PS.Longitude,
    PickupLatitude = COALESCE(PL.Latitude, PS.Latitude + 0.0060),
    PickupLongitude = COALESCE(PL.Longitude, PS.Longitude + 0.0060)
FROM Transport.DummyRoute AS R
INNER JOIN Transport.PollingStation AS PS
    ON PS.PollingStationId = R.PollingStationId
LEFT JOIN Transport.PickupLocation AS PL
    ON PL.PickupLocationId = R.PickupLocationId
WHERE R.ElectionContextId = @ElectionContextId
  AND R.IsActive = 1
  AND PS.Latitude IS NOT NULL
  AND PS.Longitude IS NOT NULL
  AND NOT EXISTS
      (SELECT 1 FROM Transport.DummyRoutePoint AS RP
       WHERE RP.DummyRouteId = R.DummyRouteId);";

            using (SqlConnection connection = _connectionFactory.Create())
            using (SqlCommand command = new SqlCommand(selectSql, connection))
            {
                command.Parameters.Add("@ElectionContextId", SqlDbType.BigInt).Value = electionContextId;
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        routes.Add(new RouteDefinition
                        {
                            DummyRouteId = Convert.ToInt64(reader["DummyRouteId"]),
                            StopAtPickupSeconds = Convert.ToInt32(reader["StopAtPickupSeconds"]),
                            StopAtStationSeconds = Convert.ToInt32(reader["StopAtStationSeconds"]),
                            StationLatitude = Convert.ToDouble(reader["StationLatitude"]),
                            StationLongitude = Convert.ToDouble(reader["StationLongitude"]),
                            PickupLatitude = Convert.ToDouble(reader["PickupLatitude"]),
                            PickupLongitude = Convert.ToDouble(reader["PickupLongitude"])
                        });
                    }
                }
            }

            int prepared = 0;
            foreach (RouteDefinition route in routes)
            {
                await InsertRoutePointsAsync(route);
                prepared++;
            }

            return prepared;
        }

        public async Task<SimulationTickResultDto> TickAsync(long electionContextId)
        {
            await Gate.WaitAsync();
            try
            {
                int prepared = await EnsureRoutesAsync(electionContextId);
                List<RouteTick> routes = await LoadTickRoutesAsync(electionContextId);
                int moved = 0;
                int recorded = 0;

                foreach (RouteTick route in routes)
                {
                    TickDecision decision = DecideNextPoint(route);
                    if (decision == null)
                    {
                        continue;
                    }

                    await _transportService.RecordLocationAsync(
                        route.VehicleAssignmentId,
                        decision.Latitude,
                        decision.Longitude,
                        DateTime.UtcNow,
                        decision.SpeedKph,
                        decision.HeadingDegrees,
                        7.5M,
                        80,
                        "4G",
                        "Dummy Simulator",
                        true);

                    recorded++;
                    if (decision.PointSequence != route.CurrentPointSequence)
                    {
                        moved++;
                    }

                    await UpdateRouteStateAsync(route, decision);
                }

                return new SimulationTickResultDto
                {
                    RoutesPrepared = prepared,
                    VehiclesMoved = moved,
                    LocationsRecorded = recorded,
                    TickUtc = DateTime.UtcNow
                };
            }
            finally
            {
                Gate.Release();
            }
        }

        private async Task InsertRoutePointsAsync(RouteDefinition route)
        {
            const int pointCount = 31;
            double deltaLat = route.StationLatitude - route.PickupLatitude;
            double deltaLon = route.StationLongitude - route.PickupLongitude;
            double length = Math.Sqrt(deltaLat * deltaLat + deltaLon * deltaLon);
            double normalLat = length > 0 ? -deltaLon / length : 0;
            double normalLon = length > 0 ? deltaLat / length : 0;

            using (SqlConnection connection = _connectionFactory.Create())
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        for (int i = 0; i < pointCount; i++)
                        {
                            double fraction = i / (double)(pointCount - 1);
                            double curve = Math.Sin(fraction * Math.PI) * 0.00028;
                            double latitude = route.PickupLatitude + deltaLat * fraction + normalLat * curve;
                            double longitude = route.PickupLongitude + deltaLon * fraction + normalLon * curve;
                            string pointType = i == 0 ? "PICKUP" : i == pointCount - 1 ? "STATION" : "ROUTE";
                            int holdSeconds = i == 0
                                ? route.StopAtPickupSeconds
                                : i == pointCount - 1
                                    ? route.StopAtStationSeconds
                                    : 0;

                            const string insertSql = @"
INSERT INTO Transport.DummyRoutePoint
(
    DummyRouteId, PointSequence,
    Latitude, Longitude, GeoPoint,
    PointType, HoldSeconds
)
VALUES
(
    @DummyRouteId, @PointSequence,
    @Latitude, @Longitude,
    GEOGRAPHY::Point(@Latitude, @Longitude, 4326),
    @PointType, @HoldSeconds
);";

                            using (SqlCommand command = new SqlCommand(insertSql, connection, transaction))
                            {
                                command.Parameters.Add("@DummyRouteId", SqlDbType.BigInt).Value = route.DummyRouteId;
                                command.Parameters.Add("@PointSequence", SqlDbType.Int).Value = i + 1;
                                command.Parameters.Add("@Latitude", SqlDbType.Float).Value = latitude;
                                command.Parameters.Add("@Longitude", SqlDbType.Float).Value = longitude;
                                command.Parameters.Add("@PointType", SqlDbType.NVarChar, 30).Value = pointType;
                                command.Parameters.Add("@HoldSeconds", SqlDbType.Int).Value = holdSeconds;
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private async Task<List<RouteTick>> LoadTickRoutesAsync(long electionContextId)
        {
            const string sql = @"
SELECT
    R.DummyRouteId,
    R.VehicleAssignmentId,
    R.CurrentPointSequence,
    R.DirectionFlag,
    R.TargetSpeedKph,
    R.LastTickAtUtc,
    MaximumSequence = MAX(RP.PointSequence)
FROM Transport.DummyRoute AS R
INNER JOIN Transport.DummyRoutePoint AS RP
    ON RP.DummyRouteId = R.DummyRouteId
INNER JOIN Transport.VehicleAssignment AS VA
    ON VA.VehicleAssignmentId = R.VehicleAssignmentId
LEFT JOIN Transport.VehicleLiveState AS LS
    ON LS.VehicleAssignmentId = R.VehicleAssignmentId
WHERE R.ElectionContextId = @ElectionContextId
  AND R.IsActive = 1
  AND VA.IsActive = 1
  AND VA.AssignmentStatus = N'Operational'
  AND ISNULL(LS.CurrentStatus, N'Moving') NOT IN
      (N'NeverActivated', N'Offline', N'Completed', N'Suspended')
  AND
  (
      R.LastTickAtUtc IS NULL
      OR DATEDIFF(SECOND, R.LastTickAtUtc, SYSUTCDATETIME()) >= 2
  )
GROUP BY
    R.DummyRouteId,
    R.VehicleAssignmentId,
    R.CurrentPointSequence,
    R.DirectionFlag,
    R.TargetSpeedKph,
    R.LastTickAtUtc
ORDER BY R.DummyRouteId;";

            List<RouteTick> result = new List<RouteTick>();
            using (SqlConnection connection = _connectionFactory.Create())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ElectionContextId", SqlDbType.BigInt).Value = electionContextId;
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        RouteTick route = new RouteTick
                        {
                            DummyRouteId = Convert.ToInt64(reader["DummyRouteId"]),
                            VehicleAssignmentId = Convert.ToInt64(reader["VehicleAssignmentId"]),
                            CurrentPointSequence = Convert.ToInt32(reader["CurrentPointSequence"]),
                            DirectionFlag = Convert.ToInt32(reader["DirectionFlag"]),
                            TargetSpeedKph = Convert.ToDecimal(reader["TargetSpeedKph"]),
                            LastTickAtUtc = reader["LastTickAtUtc"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(reader["LastTickAtUtc"]),
                            MaximumSequence = Convert.ToInt32(reader["MaximumSequence"])
                        };
                        result.Add(route);
                    }
                }
            }

            foreach (RouteTick route in result)
            {
                route.Points = await LoadRoutePointsAsync(route.DummyRouteId);
            }

            return result;
        }

        private async Task<Dictionary<int, RoutePoint>> LoadRoutePointsAsync(long dummyRouteId)
        {
            const string sql = @"
SELECT PointSequence, Latitude, Longitude, PointType, HoldSeconds
FROM Transport.DummyRoutePoint
WHERE DummyRouteId = @DummyRouteId
ORDER BY PointSequence;";

            Dictionary<int, RoutePoint> result = new Dictionary<int, RoutePoint>();
            using (SqlConnection connection = _connectionFactory.Create())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@DummyRouteId", SqlDbType.BigInt).Value = dummyRouteId;
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        RoutePoint point = new RoutePoint
                        {
                            PointSequence = Convert.ToInt32(reader["PointSequence"]),
                            Latitude = Convert.ToDouble(reader["Latitude"]),
                            Longitude = Convert.ToDouble(reader["Longitude"]),
                            PointType = Convert.ToString(reader["PointType"]),
                            HoldSeconds = Convert.ToInt32(reader["HoldSeconds"])
                        };
                        result[point.PointSequence] = point;
                    }
                }
            }

            return result;
        }

        private static TickDecision DecideNextPoint(RouteTick route)
        {
            if (route.Points == null || route.Points.Count == 0)
            {
                return null;
            }

            RoutePoint current;
            if (!route.Points.TryGetValue(route.CurrentPointSequence, out current))
            {
                current = route.Points[1];
                route.CurrentPointSequence = 1;
            }

            DateTime now = DateTime.UtcNow;
            bool firstTick = !route.LastTickAtUtc.HasValue;
            bool isEndpoint = current.PointSequence == 1 || current.PointSequence == route.MaximumSequence;

            if (!firstTick && isEndpoint && current.HoldSeconds > 0
                && (now - route.LastTickAtUtc.Value).TotalSeconds < current.HoldSeconds)
            {
                return new TickDecision
                {
                    PointSequence = current.PointSequence,
                    DirectionFlag = route.DirectionFlag,
                    Latitude = current.Latitude,
                    Longitude = current.Longitude,
                    SpeedKph = 0M,
                    HeadingDegrees = null,
                    UpdateLastTick = false
                };
            }

            int direction = route.DirectionFlag == 0 ? 1 : route.DirectionFlag;
            if (current.PointSequence >= route.MaximumSequence)
            {
                direction = -1;
            }
            else if (current.PointSequence <= 1)
            {
                direction = 1;
            }

            int nextSequence = current.PointSequence + direction;
            if (!route.Points.ContainsKey(nextSequence))
            {
                nextSequence = current.PointSequence;
            }

            RoutePoint next = route.Points[nextSequence];
            return new TickDecision
            {
                PointSequence = nextSequence,
                DirectionFlag = direction,
                Latitude = next.Latitude,
                Longitude = next.Longitude,
                SpeedKph = next.PointType == "ROUTE" ? route.TargetSpeedKph : 3M,
                HeadingDegrees = CalculateHeading(current.Latitude, current.Longitude, next.Latitude, next.Longitude),
                UpdateLastTick = true
            };
        }

        private async Task UpdateRouteStateAsync(RouteTick route, TickDecision decision)
        {
            const string sql = @"
UPDATE Transport.DummyRoute
SET CurrentPointSequence = @PointSequence,
    DirectionFlag = @DirectionFlag,
    LastTickAtUtc = CASE WHEN @UpdateLastTick = 1
                         THEN SYSUTCDATETIME()
                         ELSE LastTickAtUtc END
WHERE DummyRouteId = @DummyRouteId;";

            using (SqlConnection connection = _connectionFactory.Create())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@PointSequence", SqlDbType.Int).Value = decision.PointSequence;
                command.Parameters.Add("@DirectionFlag", SqlDbType.SmallInt).Value = decision.DirectionFlag;
                command.Parameters.Add("@UpdateLastTick", SqlDbType.Bit).Value = decision.UpdateLastTick;
                command.Parameters.Add("@DummyRouteId", SqlDbType.BigInt).Value = route.DummyRouteId;
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        private static decimal? CalculateHeading(double lat1, double lon1, double lat2, double lon2)
        {
            if (Math.Abs(lat1 - lat2) < 0.0000001 && Math.Abs(lon1 - lon2) < 0.0000001)
            {
                return null;
            }

            double phi1 = lat1 * Math.PI / 180.0;
            double phi2 = lat2 * Math.PI / 180.0;
            double deltaLon = (lon2 - lon1) * Math.PI / 180.0;
            double y = Math.Sin(deltaLon) * Math.Cos(phi2);
            double x = Math.Cos(phi1) * Math.Sin(phi2)
                       - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(deltaLon);
            double bearing = (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;
            return Convert.ToDecimal(Math.Round(bearing, 2));
        }

        private sealed class RouteDefinition
        {
            public long DummyRouteId { get; set; }
            public int StopAtPickupSeconds { get; set; }
            public int StopAtStationSeconds { get; set; }
            public double StationLatitude { get; set; }
            public double StationLongitude { get; set; }
            public double PickupLatitude { get; set; }
            public double PickupLongitude { get; set; }
        }

        private sealed class RouteTick
        {
            public long DummyRouteId { get; set; }
            public long VehicleAssignmentId { get; set; }
            public int CurrentPointSequence { get; set; }
            public int DirectionFlag { get; set; }
            public decimal TargetSpeedKph { get; set; }
            public DateTime? LastTickAtUtc { get; set; }
            public int MaximumSequence { get; set; }
            public Dictionary<int, RoutePoint> Points { get; set; }
        }

        private sealed class RoutePoint
        {
            public int PointSequence { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string PointType { get; set; }
            public int HoldSeconds { get; set; }
        }

        private sealed class TickDecision
        {
            public int PointSequence { get; set; }
            public int DirectionFlag { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public decimal? SpeedKph { get; set; }
            public decimal? HeadingDegrees { get; set; }
            public bool UpdateLastTick { get; set; }
        }
    }
}
