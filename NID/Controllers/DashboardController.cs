using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NID.Models;

namespace NID.Controllers
{
    public class DashboardController : Controller
    {
        public ActionResult Index()
        {
            DashboardViewModel model = new DashboardViewModel();

            // Dashboard Summary
            model.TotalVehicles = 20;
            model.ActiveVehicles = 17;
            model.TotalPollingStations = 15;
            model.TotalIncidents = 5;
            model.CompletedTrips = 96;
            model.TotalPickedVoters = 1245;

            // Vehicles
            model.Vehicles = GetVehicles();

            // Drivers
            model.Drivers = GetDrivers();

            // Routes
            model.Routes = GetRoutes();

            // Polling Stations
            model.PollingStations = GetPollingStations();

            // Incidents
            model.Incidents = GetIncidents();

            return View(model);
        }

        private List<VehicleModel> GetVehicles()
        {
            return new List<VehicleModel>
            {
                new VehicleModel
                {
                    VehicleId=1,
                    VehicleNo="ICT-101",
                    DriverName="Ahmed Khan",
                    RouteName="Route A",
                    PollingStation="PS-101",
                    Capacity=20,
                    Passengers=18,
                    CompletedTrips=5,
                    Status="Active",
                    Latitude="33.6844",
                    Longitude="73.0479"
                },

                new VehicleModel
                {
                    VehicleId=2,
                    VehicleNo="ICT-102",
                    DriverName="Ali Raza",
                    RouteName="Route B",
                    PollingStation="PS-102",
                    Capacity=18,
                    Passengers=16,
                    CompletedTrips=4,
                    Status="Returning",
                    Latitude="33.7000",
                    Longitude="73.0500"
                },

                new VehicleModel
                {
                    VehicleId=3,
                    VehicleNo="ICT-103",
                    DriverName="Usman",
                    RouteName="Route C",
                    PollingStation="PS-103",
                    Capacity=22,
                    Passengers=20,
                    CompletedTrips=6,
                    Status="Active",
                    Latitude="33.6900",
                    Longitude="73.0600"
                }
            };
        }

        private List<DriverModel> GetDrivers()
        {
            return new List<DriverModel>
            {
                new DriverModel
                {
                    DriverId=1,
                    DriverName="Ahmed Khan",
                    Mobile="03001234567",
                    VehicleNo="ICT-101",
                    Status="Active",
                    CompletedTrips=5
                },

                new DriverModel
                {
                    DriverId=2,
                    DriverName="Ali Raza",
                    Mobile="03007654321",
                    VehicleNo="ICT-102",
                    Status="Returning",
                    CompletedTrips=4
                }
            };
        }

        private List<RouteModel> GetRoutes()
        {
            return new List<RouteModel>
            {
                new RouteModel
                {
                    RouteId=1,
                    RouteName="Route A",
                    VehicleNo="ICT-101",
                    DriverName="Ahmed Khan",
                    StartLocation="Transport Camp",
                    EndLocation="PS-101",
                    TotalTrips=5,
                    Status="Running"
                },

                new RouteModel
                {
                    RouteId=2,
                    RouteName="Route B",
                    VehicleNo="ICT-102",
                    DriverName="Ali Raza",
                    StartLocation="Transport Camp",
                    EndLocation="PS-102",
                    TotalTrips=4,
                    Status="Running"
                }
            };
        }

        private List<PollingStationModel> GetPollingStations()
        {
            return new List<PollingStationModel>
            {
                new PollingStationModel
                {
                    PollingStationId=1,
                    StationCode="PS-101",
                    StationName="Government High School",
                    Constituency="NA-53",
                    ExpectedVoters=250,
                    PickedVoters=210,
                    WaitingVoters=40,
                    Status="Open"
                },

                new PollingStationModel
                {
                    PollingStationId=2,
                    StationCode="PS-102",
                    StationName="Girls College",
                    Constituency="NA-53",
                    ExpectedVoters=300,
                    PickedVoters=250,
                    WaitingVoters=50,
                    Status="Open"
                }
            };
        }

        private List<IncidentModel> GetIncidents()
        {
            return new List<IncidentModel>
            {
                new IncidentModel
                {
                    IncidentId=1,
                    PollingStation="PS-101",
                    IncidentType="Vehicle Breakdown",
                    Severity="Medium",
                    Description="Tyre puncture on Route A",
                    ReportedBy="Control Room",
                    ReportedTime=DateTime.Now.AddMinutes(-20),
                    Status="Resolved"
                },

                new IncidentModel
                {
                    IncidentId=2,
                    PollingStation="PS-102",
                    IncidentType="Transport Delay",
                    Severity="High",
                    Description="Traffic congestion",
                    ReportedBy="Sector Incharge",
                    ReportedTime=DateTime.Now.AddMinutes(-10),
                    Status="In Progress"
                }
            };
        }
    }
}
