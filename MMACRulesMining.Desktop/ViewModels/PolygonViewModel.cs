using MapControl;
using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Windows.Media;

namespace MMACRulesMining.Desktop.ViewModels
{
	public class PolygonViewModel : BaseViewModel
	{

		public PolygonViewModel() : base()
		{
			_locations = new ObservableCollection<Location>();
		}

		public delegate void OnClicked(PolygonViewModel sender);
		public event OnClicked Clicked;

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
				OnPropertyChanged(nameof(Records));
				OnPropertyChanged(nameof(Total));
			}
		}
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
					sqlPol += string.Format("({0}, {1}),", location.Longitude, location.Latitude);
				}
				sqlPol = sqlPol.TrimEnd(',');
				sqlPol += @")'";
				return sqlPol;
			}
		}
	}
}
