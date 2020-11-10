using MapControl;
using MapControl.Caching;
using Microsoft.Maps.MapControl.WPF;
using MMACRulesMining.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.UI.Xaml.Controls.Maps;

namespace MMACRulesMining.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow()
        {
            try
            {
                ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", "XAML Map Control Test Application");

                TileImageLoader.Cache = new ImageFileCache(TileImageLoader.DefaultCacheFolder);

                BingMapsTileLayer.ApiKey = File.ReadAllText(@"..\..\..\BingMapsApiKey.txt")?.Trim();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public MainWindow()
        {
            this.DataContext = new MainViewModel();
            InitializeComponent();
            
            if (TileImageLoader.Cache is ImageFileCache cache)
            {
                Loaded += async (s, e) =>
                {
                    await Task.Delay(2000);
                    await cache.Clean();
                };
            }

            //Binding binding = new Binding();
            //binding.Source = DataContext;
            //binding.Path = new PropertyPath(nameof(MainViewModel.Tiles));
            //binding.Mode = BindingMode.OneWay;
            //binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            //if (((MainViewModel)DataContext).Tiles != null)
            //{
            //    foreach (var tile in ((MainViewModel)DataContext).Tiles)
            //    {
            //        AddNewPolygon(tile);
            //    }
            //}
        }

        //void AddNewPolygon(Tile tile)
        //{
        //    Microsoft.Maps.MapControl.WPF.MapPolygon polygon = new Microsoft.Maps.MapControl.WPF.MapPolygon();
        //    polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
        //    polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
        //    polygon.StrokeThickness = 2;
        //    polygon.Opacity = 0.7;
        //    polygon.Locations = new LocationCollection() {
        //        new Location(tile.NE.lat, tile.NE.lon),
        //        new Location(tile.SE.lat, tile.SE.lon),
        //        new Location(tile.SW.lat, tile.SW.lon),
        //        new Location(tile.NW.lat, tile.NW.lon)
        //    };

        //    //NewPolygonLayer.Children.Add(polygon);

        //}
    }
}
