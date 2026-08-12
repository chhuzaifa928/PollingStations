function callMap(){
  (async () => {

      const topology = await fetch(
          'https://code.highcharts.com/mapdata/custom/world.topo.json'
      ).then(response => response.json());

      Highcharts.getJSON('https://cdn.jsdelivr.net/gh/highcharts/highcharts@v7.0.0/samples/data/world-population-density.json', function (data) {

          // Prevent logarithmic errors in color calulcation
          data.forEach(function (p) {
              p.value = (p.value < 1 ? 1 : p.value);
          });

          // Initialize the chart
          Highcharts.mapChart('worldmap', {

              chart: {
                  map: topology
              },

              title: {
                  text: 'World Map of Diaspora 2020'
              },
              credits:{
                text:'Data Source: Bureau of Emigration Pakistan'
              },
              legend: {
                title: {
                    text: null,
                    style: {
                        color: ( // theme
                            Highcharts.defaultOptions &&
                            Highcharts.defaultOptions.legend &&
                            Highcharts.defaultOptions.legend.title &&
                            Highcharts.defaultOptions.legend.title.style &&
                            Highcharts.defaultOptions.legend.title.style.color
                        ) || 'black'
                    }
                },
                align: 'left',
                verticalAlign: 'bottom',
                floating: true,
                layout: 'vertical',
                valueDecimals: 0,
                backgroundColor: ( // theme
                    Highcharts.defaultOptions &&
                    Highcharts.defaultOptions.legend &&
                    Highcharts.defaultOptions.legend.backgroundColor
                ) || 'rgba(255, 255, 255, 0.85)',
                symbolRadius: 0,
                symbolHeight: 14
            },

              mapNavigation: {
                  enabled: true,
                  buttonOptions: {
                      verticalAlign: 'bottom'
                  }
              },

              tooltip: {
                  backgroundColor: 'none',
                  borderWidth: 0,
                  shadow: false,
                  useHTML: true,
                  padding: 0,
                  pointFormat: '<span class="f32"><span class="flag {point.properties.hc-key}">' +
                      '</span></span> {point.name}<br>' +
                      '<span style="font-size:20px">{point.value} m</span>',
                  positioner: function () {
                      return { x: 0, y: 150 };
                  }
              },

              colorAxis: {
                  dataClasses: [{
                      to: 3
                  }, {
                      from: 3,
                      to: 10
                  }, {
                      from: 10,
                      to: 30
                  }, {
                      from: 30,
                      to: 100
                  }, {
                      from: 100,
                      to: 300
                  }, {
                      from: 300,
                      to: 1000
                  }, {
                      from: 1000
                  }]
              },

              series: [{
                  data: data,
                  joinBy: ['iso-a3', 'code3'],
                  name: 'Foreign Workers',
                  states: {
                      hover: {
                          color: '#a4edba'
                      }
                  }
              }]
          });
      });

  })();
}
