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
    public partial class SqlTransportService
    {
        public async Task<PublicTransportRequestViewModel> BuildPublicRequestFormAsync(
            long electionContextId,
            long? pollingStationId,
            long? partyId,
            long? candidateId)
        {
            PublicTransportRequestViewModel model = new PublicTransportRequestViewModel
            {
                ElectionContextId = electionContextId,
                PollingStationId = pollingStationId,
                ServicePartyId = partyId,
                ServiceCandidateId = candidateId,
                PassengerCount = 1,
                PrivacyConsent = false
            };

            using (SqlConnection connection = CreateConnection())
            {
                await connection.OpenAsync();

                model.PollingStations = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), PollingStationId) AS Value,
       N'Sr ' + CONVERT(NVARCHAR(20), Sr) + N' — ' + COALESCE(PollingStationName, StationName) AS Text
FROM Transport.PollingStation
WHERE ElectionContextId = @ElectionContextId
  AND IsOperational = 1
ORDER BY Sr;", pollingStationId.HasValue ? pollingStationId.Value.ToString() : null,
                    delegate(SqlCommand command)
                    {
                        AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                    });

                const string poolSql = @"
SELECT
    ServicePoolName =
        CASE
            WHEN @CandidateId IS NOT NULL THEN
                (SELECT TOP (1) N'Candidate transport pool: ' + CandidateName
                 FROM Transport.Candidate
                 WHERE CandidateId = @CandidateId
                   AND ElectionContextId = @ElectionContextId)
            WHEN @PartyId IS NOT NULL THEN
                (SELECT TOP (1) N'Party transport pool: ' + PartyName
                 FROM Transport.Party
                 WHERE PartyId = @PartyId)
            ELSE N'Any eligible registered transport vehicle'
        END;";

                using (SqlCommand command = new SqlCommand(poolSql, connection))
                {
                    AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                    AddParameter(command, "@CandidateId", candidateId, SqlDbType.BigInt);
                    AddParameter(command, "@PartyId", partyId, SqlDbType.BigInt);
                    object value = await command.ExecuteScalarAsync();
                    model.ServicePoolName = value == null || value == DBNull.Value
                        ? "Any eligible registered transport vehicle"
                        : Convert.ToString(value);
                }
            }

            return model;
        }

        public async Task<PublicRequestConfirmationViewModel> CreatePublicRequestAsync(
            PublicTransportRequestViewModel model,
            string createdBy)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            DateTime? requestedPickupAtUtc = model.RequestedPickupLocal.HasValue
                ? (DateTime?)ToUtcFromPakistan(model.RequestedPickupLocal.Value)
                : null;

            long requestId;
            string requestNo;
            string requestStatus;
            string pollingStationName;

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_CreateTransportRequest", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                AddParameter(command, "@PollingStationId", model.PollingStationId, SqlDbType.BigInt);
                AddParameter(command, "@ServicePartyId", model.ServicePartyId, SqlDbType.BigInt);
                AddParameter(command, "@ServiceCandidateId", model.ServiceCandidateId, SqlDbType.BigInt);
                AddParameter(command, "@RequestedByName", model.RequestedByName, SqlDbType.NVarChar, 250);
                AddParameter(command, "@Mobile", model.Mobile, SqlDbType.NVarChar, 30);
                AddParameter(command, "@AlternateMobile", model.AlternateMobile, SqlDbType.NVarChar, 30);
                AddParameter(command, "@PickupAddress", model.PickupAddress, SqlDbType.NVarChar, 1000);
                AddParameter(command, "@PickupArea", model.PickupArea, SqlDbType.NVarChar, 300);
                AddParameter(command, "@Latitude", model.Latitude, SqlDbType.Float);
                AddParameter(command, "@Longitude", model.Longitude, SqlDbType.Float);
                AddParameter(command, "@PassengerCount", model.PassengerCount, SqlDbType.SmallInt);
                AddParameter(command, "@AccessibilityCategory", model.AccessibilityCategory, SqlDbType.NVarChar, 50);
                AddParameter(command, "@RequiresWheelchair", model.RequiresWheelchair, SqlDbType.Bit);
                AddParameter(command, "@RequiresAttendant", model.RequiresAttendant, SqlDbType.Bit);
                AddParameter(command, "@IsRoundTripRequired", model.IsRoundTripRequired, SqlDbType.Bit);
                AddParameter(command, "@RequestedPickupAtUtc", requestedPickupAtUtc, SqlDbType.DateTime2);
                AddParameter(command, "@SourceChannel", "Web", SqlDbType.NVarChar, 30);
                AddParameter(command, "@Notes", model.Notes, SqlDbType.NVarChar, 1000);
                AddParameter(command, "@PrivacyConsent", model.PrivacyConsent, SqlDbType.Bit);
                AddParameter(command, "@CreatedBy", createdBy, SqlDbType.NVarChar, 100);

                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        throw new InvalidOperationException("Transport request was not created.");
                    }

                    requestId = reader.GetInt64Safe("TransportRequestId");
                    requestNo = reader.GetStringSafe("RequestNo");
                    requestStatus = reader.GetStringSafe("RequestStatus");
                }
            }

            try
            {
                IList<DispatchOfferDto> offers = await RouteRequestAsync(requestId, 5, "Automatic Router");
                requestStatus = offers.Count > 0 ? "OFFERED" : "NO_VEHICLE";
            }
            catch
            {
                // The request remains saved even if routing is temporarily unavailable.
            }

            const string stationSql = @"
