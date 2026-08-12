var map;
function provinceMap() {
    var map = L.map('mapid').setView([30.5, 66], 5);

    var mapboxAccessToken = 'REPLACE_WITH_YOUR_MAPBOX_ACCESS_TOKEN';

    L.tileLayer('https://api.mapbox.com/styles/v1/{id}/tiles/{z}/{x}/{y}?access_token=' + mapboxAccessToken, {
        maxZoom: 18,
        attribution: 'Map data &copy; <a href="http://www.pbs.gov.pk/">Pakistan Bureau of Statistics</a>',
        id: 'mapbox/light-v9',
        tileSize: 512,
        zoomOffset: -1,
        zoomControl:false,
    }).addTo(map);

    map.zoomControl.remove();
        L.control.zoom({
            position: 'topright'
        }).addTo(map);
    // control that shows state info on hover
    var info = L.control();

    info.onAdd = function (mymap) {
        this._div = L.DomUtil.create('div', 'info');
        this.update();
        return this._div;
    };


    info.update = function (props) {
        this._div.innerHTML = '' + (props ?
        '<b>' + props.NAME_1 + '</b><br />' + props.value + '  millions'
            : 'Population');
    };

    info.addTo(map);


    // get color depending on population value value
    function getColor(d) {

        return d > 100 ? '#800026' :
            d > 40 ? '#E31A1C' :
                d > 20 ? '#FC4E2A' :
                    d > 10 ? '#FD8D3C' :
                        d > 2 ? '#FEB24C' :
                            '#FFEDA0';
    }

    function style(feature) {
        return {
            weight: 2,
            opacity: 1,
            color: 'white',
            dashArray: '3',
            fillOpacity: 0.7,
            fillColor: getColor(feature.properties.value)
        };
    }

    function highlightFeature(e) {
        var layer = e.target;

        layer.setStyle({
            weight: 5,
            color: '#666',
            dashArray: '',
            fillOpacity: 0.7
        });

        if (!L.Browser.ie && !L.Browser.opera && !L.Browser.edge) {
            layer.bringToFront();
        }

        info.update(layer.feature.properties);
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
            click: provinceMap
        });
    }


    geojson = L.geoJson(provincedata, {
        style: style,
        onEachFeature: onEachFeature
    }).addTo(map);

    map.attributionControl.addAttribution('Credit data &copy; <a href="http://pbs.gov.pk/">PBS</a>');


    var legend = L.control({position: 'bottomright' });

    legend.onAdd = function (map) {
        var div = L.DomUtil.create('div', 'info legend'),
            grades = [2, 10, 20, 40, 100],
            labels = [],
            from, to;

        for (var i = 0; i < grades.length; i++) {
            from = grades[i];
            to = grades[i + 1];

            labels.push(
                '<i style="background-color:' + getColor(from==0? from: from+1) + '"></i> ' +
                (from==0? '0': from + (to ? '&ndash;' + to : '+')));
        }

        div.innerHTML = labels.join('<br>');
        return div;
    };

    legend.addTo(map);
}





function LoadProvinceData() {
    provincedata[0].features[0].properties.value = 109;
    provincedata[0].features[1].properties.value = 48;
	  provincedata[0].features[2].properties.value = 36;
    provincedata[0].features[3].properties.value = 13;
    provincedata[0].features[4].properties.value = 4;
    provincedata[0].features[5].properties.value = 2;
    provincedata[0].features[6].properties.value = 1.2;
    map = null;
    provinceMap();
    //displayProMap(par);
}
