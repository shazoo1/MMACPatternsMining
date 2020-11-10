using MMACRulesMining.Data;
using MMACRulesMining.Helpers;
using MMACRulesMining.Mappings;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace MMACRulesMining.Desktop.ViewModels
{
	public class ActionSelectionViewModel : BaseViewModel
	{
		private static ActionSelectionViewModel _instance;
		private static Window _window;

        #region Bindables
        private string _pathToFeatures;
		public string PathToFeatures
		{ 
			get => _pathToFeatures;
			set 
			{
				_pathToFeatures = value;
				OnPropertyChanged(nameof(PathToFeatures));
			}
		}

		private string LeftTableName { get; set; } = "Dataset_Kazan_filtered";
		private string RightTableName { get; set; } = "featured_weather";
		private string LeftKey { get; set; } = "ceil_time_3h(\"Dataset_Kazan_filtered\".datetime)";
		private string RightKey { get; set; } = "datetime";
		private string NewTableName { get; set; } = "cards_with_features";

		#endregion
		public ICommand SaveFeaturesCommand { get; set; }
		public ICommand JoinCommand { get; set; }

        private GlonassContext _context;
		private WeatherMapper _mapper;

		protected ActionSelectionViewModel(GlonassContext context)
		{
			_context = context;
			_mapper = new WeatherMapper(_context);
			
			SaveFeaturesCommand = new ActionCommand(
				cmd => MineFeatures(),
				cmd => PathToFeatures != null);

			JoinCommand = new ActionCommand(
				cmd => _context.CreateAsJoin(LeftTableName, LeftKey, RightTableName, RightKey, NewTableName, true),
				cmd => true);

			PathToFeatures = "E:\\Data\\featurebundle2.txt";
		}

		public static void ShowActionSelection(GlonassContext context)
		{
			if (_window != null)
			{
				_window.Activate();
				return;
			}
			if (_instance == null)
				_instance = new ActionSelectionViewModel(context);

			_window = new Window()
			{
				Title = "Select action",
				SizeToContent = SizeToContent.WidthAndHeight,
				Content = new UI.ActionSelectionView(),
				DataContext = _instance
			};

			_window.Show();
		}

		private void MineFeatures()
		{
			var context = new GlonassContext();
			_mapper.GetFeatures(context, PathToFeatures);
			//_mapper.SaveToTable(features);
		}
	}
}
