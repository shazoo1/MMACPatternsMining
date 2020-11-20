using MapControl;
using MMACRulesMining.Data;
using MMACRulesMining.Desktop.Helpers;
using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

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
		private Miner _miner;

		public DataTable Dataset;

		#region Bindings
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

		private double _minSupport;
		public double MinSupport
		{
			get => _minSupport;
			set
			{
				_minSupport = value;
				OnPropertyChanged(nameof(MinSupport));
			}
		}

		private double _minConfidence;
		public double MinConfidence
		{
			get => _minConfidence;
			set
			{
				_minConfidence = value;
				OnPropertyChanged(nameof(MinConfidence));
			}
		}

		public Location CenterPoint { get; set; } = new Location(0, 0);


		private bool _isMining;
		public bool IsMining
		{
			get => _isMining;
			set
			{
				_isMining = value;
				OnPropertyChanged(nameof(IsMining));

			}
		}

		public bool CanMine 
		{
			get => SelectedPolygon != null && !SelectedPolygon.IsLoading
					&& SelectedPolygon.Records != null
					&& MinSupport > 0 && MinSupport <= 1
					&& MinConfidence > 0 && MinConfidence <= 1
					&& !IsMining;

		}
		#endregion

		public ActionCommand MineCommand { get; private set; }
		public ActionCommand RefreshCommand { get; private set; }

		private MapTileLayer _mapLayer;
		public MapTileLayer MapLayer
		{

			get
			{
				if (_mapLayer == null)
					_mapLayer = new MapTileLayer
					{
						TileSource = new TileSource { UriFormat = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" },
						SourceName = "OpenStreetMap",
						Description = "© [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)"
					};
				return _mapLayer;
			}
		}

		public MainViewModel()
		{
			_fsHelper = new FSHelper();
			_context = new GlonassContext();

			_minSupport = 0.002;
			_minConfidence = 0.01;

			MineCommand = new ActionCommand(
				c => MineRulesForPolygon(SelectedPolygon, "Dataset_Kazan_filtered"),
				c => true
			);

			RefreshCommand = new ActionCommand(
				c => LoadPolygons(),
				c => true
			);

			PolygonViewModel.SetContext(_context);

			Polygons = new ObservableCollection<PolygonViewModel>();
			LoadPolygons();

			cc = ((ne.lat + sw.lat) / 2, (ne.lon + sw.lon) / 2);
			CenterPoint.Latitude = (ne.lat + sw.lat) / 2;
			CenterPoint.Longitude = (ne.lon + sw.lon) / 2;
			OnPropertyChanged(nameof(CenterPoint));
		}

		public void LoadPolygons(string path = @"E:\Data\polygons")
		{
			if (Polygons != null && Polygons.Count > 0)
			{
				foreach (PolygonViewModel polygon in Polygons)
				{
					polygon.Clicked -= OnPolygonSelected;
					polygon.Dispose();
				}
			}
			Polygons = new ObservableCollection<PolygonViewModel>();

			foreach (PolygonViewModel polygon in PolygonsImporter.ImportDirectory(path))
			{
				polygon.Clicked += OnPolygonSelected;
				Polygons.Add(polygon);
			}
			OnPropertyChanged(nameof(Polygons));
		}


		public void MineRulesForPolygon(PolygonViewModel polygon, string tableName)
		{
			var thread = new Thread(() =>
			{
				IsMining = true;

				if (_miner == null)
					_miner = new Miner();

				IEnumerable<Attribute> attributes = MapAttributes(polygon.Attributes
					.Where(x => x.IsEligibleForMining && x.IsSelected));

				Rule[] rules = _miner.MineRules(polygon.Records, attributes,
					string.Format(@"E:\Data\results\{0}", polygon.Alias), 0.001, 0.01);

				string fileName = string.Format(@"E:\Data\results\{0}\{1}_rules.txt", polygon.Alias, 
					DateTime.Now.ToString("ddMMyyyyHHmmss"));
				System.IO.File.WriteAllLines(fileName, rules.Select(x => x.ToString()));

				if (System.IO.File.Exists(fileName))
					Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });

				IsMining = false;
			});
			thread.Name = "Mining Thread";
			thread.Start();
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
				SelectedPolygon.LoadData(preLoading: () => OnPropertyChanged(nameof(CanMine)),
					postLoading: () => OnPropertyChanged(nameof(CanMine)));
			}
		}
	}
}
