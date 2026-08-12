var map;
 function mapData(data, map) {
                data.forEach(p => {
                    let icon;
                    if (p.category === "Edu") icon = redIcon;
                    else if (p.category === "Hospital") icon = blueIcon;
                    else if (p.category === "Scheme") icon = greenIcon;

                    const marker = L.marker([p.lat, p.lon], { icon: icon }).addTo(map);

                    marker.bindPopup(`<strong>${p.name}</strong><br>Type: ${p.type}`);

                    marker.on('click', function () {
                        showMarkerDetails(p);
                    })
                });
            }

            function showMarkerDetails(markerData) {
                let $tbody = $('#marker-details-table tbody');
                $tbody.empty();

                for (const key in markerData) {
                    if (
                        markerData.hasOwnProperty(key) &&
                        !key.toLowerCase().endsWith("id") // Skip keys ending in "id"
                    ) {
                        const value = markerData[key] == null ? '' : markerData[key]; // Replace null with empty string
                        $tbody.append(
                            `<tr>
                    <th>${key}</th>
                    <td>${value}</td>
                </tr>`
                        );
                    }
                }

                $('#info-table').show();
            }

            function displayMap(data) {
                if (map) {
                    map.remove(); // Clean up previous map
                    $('#map').html(''); // Clear the container
                }
                var center = [30.642814, 70.030772];
                googleMap = googleMap = L.tileLayer('http://{s}.google.com/vt/lyrs=s&x={x}&y={y}&z={z}', { maxZoom: 20, subdomains: ['mt0', 'mt1', 'mt2', 'mt3'] });
    var osm = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '© OpenStreetMap' });
    var osmHOT = L.tileLayer('https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png', { maxZoom: 19, attribution: '© OpenStreetMap contributors, Tiles style by Humanitarian OpenStreetMap Team hosted by OpenStreetMap France' });
    var googleStreets = L.tileLayer('http://{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}', { maxZoom: 20, subdomains: ['mt0', 'mt1', 'mt2', 'mt3'] });
    var esriSatellite = L.tileLayer('http://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', { maxZoom: 18, });
    var baseMaps = {
        "OpenStreetMap": osm,
        "OpenStreetMap.HOT": osmHOT,
        "Google Satellite": googleMap,
        "Esri": esriSatellite,
    };
    var overlayMaps = {
    };
    map = L.map("map", { fullscreenControl: true, center: center, zoomControl: false, zoom: 5, layers: [googleStreets] });
    var layerControl = L.control.layers(baseMaps, overlayMaps).addTo(map);
    layerControl.addTo(map);
    map.invalidateSize();

    L.control.zoom({
        position: 'bottomleft'
    }).addTo(map);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributers | Source: Pakistan Bureau of Statistics'
    }).addTo(map);


                map.invalidateSize();

                mapData(data, map);

            }