using MapControl;
using MMACRulesMining.Data;
using MMACRulesMining.Desktop.Helpers;
using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace MMACRulesMining.Desktop.ViewModels
{
	class MainViewModel : BaseViewModel
	{
		GlonassContext _context;

		string rulesPath = @"E:\Data\results\polygons\rules";

		(double lat, double lon) se = (55.6363888888889, 49.3325);
		(double lat, double lon) sw = (55.6363888888889, 48.8361111111111);
		(double lat, double lon) ne = (55.9327777777778, 49.3325);
		(double lat, double lon) nw = (55.9327777777778, 48.8361111111111);

		(double lat, double lon) cc = (0, 0);

		private FSHelper _fsHelper;

		public List<Tile> Tiles { get; set; }
		public DataTable Dataset;

		public ObservableCollection<PolygonViewModel> Polygons { get; set; }

		private PolygonViewModel _selectedPolygon;
		public PolygonViewModel SelectedPolygon
		{
			get => _selectedPolygon;
			set
			{
				_selectedPolygon = value;
				OnPropertyChanged(nameof(SelectedPolygon));
			}
		}

		public ActionCommand MineCommand { get; private set; }

		public MapTileLayer MapLayer
		{
			get => new MapTileLayer
			{
				TileSource = new TileSource { UriFormat = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" },
				SourceName = "OpenStreetMap",
				Description = "© [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)"
			};
		}

		public MainViewModel()
		{
			_fsHelper = new FSHelper();
			_context = new GlonassContext();

			MineCommand = new ActionCommand(
				c => MineRulesForPolygon(SelectedPolygon, "Dataset_Kazan_filtered"),
				c => SelectedPolygon != null && !SelectedPolygon.IsLoading 
					&& SelectedPolygon.Records != null
			);

			PolygonViewModel.SetContext(_context);

			Polygons = new ObservableCollection<PolygonViewModel>();
			
			foreach(PolygonViewModel polygon in PolygonsImporter.ImportDirectory(@"E:\Data\polygons"))
			{
				polygon.Clicked += OnPolygonSelected;
				Polygons.Add(polygon);
			}
			OnPropertyChanged(nameof(Polygons));

			cc = ((ne.lat + sw.lat) / 2, (ne.lon + sw.lon) / 2);
			CenterPoint.Latitude = (ne.lat + sw.lat) / 2;
			CenterPoint.Longitude = (ne.lon + sw.lon) / 2;
			OnPropertyChanged(nameof(CenterPoint));
			OnPropertyChanged(nameof(Tiles));

			//Tiles = LoadTiles(rulesPath);

			//ActionSelectionViewModel.ShowActionSelection(_context);

			//Dataset = _fsHelper.GetDataSet(@"E:\Data\cards_features.csv", ";");
			//Tiles = PrepareTiles(ne, sw, 10);
			//foreach (var tile in Tiles)
			//{
			//	MineRulesForTile(tile, Dataset);
			//}
			//foreach (var tile in Tiles)
			//{
			//	Polygons.Add(GetPolygonForTile(tile));
			//}
		}

		public Location CenterPoint { get; set; } = new Location(0, 0);

		public List<Tile> PrepareTiles((double lat, double lon) point1, (double lat, double lon) point2, int size)
		{
			List<Tile> output = new List<Tile>();
			double top = Math.Max(point1.lat, point2.lat);
			double left = Math.Min(point1.lon, point2.lon);

			double latStep = (top - Math.Min(point1.lat, point2.lat)) / (double)size;
			double lonStep = (Math.Max(point1.lon, point2.lon) - left) / (double)size;


			for (int lat = 0; lat < size; lat++)
			{
				for (int lon = 0; lon < size; lon++)
				{
					Tile tile = new Tile(top - (latStep * lat), top - (latStep * lat) - latStep, left + (lonStep * lon), left + (lonStep * lon) + lonStep)
					{
						GridLat = lat,
						GridLon = lon
					};
					output.Add(tile);
				}
			}

			return output;
		}

		public List<Tile> LoadTiles(string path)
		{
			if (System.IO.Directory.Exists(path))
			{
				var tiles = new List<Tile>();
				var directory = new System.IO.DirectoryInfo(path);

				foreach(var file in directory.GetFiles())
				{
					if (TryLoadTile(file, out Tile tile))
						tiles.Add(tile);
				}
				return tiles;
			}
			return null;
		}

		public bool TryLoadTile(System.IO.FileInfo fileInfo, out Tile tile)
		{
			tile = _fsHelper.LoadObject<Tile>(fileInfo.FullName);
			if (tile != null)
				return true;
			return false;
		}

		public void MineRulesForTile(Tile tile, DataTable dataset)
		{
			var miner = new Miner();
			var filtered = dataset.AsEnumerable().Where(x => double.Parse(x.Field<string>("latitude")) >= tile.Bottom
														&& double.Parse(x.Field<string>("latitude")) <= tile.Top
														&& double.Parse(x.Field<string>("longitude")) >= tile.Left
														&& double.Parse(x.Field<string>("longitude")) <= tile.Right);
			var testFiltered = filtered.AsEnumerable();
			var geoTile = filtered.Any() ? filtered.CopyToDataTable() : dataset.Clone();

			var rules = miner.MineRules(geoTile, @"E:\Data\attributes.csv", @"E:\Data\unused_consequents.csv",
				@"E:\Data\results", 0.001, 0.01);

			tile.TotalElements = filtered.Count();
			tile.Rules = rules;
			
			_fsHelper.SaveObject<Tile>(tile, string.Format(@"E:\Data\results\tiles\rules\{0}x{1}_rules", tile.GridLon, tile.GridLat));
			if (!System.IO.Directory.Exists(rulesPath + @"\text"))
				System.IO.Directory.CreateDirectory(rulesPath + @"\text");
			System.IO.File.WriteAllLines(string.Format(@"E:\Data\results\tiles\rules\text\{0}x{1}_rules.txt", tile.GridLon, tile.GridLat), rules.Select(x => x.ToString()));
		}

		public void MineRulesForPolygon(PolygonViewModel polygon, string tableName)
		{
			Miner miner = new Miner();
			IEnumerable<Attribute> attributes = MapAttributes(polygon.Attributes
				.Where(x => x.IsEligibleForMining));

			Rule[] rules = miner.MineRules(polygon.Records, attributes,
				string.Format(@"E:\Data\results\{0}", polygon.Alias), 0.001, 0.01);

			System.IO.File.WriteAllLines(string.Format(@"E:\Data\results\{0}\{0}_rules.txt", polygon.Alias), rules.Select(x => x.ToString()));
			
		}

		private IEnumerable<Attribute> MapAttributes(IEnumerable<AttributeViewModel> attributeVMs)
		{
			List<Attribute> attributes = new List<Attribute>();
			foreach(var vm in attributeVMs)
			{
				attributes.Add(new Attribute(vm.AttributeName, vm.BadValue, attributes.Count, vm.IsConsequent));
			}
			return attributes;
		}

		PolygonViewModel GetPolygonForTile(Tile tile)
		{
			return new PolygonViewModel()
			{
				Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue),
				Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green),
				StrokeThickness = 2,
				Opacity = 0.7,
				Locations = new ObservableCollection<MapControl.Location>()
				{
					new Location(tile.NE.lat, tile.NE.lon),
					new Location(tile.SE.lat, tile.SE.lon),
					new Location(tile.SW.lat, tile.SW.lon),
					new Location(tile.NW.lat, tile.NW.lon)
				}
			};
			
			//NewPolygonLayer.Children.Add(polygon);
		}

		private void OnPolygonSelected(PolygonViewModel polygon)
		{
			if (polygon != SelectedPolygon)
			{
				if (SelectedPolygon != null)
				{
					SelectedPolygon.IsSelected = false;
					// Dispose table to prevent memory leaks.
					SelectedPolygon.DropSelection();
				}
				SelectedPolygon = polygon;
				SelectedPolygon.LoadData();
			}
		}
	}

	[Serializable]
	public class Tile
	{
		public Dictionary<string, int> AttributesCounts { get; set; } = new Dictionary<string, int>();
		public Dictionary<int, int> RulesLengthsCounts { get; set; } = new Dictionary<int, int>();
		public int TotalElements { get; set; }
		public int TotalRules { get => Rules?.Length ?? 0; }

		public int GridLat { get; set; }
		public int GridLon { get; set; }

		public double Top { get; set; }
		public double Bottom { get; set; }
		public double Left { get; set; }
		public double Right { get; set; }

		private Rule[] _rules;
		public Rule[] Rules 
		{ 
			get => _rules;
			set 
			{
				_rules = value;
				CountLengths(value);
			} 
		}

		public Tile(double north, double south, double west, double east)
		{
			this.Top = north;
			this.Bottom = south;
			this.Left = west;
			this.Right = east;
		}

		public Tile(int totalElements, double top, double bottom, double left, double right)
		{
			TotalElements = totalElements;
			Top = top;
			Bottom = bottom;
			Left = left;
			Right = right;
		}

		public (double lat, double lon) SE => (Bottom, Right);
		public (double lat, double lon) SW => (Bottom, Left);
		public (double lat, double lon) NE => (Top, Right);
		public (double lat, double lon) NW => (Top, Left);


		private void CountLengths(Rule[] rules)
		{
			foreach(var rule in rules)
			{
				int count = rule.antecedent.Length + rule.consequent.Length;
				if (RulesLengthsCounts.ContainsKey(count))
					RulesLengthsCounts[count]++;
				else
					RulesLengthsCounts.Add(count, 1);
			}
		}

		public override string ToString()
		{
			return string.Format("{0}x{1}", GridLat, GridLon);
		}
	}
}
