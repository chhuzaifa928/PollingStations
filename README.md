<div align="center">

# 🇵🇰 Election Polling Station Management System

### *EMIS — Election Management Information System for Pakistan*

A full-featured **ASP.NET MVC 5** web platform for managing **election-day operations** — polling stations, results, voter transport, and command-centre analytics — with live interactive maps.

<br>

![C#](https://img.shields.io/badge/C%23-%20.NET%20Framework%204.7.2-blueviolet)
![ASP.NET MVC](https://img.shields.io/badge/ASP.NET%20MVC-5.2.7-blue)
![EF6](https://img.shields.io/badge/Entity%20Framework-6.2.0-2ea44f)
![Dapper](https://img.shields.io/badge/Dapper-2.1.66-4e9a06)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-red)
![License](https://img.shields.io/badge/license-MIT-yellowgreen)

</div>

---

## ✨ Highlights

| Area | What it does |
| --- | --- |
| 🗳️ **Results & Polling** | Constituency results, polling-station schemes, candidate vote tallies, voter lookups |
| 🚌 **Election Transport** | Election-day voter-transport command centre — vehicles, drivers, providers, trips, requests |
| 🗺️ **Live Maps** | Real-time vehicle tracking with Leaflet + district/province GeoJSON maps & Highcharts dashboards |
| 🏗️ **Metadata-Driven CRUD** | Dynamic forms & grids generated at runtime from a `field_metadata` table |
| 🛠️ **Super Admin** | Create, alter, rename & drop tables straight from the UI — with automatic metadata sync |
| 📱 **Public Portal** | Citizens request a ride to their polling station and track it by request number |

---

## 🚀 Features

**Election Data & Results**
- Constituency & assembly-level results with candidate and party breakdowns
- Polling station and polling scheme lookup with voter statistics
- Candidate vote tallies and regional maps (district / division / province)

**Election Transport Command Centre**
- Live vehicle dashboard with status, speed, heading and distance-to-station
- Provider accountability — promises vs. fulfilled commitments, performance scores
- Request routing & dispatch offers, trip verification, and operational exceptions
- Analytics across stations, vehicles and providers
- Public ride-request form with confirmation & tracking
- Integration REST API for vehicle GPS/location pushes (API-key protected)
- Demo simulator for testing the live map end-to-end

**Platform**
- Generic metadata-driven CRUD engine for any registered table
- Runtime table builder with column-level validation rules
- Leaflet maps, Highcharts/Highmaps, DataTables, Bootstrap UI

---

## 🧰 Tech Stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET MVC 5, C#, .NET Framework 4.7.2 |
| Data | Entity Framework 6 (Database-First / EDMX), Dapper, SQL Server |
| Frontend | Razor, Bootstrap 3, jQuery, DataTables, Leaflet, Highcharts/Highmaps |
| Tooling | Visual Studio 2019+, NuGet (packages.config) |

---

## 🚀 Getting Started

### Prerequisites

- Visual Studio 2019+ (or VS 2022 with .NET Framework 4.7.2 targeting pack)
- SQL Server (LocalDB or Express recommended)
- Mapbox access token *(optional — powers the province map on the home pages)*

### Setup

```bash
# 1. Clone the repository
git clone https://github.com/chhuzaifa928/PollingStations.git
cd PollingStations

# 2. Open the solution in Visual Studio
PollingStations\NID.sln        # <- open this

# 3. Restore NuGet packages (Build -> Restore NuGet Packages)
```

1. Create the `EMIS` database on your SQL Server instance.
2. Update the connection string in `NID/Web.config`:
   ```xml
   <add name="ElectionEntities" connectionString="...data source=YOUR_SERVER;initial catalog=EMIS;integrated security=True..." providerName="System.Data.EntityClient" />
   ```
3. *(Optional)* Set your real Mapbox token in `NID/script/site/pkmap.js`:
   ```js
   var mapboxAccessToken = 'YOUR_MAPBOX_ACCESS_TOKEN';
   ```
4. Press **F5** — the app runs under IIS Express.

> The Election Transport module expects an active `Transport.ElectionContext` (see `Transport.usp_SyncPollingStations`) and can be seeded with demo data from the command-centre UI.

---

## 🗂️ Project Structure

```
PollingStations/
├── NID.sln                          # Visual Studio solution
└── NID/                             # Main MVC web application
    ├── App_Start/                   # Routes, filters, bundles
    ├── Areas/
    │   └── ElectionTransport/       # Election-day transport module
    │       ├── Controllers/         # Command centre, public portal, APIs
    │       ├── Services/            # ITransportService + SQL implementations
    │       ├── Infrastructure/      # Connection factory, security, config
    │       ├── Models/              # DTOs & form view-models
    │       └── Views/               # Razor views + JS/CSS
    ├── Controllers/                 # Home, CRUD, Generic, Super, Metadata...
    ├── Models/                      # EF (EDMX) entities + view models
    ├── Views/                       # Razor views
    ├── Content/                     # CSS, images, flags, fonts
    ├── Scripts/ & script/           # JS libraries + map data (GeoJSON)
    ├── App_Data/
    └── Web.config                   # Connection strings & module settings
```

---

## ⚙️ Configuration

Settings for the transport module live in `NID/Web.config`:

| Key | Purpose | Default |
| --- | --- | --- |
| `ElectionTransport.ConnectionStringName` | DB connection to use | `ElectionEntities` |
| `ElectionTransport.IntegrationApiKey` | API key for the location-push API | `CHANGE-THIS...` |
| `ElectionTransport.DashboardRefreshSeconds` | Live dashboard refresh rate | `60` |
| `ElectionTransport.TrailMinutes` | Vehicle trail window | `15` |
| `ElectionTransport.AllowDemoAdministration` | Enable demo seeding/simulation | `true` |

> **Security note:** before production, change the integration API key, disable demo administration, and use a real database credential (not integrated-security localhost).

---

## 🗺️ Demo

The transport module ships with:
- `SeedDemo` — populates vehicles, drivers, providers & requests
- `SimulationTick` — moves vehicles along prepared routes so the live map updates in real time

Enable both via `ElectionTransport.AllowDemoAdministration=true`, then trigger them from the command centre.

---

## 📜 License

Distributed under the MIT License. See `LICENSE` for more information.

---

<div align="center">
  Made with ❤️ for transparent, well-run elections.
</div>
