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
        public async Task<VehicleManageViewModel> BuildVehicleFormAsync(long electionContextId, long? vehicleAssignmentId)
        {
            VehicleManageViewModel model = new VehicleManageViewModel
            {
                ElectionContextId = electionContextId,
                VehicleAssignmentId = vehicleAssignmentId,
                SeatingCapacity = 4,
                AssignmentStatus = "Registered",
                MaxServiceRadiusKm = 10M,
                IsActive = true
            };

            using (SqlConnection connection = CreateConnection())
            {
                await connection.OpenAsync();

                if (vehicleAssignmentId.HasValue)
                {
                    const string detailSql = @"
SELECT
    VA.VehicleAssignmentId,
    VA.ElectionContextId,
    V.VehicleTypeId,
    V.RegistrationNo,
    V.DisplayName,
    V.Make,
    V.Model,
    V.ModelYear,
    V.Color,
    V.SeatingCapacity,
    V.OwnerName,
    V.OwnerMobile,
    D.DriverName,
    D.Mobile AS DriverMobile,
    D.Address AS DriverAddress,
    D.DrivingLicenseNo,
    VA.ProviderId,
    VA.CandidateId,
    VA.PartyId,
    VA.AssignedPollingStationId,
    VA.AssignmentStatus,
    VA.MaxServiceRadiusKm,
    VA.Remarks,
    VA.IsActive
FROM Transport.VehicleAssignment AS VA
INNER JOIN Transport.Vehicle AS V ON V.VehicleId = VA.VehicleId
INNER JOIN Transport.Driver AS D ON D.DriverId = VA.DriverId
WHERE VA.VehicleAssignmentId = @VehicleAssignmentId;";

                    using (SqlCommand command = new SqlCommand(detailSql, connection))
                    {
                        AddParameter(command, "@VehicleAssignmentId", vehicleAssignmentId.Value, SqlDbType.BigInt);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model.ElectionContextId = reader.GetInt64Safe("ElectionContextId");
                                model.VehicleTypeId = Convert.ToInt16(reader.GetInt32Safe("VehicleTypeId"));
                                model.RegistrationNo = reader.GetStringSafe("RegistrationNo");
                                model.DisplayName = reader.GetStringSafe("DisplayName");
                                model.Make = reader.GetStringSafe("Make");
                                model.Model = reader.GetStringSafe("Model");
                                int? year = reader.GetNullableInt32("ModelYear");
                                model.ModelYear = year.HasValue ? (short?)year.Value : null;
                                model.Color = reader.GetStringSafe("Color");
                                model.SeatingCapacity = Convert.ToInt16(reader.GetInt32Safe("SeatingCapacity"));
                                model.OwnerName = reader.GetStringSafe("OwnerName");
                                model.OwnerMobile = reader.GetStringSafe("OwnerMobile");
                                model.DriverName = reader.GetStringSafe("DriverName");
                                model.DriverMobile = reader.GetStringSafe("DriverMobile");
                                model.DriverAddress = reader.GetStringSafe("DriverAddress");
                                model.DrivingLicenseNo = reader.GetStringSafe("DrivingLicenseNo");
                                model.ProviderId = reader.GetNullableInt64("ProviderId");
                                model.CandidateId = reader.GetNullableInt64("CandidateId");
                                model.PartyId = reader.GetNullableInt64("PartyId");
                                model.AssignedPollingStationId = reader.GetNullableInt64("AssignedPollingStationId");
                                model.AssignmentStatus = reader.GetStringSafe("AssignmentStatus");
                                model.MaxServiceRadiusKm = reader.GetNullableDecimal("MaxServiceRadiusKm");
                                model.Remarks = reader.GetStringSafe("Remarks");
                                model.IsActive = reader.GetBooleanSafe("IsActive");
                            }
                        }
                    }
                }

                model.VehicleTypes = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), VehicleTypeId) AS Value,
       Name + N' (' + CONVERT(NVARCHAR(10), DefaultCapacity) + N' seats)' AS Text
