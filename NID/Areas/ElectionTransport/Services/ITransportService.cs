using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using NID.Areas.ElectionTransport.Models;

namespace NID.Areas.ElectionTransport.Services
{
    public interface ITransportService
    {
        Task<IList<ElectionContextDto>> GetContextsAsync();
        Task<ElectionContextDto> GetContextAsync(long electionContextId);
        Task<TransportDashboardDto> GetDashboardAsync(long electionContextId);
        Task<IList<LiveVehicleDto>> GetLiveVehiclesAsync(VehicleMapFilterModel filter);
        Task<IList<PollingStationOperationsDto>> GetPollingStationsAsync(long electionContextId);
        Task<IList<ProviderPerformanceDto>> GetProvidersAsync(long electionContextId);
        Task<IList<TransportRequestQueueDto>> GetRequestsAsync(long electionContextId, string status);
        Task<IList<TripDto>> GetTripsAsync(long electionContextId, int take);
        Task<IList<TransportExceptionDto>> GetExceptionsAsync(long electionContextId);
        Task<VehicleDetailsDto> GetVehicleDetailsAsync(long vehicleAssignmentId);
        Task<IList<VehicleTrailPointDto>> GetVehicleTrailAsync(long vehicleAssignmentId, int minutes);
        Task<PollingStationDetailsDto> GetPollingStationDetailsAsync(long pollingStationId);
        Task<ProviderDetailsDto> GetProviderDetailsAsync(long providerId);
        Task<TransportRequestDetailsDto> GetRequestDetailsAsync(long transportRequestId);
        Task<PublicRequestStatusDto> GetPublicRequestStatusAsync(string requestNo, string mobileLast4);
        Task<VehicleManageViewModel> BuildVehicleFormAsync(long electionContextId, long? vehicleAssignmentId);
        Task<long> SaveVehicleAsync(VehicleManageViewModel model, string userName);
        Task<ProviderManageViewModel> BuildProviderFormAsync(long electionContextId, long? providerId);
        Task<long> SaveProviderAsync(ProviderManageViewModel model, string userName);
        Task<PublicTransportRequestViewModel> BuildPublicRequestFormAsync(long electionContextId, long? pollingStationId, long? partyId, long? candidateId);
        Task<PublicRequestConfirmationViewModel> CreatePublicRequestAsync(PublicTransportRequestViewModel model, string createdBy);
        Task<IList<DispatchOfferDto>> RouteRequestAsync(long transportRequestId, int offerCount, string changedBy);
        Task AcceptDispatchAsync(long requestDispatchId, string changedBy);
        Task UpdateRequestStatusAsync(long transportRequestId, string newStatus, string remarks, string changedBy);
        Task<LocationPushResultDto> RecordLocationByVehicleCodeAsync(LocationPushInputModel input);
        Task SeedDemoDataAsync(long electionContextId, int vehicleCount, int requestCount);
        Task RefreshOfflineStatesAsync(long? electionContextId);
    }

    public interface IDummyTransportSimulator
    {
        Task<int> EnsureRoutesAsync(long electionContextId);
        Task<SimulationTickResultDto> TickAsync(long electionContextId);
    }
}