SELECT PollingStationName
FROM Transport.PollingStation
WHERE PollingStationId = @PollingStationId;";

            pollingStationName = null;
            if (model.PollingStationId.HasValue)
            {
                using (SqlConnection connection = CreateConnection())
                using (SqlCommand command = new SqlCommand(stationSql, connection))
                {
                    AddParameter(command, "@PollingStationId", model.PollingStationId.Value, SqlDbType.BigInt);
                    await connection.OpenAsync();
                    object value = await command.ExecuteScalarAsync();
                    pollingStationName = value == null || value == DBNull.Value ? null : Convert.ToString(value);
                }
            }

            return new PublicRequestConfirmationViewModel
            {
                RequestNo = requestNo,
                RequestStatus = requestStatus,
                PollingStationName = pollingStationName,
                Message = requestStatus == "OFFERED"
                    ? "Your request has been offered to nearby eligible drivers. Keep the request number for status tracking."
                    : "Your request has been received. No eligible vehicle is immediately available, but the coordination team can retry routing."
            };
        }

        public async Task<IList<DispatchOfferDto>> RouteRequestAsync(
            long transportRequestId,
            int offerCount,
            string changedBy)
        {
            List<DispatchOfferDto> result = new List<DispatchOfferDto>();
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_RouteTransportRequest", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddParameter(command, "@TransportRequestId", transportRequestId, SqlDbType.BigInt);
                AddParameter(command, "@OfferCount", Math.Max(1, Math.Min(20, offerCount)), SqlDbType.Int);
                AddParameter(command, "@OfferSeconds", 60, SqlDbType.Int);
                AddParameter(command, "@ChangedBy", changedBy, SqlDbType.NVarChar, 100);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(MapDispatch(reader));
                    }
                }
            }

            return result;
        }

        public async Task AcceptDispatchAsync(long requestDispatchId, string changedBy)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_AcceptTransportDispatch", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddParameter(command, "@RequestDispatchId", requestDispatchId, SqlDbType.BigInt);
                AddParameter(command, "@ChangedBy", changedBy, SqlDbType.NVarChar, 100);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateRequestStatusAsync(
            long transportRequestId,
            string newStatus,
            string remarks,
            string changedBy)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_UpdateTransportRequestStatus", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddParameter(command, "@TransportRequestId", transportRequestId, SqlDbType.BigInt);
                AddParameter(command, "@NewStatus", newStatus, SqlDbType.NVarChar, 40);
                AddParameter(command, "@Remarks", remarks, SqlDbType.NVarChar, 1000);
                AddParameter(command, "@ChangedBy", changedBy, SqlDbType.NVarChar, 100);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<LocationPushResultDto> RecordLocationByVehicleCodeAsync(LocationPushInputModel input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            long? assignmentId = await ResolveAssignmentByVehicleCodeAsync(input.VehicleAppCode);
            if (!assignmentId.HasValue)
            {
                throw new InvalidOperationException("No active assignment is linked to the supplied vehicle application code.");
            }

            return await RecordLocationAsync(
                assignmentId.Value,
                input.Latitude,
                input.Longitude,
                input.RecordedAtUtc,
                input.SpeedKph,
                input.HeadingDegrees,
                input.AccuracyMeters,
                input.BatteryPercent,
                input.NetworkType,
                "Driver App",
                input.IsMockLocation);
        }

        public async Task SeedDemoDataAsync(long electionContextId, int vehicleCount, int requestCount)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_SeedDemoData", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 180;
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                AddParameter(command, "@VehicleCount", vehicleCount, SqlDbType.Int);
                AddParameter(command, "@RequestCount", requestCount, SqlDbType.Int);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task RefreshOfflineStatesAsync(long? electionContextId)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_RefreshOfflineStates", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        internal async Task<LocationPushResultDto> RecordLocationAsync(
            long vehicleAssignmentId,
            double latitude,
            double longitude,
            DateTime? recordedAtUtc,
            decimal? speedKph,
            decimal? headingDegrees,
            decimal? accuracyMeters,
            byte? batteryPercent,
            string networkType,
            string source,
            bool isMockLocation)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand("Transport.usp_RecordVehicleLocation", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId, SqlDbType.BigInt);
                AddParameter(command, "@Latitude", latitude, SqlDbType.Float);
                AddParameter(command, "@Longitude", longitude, SqlDbType.Float);
                AddParameter(command, "@RecordedAtUtc", recordedAtUtc, SqlDbType.DateTime2);
                AddParameter(command, "@SpeedKph", speedKph, SqlDbType.Decimal);
                command.Parameters["@SpeedKph"].Precision = 9;
                command.Parameters["@SpeedKph"].Scale = 2;
                AddParameter(command, "@HeadingDegrees", headingDegrees, SqlDbType.Decimal);
                command.Parameters["@HeadingDegrees"].Precision = 6;
                command.Parameters["@HeadingDegrees"].Scale = 2;
                AddParameter(command, "@AccuracyMeters", accuracyMeters, SqlDbType.Decimal);
                command.Parameters["@AccuracyMeters"].Precision = 9;
                command.Parameters["@AccuracyMeters"].Scale = 2;
                AddParameter(command, "@BatteryPercent", batteryPercent, SqlDbType.TinyInt);
                AddParameter(command, "@NetworkType", networkType, SqlDbType.NVarChar, 30);
                AddParameter(command, "@LocationSource", source, SqlDbType.NVarChar, 30);
                AddParameter(command, "@IsMockLocation", isMockLocation, SqlDbType.Bit);
                await connection.OpenAsync();
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        throw new InvalidOperationException("Location was not recorded.");
                    }

                    return new LocationPushResultDto
                    {
                        VehicleLocationId = reader.GetInt64Safe("VehicleLocationId"),
                        VehicleAssignmentId = reader.GetInt64Safe("VehicleAssignmentId"),
                        VehicleId = reader.GetInt64Safe("VehicleId"),
                        CurrentStatus = reader.GetStringSafe("CurrentStatus"),
                        DistanceToStationMeters = reader.GetNullableDecimal("DistanceToStationMeters"),
                        SegmentDistanceKm = reader.GetNullableDecimal("SegmentDistanceKm"),
                        IsInsidePollingStationBuffer = reader.GetBooleanSafe("IsInsidePollingStationBuffer"),
                        NewTripId = reader.GetNullableInt64("NewTripId")
                    };
                }
            }
        }

        private async Task<long?> ResolveAssignmentByVehicleCodeAsync(Guid vehicleAppCode)
        {
            const string sql = @"
SELECT TOP (1) VA.VehicleAssignmentId
FROM Transport.Vehicle AS V
INNER JOIN Transport.VehicleAssignment AS VA
    ON VA.VehicleId = V.VehicleId
WHERE V.DriverAppVehicleCode = @VehicleAppCode
  AND V.IsActive = 1
  AND VA.IsActive = 1
ORDER BY VA.VehicleAssignmentId DESC;";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                AddParameter(command, "@VehicleAppCode", vehicleAppCode, SqlDbType.UniqueIdentifier);
                await connection.OpenAsync();
                object value = await command.ExecuteScalarAsync();
                return value == null || value == DBNull.Value ? (long?)null : Convert.ToInt64(value);
            }
        }
    }
}