FROM Transport.VehicleType
WHERE IsActive = 1
ORDER BY SortOrder, Name;", model.VehicleTypeId == 0 ? null : model.VehicleTypeId.ToString());

                model.PollingStations = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), PollingStationId) AS Value,
       N'Sr ' + CONVERT(NVARCHAR(20), Sr) + N' — ' + COALESCE(PollingStationName, StationName) AS Text
FROM Transport.PollingStation
WHERE ElectionContextId = @ElectionContextId
  AND IsOperational = 1
ORDER BY Sr;", model.AssignedPollingStationId.HasValue ? model.AssignedPollingStationId.Value.ToString() : null,
                    delegate(SqlCommand command)
                    {
                        AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                    });

                model.Providers = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), ProviderId) AS Value,
       ProviderName + N' — ' + ProviderType AS Text
FROM Transport.Provider
WHERE ElectionContextId = @ElectionContextId
  AND IsActive = 1
ORDER BY ProviderName;", model.ProviderId.HasValue ? model.ProviderId.Value.ToString() : null,
                    delegate(SqlCommand command)
                    {
                        AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                    });

                model.Candidates = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), CandidateId) AS Value, CandidateName AS Text
FROM Transport.Candidate
WHERE ElectionContextId = @ElectionContextId
  AND IsActive = 1
ORDER BY CandidateName;", model.CandidateId.HasValue ? model.CandidateId.Value.ToString() : null,
                    delegate(SqlCommand command)
                    {
                        AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                    });

                model.Parties = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), PartyId) AS Value,
       PartyName + COALESCE(N' (' + Abbreviation + N')', N'') AS Text
FROM Transport.Party
WHERE IsActive = 1
ORDER BY PartyName;", model.PartyId.HasValue ? model.PartyId.Value.ToString() : null);
            }

            return model;
        }

        public async Task<long> SaveVehicleAsync(VehicleManageViewModel model, string userName)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            using (SqlConnection connection = CreateConnection())
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        long driverId;
                        long vehicleId;
                        long assignmentId;

                        if (model.VehicleAssignmentId.HasValue)
                        {
                            const string idsSql = @"
SELECT VA.VehicleId, VA.DriverId
FROM Transport.VehicleAssignment AS VA
WHERE VA.VehicleAssignmentId = @VehicleAssignmentId;";

                            using (SqlCommand command = new SqlCommand(idsSql, connection, transaction))
                            {
                                AddParameter(command, "@VehicleAssignmentId", model.VehicleAssignmentId.Value, SqlDbType.BigInt);
                                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                    {
                                        throw new InvalidOperationException("Vehicle assignment was not found.");
                                    }

                                    vehicleId = reader.GetInt64Safe("VehicleId");
                                    driverId = reader.GetInt64Safe("DriverId");
                                }
                            }

                            const string driverUpdate = @"
UPDATE Transport.Driver
SET DriverName = @DriverName,
    Mobile = @DriverMobile,
    Address = @DriverAddress,
    DrivingLicenseNo = @DrivingLicenseNo,
    ConsentGiven = 1,
    IsActive = 1
WHERE DriverId = @DriverId;";

                            using (SqlCommand command = new SqlCommand(driverUpdate, connection, transaction))
                            {
                                AddDriverParameters(command, model);
                                AddParameter(command, "@DriverId", driverId, SqlDbType.BigInt);
                                await command.ExecuteNonQueryAsync();
                            }

                            const string vehicleUpdate = @"
IF EXISTS
(
    SELECT 1
    FROM Transport.Vehicle
    WHERE RegistrationNoNormalized = UPPER(REPLACE(REPLACE(@RegistrationNo, N'-', N''), N' ', N''))
      AND VehicleId <> @VehicleId
)
    THROW 52001, 'Another vehicle already uses this registration number.', 1;

