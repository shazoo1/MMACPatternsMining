using MapControl;
using MMACRulesMining.Data;
using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MMACRulesMining.Desktop.ViewModels
{
	public class PolygonViewModel : BaseViewModel
	{

		private static GlonassContext _context;
		public delegate void OnClicked(PolygonViewModel sender);

		public event OnClicked Clicked;
		public event EventHandler OnLoaded;

		private Thread _dataLoadingThread;

		public PolygonViewModel() : base()
		{
			_locations = new ObservableCollection<Location>();
			Attributes = new List<AttributeViewModel>();
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Records?.Dispose();
					Records = null;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		#region Bindables

		private ObservableCollection<Location> _locations;
		public ObservableCollection<Location> Locations
		{
			get => _locations;
			set
			{
				_locations = value;
				OnPropertyChanged(nameof(Locations));
			}
		}

		private Brush _fill;
		public Brush Fill
		{
			get => _fill;
			set
			{
				_fill = value;
				OnPropertyChanged(nameof(Fill));
			}
		}

		private Brush _stroke;
		public Brush Stroke
		{
			get => _stroke;
			set
			{
				_stroke = value;
				OnPropertyChanged(nameof(Stroke));
			}
		}

		private double _strokeThickness;
		public double StrokeThickness
		{
			get => _strokeThickness;
			set
			{
				_strokeThickness = value;
				OnPropertyChanged(nameof(StrokeThickness));
			}
		}

		private double _opacity;
		public double Opacity
		{
			get => _opacity;
			set
			{
				_opacity = value;
				OnPropertyChanged(nameof(Opacity));
			}
		}

		private string _title;
		public string Title 
		{ 
			get => _title; 
			set
			{
				_title = value;
				OnPropertyChanged(nameof(Title));
			}
		}

		private bool _isSelected;
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnPropertyChanged(nameof(IsSelected));
			}
		}

		public int Total { get => _records?.Rows.Count ?? 0; }

		private DataTable _records;
		public DataTable Records 
		{
			get => _records;
			set
			{
				_records = value;

				if (value != null)
				{
					Attributes = new List<AttributeViewModel>();

					foreach (DataColumn col in _records.Columns)
					{
						if (!Attributes.Any(x => x.AttributeName == col.ColumnName))
						{
							IEnumerable<string> possibleValues = _records.AsEnumerable()
								.Select(x => x[col].ToString())
								.Distinct();

							// If there are more than 10 distinct values in column, this column is more likely numeric.
							bool isCategorical = possibleValues.Count() <= 10;
							string badValue = "";

							if (isCategorical)
							{
								badValue = possibleValues.Contains("False") ? "False" : possibleValues.FirstOrDefault();
							}

							Attributes.Add(new AttributeViewModel
							{
								AttributeName = col.ColumnName,
								IsConsequent = false,
								IsSelected = isCategorical,
								PossibleValues = isCategorical ? possibleValues : null,
								BadValue = badValue,
								IsEligibleForMining = isCategorical
							});
						}
					}
				}

				OnPropertyChanged(nameof(Records));
				OnPropertyChanged(nameof(Total));
				OnPropertyChanged(nameof(Attributes));
			}
		}

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set
			{
				_isLoading = value;
				OnPropertyChanged(nameof(IsLoading));
			}
		}

		public List<AttributeViewModel> Attributes { get; set; }

		public string Alias { get; set; }

		#endregion

		#region Commands
		private ActionCommand _clickCommand;
		public ActionCommand ClickCommand
		{
			get 
			{
				if (_clickCommand == null)
				{
					_clickCommand = new ActionCommand(
						c =>
						{
							IsSelected = true;
							Clicked?.Invoke(this);
						},
						c => true);
				}
				return _clickCommand;
			}
		}
		#endregion

		public string SqlPolygon
		{
			get
			{
				string sqlPol = @"polygon '(";
				foreach(var location in Locations)
				{
					sqlPol += string.Format(CultureInfo.InvariantCulture, "({0}, {1}),", 
						location.Longitude, location.Latitude);
				}
				sqlPol = sqlPol.TrimEnd(',');
				sqlPol += @")'";
				return sqlPol;
			}
		}

		public void LoadData(string latColumn = "latitude", string lonColumn = "longitude", 
			Action preLoading = null, Action postLoading = null) 
		{
			if (_context != null)
			{
				new Thread(() =>
				{
					IsLoading = true;
					preLoading?.Invoke();

					if (_context.SelectWithinPolygon(SqlPolygon, latColumn, lonColumn, out DataTable tab))
					{
						Records = tab;
					}

					IsLoading = false;
					postLoading?.Invoke();
				})
				{
					Name = "Polygon Data Loading Thread"
				}.Start();
			}
		}

		public static void SetContext(GlonassContext context)
		{
			_context = context;
		}

		public void DropSelection()
		{
			Records?.Dispose();
			_dataLoadingThread?.Interrupt();
		}
	}

	public class AttributeViewModel : BaseViewModel
	{
		public string AttributeName { get; set; }

		private bool _isSelected;
		public bool IsSelected 
		{ 
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnPropertyChanged(nameof(IsSelected));
			}
		}

		private bool _isAntecedent;
		public bool IsConsequent
		{
			get => _isAntecedent;
			set
			{
				_isAntecedent = value;
				OnPropertyChanged(nameof(IsConsequent));
			}
		}

		public IEnumerable<string> PossibleValues { get; set; }

		private string _badValue;
		public string BadValue 
		{ 
			get => _badValue;
			set 
			{
				_badValue = value;
				OnPropertyChanged(nameof(BadValue));
			} 
		}

		public bool IsEligibleForMining { get; set; }

	}
}
