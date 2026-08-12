var map;

function decideLegend(data, numBins = 6) {
    if (!data || data.length === 0) return { gap: [], unit: '', series: '', quantity: '' };

    const values = data
        .map(item => item.value)
        .filter(v => typeof v === "number" && !isNaN(v))
        .sort((a, b) => a - b);

    if (values.length === 0) return { gap: [], unit: '', series: '', quantity: '' };

    const min = values[0];
    const max = values[values.length - 1];

    if (min === max) {
        return {
            gap: [0,min,max],
            unit: '',
            series: '',
            quantity: ''
        };
    }

    const total = values.length;
    let rawBreaks = [min];

    for (let i = 1; i <= numBins; i++) {
        const idx = Math.floor(i * total / numBins);
        rawBreaks.push(values[Math.min(idx, total - 1)]);
    }

    //alert(JSON.stringify(rawBreaks));

    // Round with better control to prevent flattening
    const roundedBreaks = rawBreaks.map(val => {
        if (val === 0) return 0;
        const magnitude = Math.pow(10, Math.floor(Math.log10(Math.abs(val))));
        let rounded = +(Math.round(val / magnitude * 2) / 2 * magnitude).toFixed(3);
        // Keep small values from collapsing
        if (Math.abs(rounded - val) / val > 0.5) {
            rounded = val;  // fall back to unrounded
        }
        return rounded;
    });

    // Ensure uniqueness and fill gaps if needed
    let uniqueBreaks = [...new Set(roundedBreaks)].sort((a, b) => a - b);

    // Fallback: if not enough bins, use raw breaks
    if (uniqueBreaks.length <= 2) {
        uniqueBreaks = [...new Set(rawBreaks)];
    }


    // Build bins
    let bins = [];
    for (let i = 0; i < uniqueBreaks.length - 1; i++) {
        bins.push(uniqueBreaks[i]);
    }
    if (bins.length < 4) {
        //bins = [0,min, Math.round(max/2), max, max*2, max*3];
        bins = [0,1, 2,3,4,5];
    }
    return {
        gap: bins,
        unit: '',
        series: '',
        quantity: ''
    };
}


function provinceMap(mapdata, leg, level, dated, desc, center, displayTable=1) {
    if (map != null) {
        map.remove();
    }
    var center = center;
    var zoomLevel = 5;
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
    map = L.map("c_map", { fullscreenControl: true, center: center, zoomControl: false, zoom: zoomLevel, layers: [googleStreets] });
    var layerControl = L.control.layers(baseMaps, overlayMaps).addTo(map);
    layerControl.addTo(map);
    map.invalidateSize();

    L.control.zoom({
        position: 'bottomleft'
    }).addTo(map);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributers | Source: Pakistan Bureau of Statistics'
    }).addTo(map);

    // control that shows state info on hover
    var info = L.control();

    info.onAdd = function (mymap) {
        this._div = L.DomUtil.create('div', 'info hover left');
        this.position = 'topleft';
        this.update();
        return this._div;
    };


    info.update = function (props) {
        this._div.innerHTML = '' + (props ?
            '<b>' + props.name.replace("DISTRICT", "") + '</b><br />' + props.value + '  '
            : ' ');
    };

    info.addTo(map);

    gap = leg.gap;

    // get color depending on population value value
    function getColor(d) {
        return d > gap[4] ? '#800026' :
            d > gap[3] ? '#BD0026' :
                d > gap[2] ? '#FC4E2A' :
                    d > gap[1] ? '#FD8D3C' :
                        d > gap[0] ? 'green' :
                            'grey';
    }

    function getColor(d, unit) {

        return d > gap[4] ? '#800026' :
            d > gap[3] ? '#BD0026' :
                d > gap[2] ? '#FC4E2A' :
                    d > gap[1] ? '#FD8D3C' :
                        d > gap[0] ? 'green' :
                            'grey';
    }
    map.on('enterFullscreen', function () {
        document.querySelector('.legend').style.fontSize = '20px';
    });

    map.on('exitFullscreen', function () {
        document.querySelector('.legend').style.fontSize = '12px';  // default size
    });

    function style(feature) {
        if (feature.properties.data != null) {
            return {
                weight: 1,
                opacity: 1,
                color: 'grey',
                dashArray: '3',
                fillOpacity: 0.7,
                fillColor: getColor(feature.properties.data.value, leg.unit)
            };
        }
        else {
            return {
                weight: 1,
                opacity: 1,
                color: 'grey',
                dashArray: '3',
                fillOpacity: 0.7,
                fillColor: 'grey'
            };
        }

    }

    function performMapClick(e) {
        const layer = e.target; 
        const feature = layer.feature;
        if (displayTable == 1) {
            let $tbody = $('#marker-details-table tbody');
            $tbody.empty();

            for (const key in feature.properties.data) {
                if (feature.properties.data.hasOwnProperty(key) && !key.toLowerCase().endsWith("id")) {
                    let value = feature.properties.data[key] == null ? '' : feature.properties.data[key];

                    if (typeof value === 'string' && (value.toUpperCase().includes('DISTRICT') || value.toUpperCase().includes('DIVISION'))) {
                        value = value.replace(/DISTRICT|DIVISION/gi, '').trim();
                    }

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
        
    }

    function highlightFeature(e) {
        var layer = e.target;

        layer.setStyle({
            weight: 5,
            color: 'black',
            dashArray: '',
            fillOpacity: 0.7
        });

        if (!L.Browser.ie && !L.Browser.opera && !L.Browser.edge) {
            layer.bringToFront();
        }

        info.update(layer.feature.properties.data);
    }

    var geojson;

    function resetHighlight(e) {
        geojson.resetStyle(e.target);
        info.update();
    }

    function zoomToFeature(e) {
        map.fitBounds(e.target.getBounds());
    }

    function onEachFeature(feature, layer) {
        layer.on({
            mouseover: highlightFeature,
            mouseout: resetHighlight,
            click: performMapClick
        });
    }

    console.log(mapdata);
    geojson = L.geoJson(mapdata, {
        style: style,
        onEachFeature: onEachFeature
    }).addTo(map);

    //map.attributionControl.addAttribution('Credit data &copy; <a href="http://pbs.gov.pk/">PBS</a>');


    var legend = L.control({ position: 'bottomright' });

    legend.onAdd = function (map) {
        var div = L.DomUtil.create('div', 'info legend'),
            labels = [
                '<i style="background-color:grey"></i>0'],
            from, to;

        for (var i = 1; i < gap.length; i++) {
            from = gap[i];
            to = gap[i + 1];

            labels.push(
                '<i style="background-color:' + getColor(from>0? from : from + 1) + '"></i> ' +
                (from + (to ? '' + leg.unit + ' &ndash;' + to : leg.unit + ' +')));
        }

        div.innerHTML = labels.join('<br>');
        return div;
    };

    legend.addTo(map);
}

const formatBoundariesAsGeoJSON = (bouns, progressData) => {
    const features = bouns.map(boundary => {
        const progress = progressData.find(progress => parseInt(progress.id) === parseInt(boundary.code));
        return {
            type: "Feature",
            geometry: boundary.boundary,
            properties: {
                level: boundary.next_level,
                name: boundary.name,
                code: boundary.code,
                data: progress ? progress : null
            }
        };
    });

    // Create GeoJSON FeatureCollection
    const geoJSON = {
        type: "FeatureCollection",
        features: features
    };
    return geoJSON;
};