UPDATE Transport.Vehicle
SET VehicleTypeId = @VehicleTypeId,
    RegistrationNo = @RegistrationNo,
    DisplayName = @DisplayName,
    Make = @Make,
    Model = @Model,
    ModelYear = @ModelYear,
    Color = @Color,
    SeatingCapacity = @SeatingCapacity,
    OwnerName = @OwnerName,
    OwnerMobile = @OwnerMobile,
    VerificationStatus = N'Verified',
    IsActive = @IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE VehicleId = @VehicleId;";

                            using (SqlCommand command = new SqlCommand(vehicleUpdate, connection, transaction))
                            {
                                AddVehicleParameters(command, model);
                                AddParameter(command, "@VehicleId", vehicleId, SqlDbType.BigInt);
                                await command.ExecuteNonQueryAsync();
                            }

                            const string assignmentUpdate = @"
UPDATE Transport.VehicleAssignment
SET ElectionContextId = @ElectionContextId,
    ProviderId = @ProviderId,
    CandidateId = @CandidateId,
    PartyId = @PartyId,
    AssignedPollingStationId = @AssignedPollingStationId,
    AssignmentStatus = @AssignmentStatus,
    MaxServiceRadiusKm = @MaxServiceRadiusKm,
    Remarks = @Remarks,
    IsActive = @IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE VehicleAssignmentId = @VehicleAssignmentId;";

                            using (SqlCommand command = new SqlCommand(assignmentUpdate, connection, transaction))
                            {
                                AddAssignmentParameters(command, model);
                                AddParameter(command, "@VehicleAssignmentId", model.VehicleAssignmentId.Value, SqlDbType.BigInt);
                                await command.ExecuteNonQueryAsync();
                            }

                            assignmentId = model.VehicleAssignmentId.Value;
                        }
                        else
                        {
                            const string driverInsert = @"
SELECT @ExistingDriverId = DriverId
FROM Transport.Driver
WHERE Mobile = @DriverMobile;

IF @ExistingDriverId IS NULL
BEGIN
    INSERT INTO Transport.Driver
    (
        DriverName, Mobile, Address, DrivingLicenseNo,
        IsAppRegistered, ConsentGiven, IsVerified,
        IsActive, CreatedBy
    )
    VALUES
    (
        @DriverName, @DriverMobile, @DriverAddress,
        @DrivingLicenseNo, 0, 1, 0, 1, @CreatedBy
    );
    SET @ExistingDriverId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE Transport.Driver
    SET DriverName = @DriverName,
        Address = @DriverAddress,
        DrivingLicenseNo = @DrivingLicenseNo,
        ConsentGiven = 1,
        IsActive = 1
    WHERE DriverId = @ExistingDriverId;
END

SELECT @ExistingDriverId;";

                            using (SqlCommand command = new SqlCommand(driverInsert, connection, transaction))
                            {
                                AddDriverParameters(command, model);
                                AddParameter(command, "@CreatedBy", userName, SqlDbType.NVarChar, 100);
                                SqlParameter output = command.Parameters.Add("@ExistingDriverId", SqlDbType.BigInt);
                                output.Direction = ParameterDirection.InputOutput;
                                output.Value = DBNull.Value;
                                object value = await command.ExecuteScalarAsync();
                                driverId = Convert.ToInt64(value);
                            }

                            const string vehicleInsert = @"
IF EXISTS
(
    SELECT 1
    FROM Transport.Vehicle
    WHERE RegistrationNoNormalized = UPPER(REPLACE(REPLACE(@RegistrationNo, N'-', N''), N' ', N''))
)
    THROW 52002, 'A vehicle with this registration number already exists.', 1;

INSERT INTO Transport.Vehicle
(
    VehicleTypeId, RegistrationNo, DisplayName,
    Make, Model, ModelYear, Color, SeatingCapacity,
    OwnerName, OwnerMobile,
    VerificationStatus, IsActive, CreatedBy
)
VALUES
(
    @VehicleTypeId, @RegistrationNo, @DisplayName,
    @Make, @Model, @ModelYear, @Color, @SeatingCapacity,
    @OwnerName, @OwnerMobile,
    N'Verified', @IsActive, @CreatedBy
);
SELECT CONVERT(BIGINT, SCOPE_IDENTITY());";

                            using (SqlCommand command = new SqlCommand(vehicleInsert, connection, transaction))
                            {
                                AddVehicleParameters(command, model);
                                AddParameter(command, "@CreatedBy", userName, SqlDbType.NVarChar, 100);
                                vehicleId = Convert.ToInt64(await command.ExecuteScalarAsync());
                            }

                            const string assignmentInsert = @"
INSERT INTO Transport.VehicleAssignment
(
    ElectionContextId, VehicleId, DriverId,
    ProviderId, CandidateId, PartyId,
    AssignedPollingStationId, AssignmentStatus,
    MaxServiceRadiusKm, IsPrimaryAssignment,
    IsActive, Remarks, CreatedBy
)
VALUES
(
    @ElectionContextId, @VehicleId, @DriverId,
    @ProviderId, @CandidateId, @PartyId,
    @AssignedPollingStationId, @AssignmentStatus,
    @MaxServiceRadiusKm, 1,
    @IsActive, @Remarks, @CreatedBy
);
SELECT CONVERT(BIGINT, SCOPE_IDENTITY());";

                            using (SqlCommand command = new SqlCommand(assignmentInsert, connection, transaction))
                            {
                                AddAssignmentParameters(command, model);
                                AddParameter(command, "@VehicleId", vehicleId, SqlDbType.BigInt);
                                AddParameter(command, "@DriverId", driverId, SqlDbType.BigInt);
                                AddParameter(command, "@CreatedBy", userName, SqlDbType.NVarChar, 100);
                                assignmentId = Convert.ToInt64(await command.ExecuteScalarAsync());
                            }

                            const string liveStateInsert = @"
INSERT INTO Transport.VehicleLiveState
(
    VehicleAssignmentId, VehicleId,
    CurrentStatus, IsOnline,
    TodayTrips, TodayDistanceKm,
    TotalDistanceKm
)
VALUES
(
    @VehicleAssignmentId, @VehicleId,
    N'NeverActivated', 0,
    0, 0, 0
);";

                            using (SqlCommand command = new SqlCommand(liveStateInsert, connection, transaction))
                            {
                                AddParameter(command, "@VehicleAssignmentId", assignmentId, SqlDbType.BigInt);
                                AddParameter(command, "@VehicleId", vehicleId, SqlDbType.BigInt);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await WriteAuditAsync(connection, transaction, model.ElectionContextId,
                            "VehicleAssignment", assignmentId.ToString(),
                            model.VehicleAssignmentId.HasValue ? "Update" : "Create",
                            userName, "Vehicle registration/assignment saved.");

                        transaction.Commit();
                        return assignmentId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<ProviderManageViewModel> BuildProviderFormAsync(long electionContextId, long? providerId)
        {
            ProviderManageViewModel model = new ProviderManageViewModel
            {
                ElectionContextId = electionContextId,
                ProviderId = providerId,
                ProviderType = "Influencer",
                IsActive = true
            };

            using (SqlConnection connection = CreateConnection())
            {
                await connection.OpenAsync();

                if (providerId.HasValue)
                {
                    const string sql = @"
SELECT TOP (1)
    P.ProviderId,
    P.ElectionContextId,
    P.ProviderName,
    P.ProviderType,
    P.Mobile,
    P.AlternateMobile,
    P.Address,
    P.Area,
    P.CandidateId,
    P.PartyId,
    P.Remarks,
    P.IsVerified,
    P.IsActive,
    PC.PromisedQuantity,
    PC.PollingStationId,
    PC.VehicleTypeId
FROM Transport.Provider AS P
LEFT JOIN Transport.ProviderCommitment AS PC
    ON PC.ProviderId = P.ProviderId
   AND PC.CommitmentStatus <> N'Cancelled'
WHERE P.ProviderId = @ProviderId
ORDER BY PC.ProviderCommitmentId DESC;";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddParameter(command, "@ProviderId", providerId.Value, SqlDbType.BigInt);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                model.ElectionContextId = reader.GetInt64Safe("ElectionContextId");
                                model.ProviderName = reader.GetStringSafe("ProviderName");
                                model.ProviderType = reader.GetStringSafe("ProviderType");
                                model.Mobile = reader.GetStringSafe("Mobile");
                                model.AlternateMobile = reader.GetStringSafe("AlternateMobile");
                                model.Address = reader.GetStringSafe("Address");
                                model.Area = reader.GetStringSafe("Area");
                                model.CandidateId = reader.GetNullableInt64("CandidateId");
                                model.PartyId = reader.GetNullableInt64("PartyId");
                                model.Remarks = reader.GetStringSafe("Remarks");
                                model.IsVerified = reader.GetBooleanSafe("IsVerified");
                                model.IsActive = reader.GetBooleanSafe("IsActive");
                                model.PromisedQuantity = reader.GetInt32Safe("PromisedQuantity");
                                model.CommitmentPollingStationId = reader.GetNullableInt64("PollingStationId");
                                int? typeId = reader.GetNullableInt32("VehicleTypeId");
                                model.CommitmentVehicleTypeId = typeId.HasValue ? (short?)typeId.Value : null;
                            }
                        }
                    }
                }

                model.Candidates = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), CandidateId) AS Value, CandidateName AS Text
