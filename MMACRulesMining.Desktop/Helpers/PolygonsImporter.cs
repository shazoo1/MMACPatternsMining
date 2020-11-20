using MapControl;
using MMACRulesMining.Desktop.ViewModels;
using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MMACRulesMining.Desktop.Helpers
{
	public class PolygonsImporter
	{
		private static PolygonsImporter _instance;
		private static FSHelper _fSHelper;

		public static List<PolygonViewModel> Polygons { get; private set; }

		private PolygonsImporter()
		{
			Polygons = new List<PolygonViewModel>();
			_fSHelper = new FSHelper();
		}

		public static IEnumerable<PolygonViewModel> ImportDirectory(string dirPath)
		{
			if (_instance == null)
				_instance = new PolygonsImporter();

			List<PolygonViewModel> polygons = new List<PolygonViewModel>();

			foreach(string filePath in System.IO.Directory.GetFiles(dirPath))
			{
				if (ReadPolygonFromFile(filePath,
					out PolygonViewModel polygon))
					polygons.Add(polygon);
			}

			return polygons;
		}

		public static bool ReadPolygonFromFile(string filePath, out PolygonViewModel polygon)
		{
			#region Line reading methods

			(double longitute, double latitude)? ReadPointFromLine(string line)
			{
				// Split line into parts and trim both
				string[] point = line.Split(',').Select(x => x.Trim(' ')).ToArray();

				if (point.Length == 2 && 
					double.TryParse(point[0], System.Globalization.NumberStyles.Any,
						CultureInfo.InvariantCulture, out double longitude) &&
					double.TryParse(point[1], System.Globalization.NumberStyles.Any,
						CultureInfo.InvariantCulture, out double latitude))
					return (longitude, latitude);

				return null;
			}

			(string attributeKey, string attributeValue)? ReadAttributeFromLine(string line)
			{
				string[] pair = line.Split(':').Select(x => x.Trim(' ')).ToArray();

				if (pair.Length == 2)
				{
					return (pair[0], pair[1]);
				}

				return null;
			}

			#endregion

			string[] textPolygon = _fSHelper.ReadLines(filePath);

			PolygonViewModel polygonVM = new PolygonViewModel()
			{
				Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue),
				Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green),
				StrokeThickness = 2,
				Opacity = 0.6,
			};

			try
			{
				foreach (string line in textPolygon)
				{
					if (!string.IsNullOrEmpty(line))
					{
						if (char.IsDigit(line[0]))
						{
							var point = ReadPointFromLine(line);
							if (point.HasValue)
								polygonVM.Locations.Add(new Location(point.Value.latitude, point.Value.longitute));
						}
						else 
						{
							var keyValue = ReadAttributeFromLine(line);
							if (keyValue.HasValue)
							{
								if (keyValue.Value.attributeKey.ToLower() == "name")
								{
									polygonVM.Title = keyValue.Value.attributeValue;
								}
								else if (keyValue.Value.attributeKey.ToLower() == "alias")
								{
									polygonVM.Alias = keyValue.Value.attributeValue;
								}
							}
						}
					}
				}
				polygon = polygonVM;
				return true;
			}
			catch (Exception e) 
			{
				Debug.WriteLine(e);
				polygon = null;
				return false;
			}
		}

	}
}