FROM Transport.Candidate
WHERE ElectionContextId = @ElectionContextId AND IsActive = 1
ORDER BY CandidateName;", model.CandidateId.HasValue ? model.CandidateId.Value.ToString() : null,
                    delegate(SqlCommand command)
                    {
                        AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                    });

                model.Parties = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), PartyId) AS Value, PartyName AS Text
FROM Transport.Party
WHERE IsActive = 1
ORDER BY PartyName;", model.PartyId.HasValue ? model.PartyId.Value.ToString() : null);

                model.PollingStations = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), PollingStationId) AS Value,
       N'Sr ' + CONVERT(NVARCHAR(20), Sr) + N' — ' + COALESCE(PollingStationName, StationName) AS Text
FROM Transport.PollingStation
WHERE ElectionContextId = @ElectionContextId AND IsOperational = 1
ORDER BY Sr;", model.CommitmentPollingStationId.HasValue ? model.CommitmentPollingStationId.Value.ToString() : null,
                    delegate(SqlCommand command)
                    {
                        AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                    });

                model.VehicleTypes = await ReadSelectListAsync(connection, @"
SELECT CONVERT(NVARCHAR(20), VehicleTypeId) AS Value, Name AS Text
FROM Transport.VehicleType
WHERE IsActive = 1
ORDER BY SortOrder, Name;", model.CommitmentVehicleTypeId.HasValue ? model.CommitmentVehicleTypeId.Value.ToString() : null);
            }

            return model;
        }

        public async Task<long> SaveProviderAsync(ProviderManageViewModel model, string userName)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            using (SqlConnection connection = CreateConnection())
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        long providerId;
                        if (model.ProviderId.HasValue)
                        {
                            const string updateSql = @"
UPDATE Transport.Provider
SET ProviderName = @ProviderName,
    ProviderType = @ProviderType,
    Mobile = @Mobile,
    AlternateMobile = @AlternateMobile,
    Address = @Address,
    Area = @Area,
    CandidateId = @CandidateId,
    PartyId = @PartyId,
    Remarks = @Remarks,
    IsVerified = @IsVerified,
    IsActive = @IsActive
WHERE ProviderId = @ProviderId;";

                            using (SqlCommand command = new SqlCommand(updateSql, connection, transaction))
                            {
                                AddProviderParameters(command, model);
                                AddParameter(command, "@ProviderId", model.ProviderId.Value, SqlDbType.BigInt);
                                await command.ExecuteNonQueryAsync();
                            }

                            providerId = model.ProviderId.Value;
                        }
                        else
                        {
                            const string insertSql = @"
INSERT INTO Transport.Provider
(
    ElectionContextId, PartyId, CandidateId,
    ProviderName, ProviderType, Mobile,
    AlternateMobile, Address, Area, Remarks,
    IsVerified, IsActive, CreatedBy
)
VALUES
(
    @ElectionContextId, @PartyId, @CandidateId,
    @ProviderName, @ProviderType, @Mobile,
    @AlternateMobile, @Address, @Area, @Remarks,
    @IsVerified, @IsActive, @CreatedBy
);
SELECT CONVERT(BIGINT, SCOPE_IDENTITY());";

                            using (SqlCommand command = new SqlCommand(insertSql, connection, transaction))
                            {
                                AddProviderParameters(command, model);
                                AddParameter(command, "@CreatedBy", userName, SqlDbType.NVarChar, 100);
                                providerId = Convert.ToInt64(await command.ExecuteScalarAsync());
                            }
                        }

                        if (model.PromisedQuantity > 0)
                        {
                            const string commitmentSql = @"
INSERT INTO Transport.ProviderCommitment
(
    ElectionContextId, ProviderId,
    PartyId, CandidateId,
    PollingStationId, VehicleTypeId,
    PromisedQuantity, CommitmentDate,
    CommitmentStatus, Remarks, CreatedBy
)
VALUES
(
    @ElectionContextId, @ProviderId,
    @PartyId, @CandidateId,
    @PollingStationId, @VehicleTypeId,
    @PromisedQuantity, CONVERT(DATE, GETDATE()),
    N'Promised', @Remarks, @CreatedBy
);";

                            using (SqlCommand command = new SqlCommand(commitmentSql, connection, transaction))
                            {
                                AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
                                AddParameter(command, "@ProviderId", providerId, SqlDbType.BigInt);
                                AddParameter(command, "@PartyId", model.PartyId, SqlDbType.BigInt);
                                AddParameter(command, "@CandidateId", model.CandidateId, SqlDbType.BigInt);
                                AddParameter(command, "@PollingStationId", model.CommitmentPollingStationId, SqlDbType.BigInt);
                                AddParameter(command, "@VehicleTypeId", model.CommitmentVehicleTypeId, SqlDbType.SmallInt);
                                AddParameter(command, "@PromisedQuantity", model.PromisedQuantity, SqlDbType.Int);
                                AddParameter(command, "@Remarks", model.Remarks, SqlDbType.NVarChar, 1000);
                                AddParameter(command, "@CreatedBy", userName, SqlDbType.NVarChar, 100);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await WriteAuditAsync(connection, transaction, model.ElectionContextId,
                            "Provider", providerId.ToString(),
                            model.ProviderId.HasValue ? "Update" : "Create",
                            userName, "Provider/influencer record saved.");

                        transaction.Commit();
                        return providerId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private async Task<IList<SelectListItem>> ReadSelectListAsync(
            SqlConnection connection,
            string sql,
            string selectedValue,
            Action<SqlCommand> configure = null)
        {
            List<SelectListItem> result = new List<SelectListItem>();
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                if (configure != null)
                {
                    configure(command);
                }

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string value = reader.GetStringSafe("Value");
                        result.Add(new SelectListItem
                        {
                            Value = value,
                            Text = reader.GetStringSafe("Text"),
                            Selected = string.Equals(value, selectedValue, StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }

            return result;
        }

        private static void AddDriverParameters(SqlCommand command, VehicleManageViewModel model)
        {
            AddParameter(command, "@DriverName", model.DriverName, SqlDbType.NVarChar, 250);
            AddParameter(command, "@DriverMobile", model.DriverMobile, SqlDbType.NVarChar, 30);
            AddParameter(command, "@DriverAddress", model.DriverAddress, SqlDbType.NVarChar, 1000);
            AddParameter(command, "@DrivingLicenseNo", model.DrivingLicenseNo, SqlDbType.NVarChar, 100);
        }

        private static void AddVehicleParameters(SqlCommand command, VehicleManageViewModel model)
        {
            AddParameter(command, "@VehicleTypeId", model.VehicleTypeId, SqlDbType.SmallInt);
            AddParameter(command, "@RegistrationNo", model.RegistrationNo, SqlDbType.NVarChar, 50);
            AddParameter(command, "@DisplayName", model.DisplayName, SqlDbType.NVarChar, 200);
            AddParameter(command, "@Make", model.Make, SqlDbType.NVarChar, 100);
            AddParameter(command, "@Model", model.Model, SqlDbType.NVarChar, 100);
            AddParameter(command, "@ModelYear", model.ModelYear, SqlDbType.SmallInt);
            AddParameter(command, "@Color", model.Color, SqlDbType.NVarChar, 100);
            AddParameter(command, "@SeatingCapacity", model.SeatingCapacity, SqlDbType.SmallInt);
            AddParameter(command, "@OwnerName", model.OwnerName, SqlDbType.NVarChar, 250);
            AddParameter(command, "@OwnerMobile", model.OwnerMobile, SqlDbType.NVarChar, 30);
            AddParameter(command, "@IsActive", model.IsActive, SqlDbType.Bit);
        }

        private static void AddAssignmentParameters(SqlCommand command, VehicleManageViewModel model)
        {
            AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
            AddParameter(command, "@ProviderId", model.ProviderId, SqlDbType.BigInt);
            AddParameter(command, "@CandidateId", model.CandidateId, SqlDbType.BigInt);
            AddParameter(command, "@PartyId", model.PartyId, SqlDbType.BigInt);
            AddParameter(command, "@AssignedPollingStationId", model.AssignedPollingStationId, SqlDbType.BigInt);
            AddParameter(command, "@AssignmentStatus", model.AssignmentStatus, SqlDbType.NVarChar, 30);
            AddParameter(command, "@MaxServiceRadiusKm", model.MaxServiceRadiusKm, SqlDbType.Decimal);
            command.Parameters["@MaxServiceRadiusKm"].Precision = 8;
            command.Parameters["@MaxServiceRadiusKm"].Scale = 2;
            AddParameter(command, "@Remarks", model.Remarks, SqlDbType.NVarChar, 1000);
            AddParameter(command, "@IsActive", model.IsActive, SqlDbType.Bit);
        }

        private static void AddProviderParameters(SqlCommand command, ProviderManageViewModel model)
        {
            AddParameter(command, "@ElectionContextId", model.ElectionContextId, SqlDbType.BigInt);
            AddParameter(command, "@ProviderName", model.ProviderName, SqlDbType.NVarChar, 250);
            AddParameter(command, "@ProviderType", model.ProviderType, SqlDbType.NVarChar, 50);
            AddParameter(command, "@Mobile", model.Mobile, SqlDbType.NVarChar, 30);
            AddParameter(command, "@AlternateMobile", model.AlternateMobile, SqlDbType.NVarChar, 30);
            AddParameter(command, "@Address", model.Address, SqlDbType.NVarChar, 1000);
            AddParameter(command, "@Area", model.Area, SqlDbType.NVarChar, 300);
            AddParameter(command, "@CandidateId", model.CandidateId, SqlDbType.BigInt);
            AddParameter(command, "@PartyId", model.PartyId, SqlDbType.BigInt);
            AddParameter(command, "@Remarks", model.Remarks, SqlDbType.NVarChar, 1000);
            AddParameter(command, "@IsVerified", model.IsVerified, SqlDbType.Bit);
            AddParameter(command, "@IsActive", model.IsActive, SqlDbType.Bit);
        }

        private static async Task WriteAuditAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            long electionContextId,
            string entityName,
            string entityId,
            string actionName,
            string userName,
            string details)
        {
            const string sql = @"
INSERT INTO Transport.AuditLog
(
    ElectionContextId, EntityName, EntityId,
    ActionName, UserName, DetailsJson
)
VALUES
(
    @ElectionContextId, @EntityName, @EntityId,
    @ActionName, @UserName, @DetailsJson
);";

            using (SqlCommand command = new SqlCommand(sql, connection, transaction))
            {
                AddParameter(command, "@ElectionContextId", electionContextId, SqlDbType.BigInt);
                AddParameter(command, "@EntityName", entityName, SqlDbType.NVarChar, 100);
                AddParameter(command, "@EntityId", entityId, SqlDbType.NVarChar, 100);
                AddParameter(command, "@ActionName", actionName, SqlDbType.NVarChar, 100);
                AddParameter(command, "@UserName", userName, SqlDbType.NVarChar, 200);
                AddParameter(command, "@DetailsJson", details, SqlDbType.NVarChar, -1);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
