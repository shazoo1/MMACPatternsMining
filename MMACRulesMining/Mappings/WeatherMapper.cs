using CsvHelper;
using Microsoft.EntityFrameworkCore;
using MMACRulesMining.Data;
using MMACRulesMining.Helpers;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MMACRulesMining.Mappings
{
	/// <summary>
	/// Maps weather to defined features.
	/// </summary>
	public class WeatherMapper : BaseWeatherMapper
	{

		public WeatherMapper(GlonassContext context) : base(context)
		{
			
		}

		protected override void FillDictionary() 
		{
			base.FillDictionary();
		}

		public override void GetFeatures(GlonassContext context, string path = null)
		{
			var weather = context.Wfilled.OrderBy(x => x.Datetime).ToArray();

			for (int i = 0; i < weather.Count();)
			{
				// 1 day window to count mean and max
				List<Wfilled> window = new List<Wfilled>();
				do
				{
					window.Add(weather[i]);
					i++;
				}
				while (weather[i - 1].Datetime.Value.Hour != 6 && i < weather.Count());
				ProcessWindow(window);
			}

			if (path != null)
				SaveFeatures(path);

			featured.TableName = "featured_weather";
			_context.SaveToTable(featured, true);
		}

		public void SaveFeatures(string path)
		{
			using (var textWriter = File.CreateText(path))
			using (var csv = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
			{
				csv.Configuration.Delimiter = ";";
				csv.Configuration.Quote = '"';

				// Write columns
				foreach (DataColumn column in featured.Columns)
				{
					csv.WriteField(column.ColumnName);
				}
				csv.NextRecord();

				// Write row values
				foreach (DataRow row in featured.Rows)
				{
					for (var i = 0; i < featured.Columns.Count; i++)
					{
						csv.WriteField(row[i]);
					}
					csv.NextRecord();
				}
			}
		}

		protected override void ProcessWindow(List<Wfilled> window)
		{
			List<string> todayFeatures = new List<string>();
			string feature;

			// Process features of the day
			if ((feature = ProcessMeanTemp(window, ref features)) != null)
				todayFeatures.Add(feature);
			if ((feature = ProcessMaxTemp(window, ref features)) != null)
				todayFeatures.Add(feature);
			if ((feature = ProcessSnowDepth(window, ref features)) != null)
				todayFeatures.Add(feature);
			if ((feature = ProcessPrecipitation(window, ref features)) != null)
				todayFeatures.Add(feature);

			foreach(Wfilled entry in window)
			{
				List<string> currentFeatures = new List<string>();
				currentFeatures.AddRange(todayFeatures);

				if ((feature = ProcessWindGusts(entry, ref features)) != null)
					currentFeatures.Add(feature);

				// Translate features and leave as is.
				if ((feature = entry.Event1) != null)
				{
					feature = ProcessFeature(feature, ref features);
					currentFeatures.Add(feature);
				}
				if ((feature = entry.Event2) != null)
				{
					feature = ProcessFeature(feature, ref features);
					currentFeatures.Add(feature);
				}
				if ((feature = entry.Surface) != null)
				{
					feature = ProcessFeature(feature, ref features);
					currentFeatures.Add(feature);
				}


				var newRow = featured.NewRow();

				foreach (var f in currentFeatures)
					newRow[f] = "True";

				newRow["Datetime"] = entry.Datetime;
				featured.Rows.Add(newRow);
			}
		}

		#region Daily features

		private string ProcessMeanTemp(List<Wfilled> window, ref List<string> features)
		{
			var meanTemp = window.Average(x => x.Temp);
			string term = "";
			DataColumn col;
			if (meanTemp <= -20)
			{
				term = "freezing";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (meanTemp <= -7)
			{
				term = "very_cold";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (meanTemp <= 0)
			{
				term = "cold";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			return null;
		}

		private string ProcessMaxTemp(List<Wfilled> window, ref List<string> features)
		{
			string term = "";
			var maxTemp = window.Max(x => x.Maxtemp);
			DataColumn col;
			if (maxTemp >= 43)
			{
				term = "burning";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (maxTemp >= 32)
			{
				term = "very_hot";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (maxTemp >= 25)
			{
				term = "hot";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			return null;
		}

		private string ProcessSnowDepth(List<Wfilled> window, ref List<string> features)
		{
			float snowDepth = window.Sum(x =>
			{
				if (float.TryParse(x.Snowdepth, NumberStyles.Any, CultureInfo.InvariantCulture, out float dep))
					return dep;
				return 0;
			});
			string term = "";

			DataColumn col;
			if (snowDepth >= 20)
			{
				term = "deep_snow";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (snowDepth >= 10)
			{
				term = "medium_snow";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (snowDepth >= 1)
			{
				term = "little_snow";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			return null;
		}

		private string ProcessPrecipitation(List<Wfilled> window, ref List<string> features)
		{
			float precipitation = window.Sum(x =>
			{
				if (float.TryParse(x.Precip, out float res))
					return res;
				return 0;
			});
			string term = "";

			DataColumn col;
			if (precipitation >= 150)
			{
				term = "excep_precip";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (precipitation >= 100)
			{
				term = "severe_precip";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			else if (precipitation >= 50)
			{
				term = "high_precip";
				if (!features.Contains(term))
				{
					col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
					featured.Columns.Add(col);
					features.Add(term);
				}
				return term;
			}
			return null;
		}

		#endregion

		private string ProcessWindGusts(Wfilled entry, ref List<string> features)
		{
			if (entry.Windgust.HasValue)
			{
				string term = "";
				DataColumn col;
				var speed = entry.Windgust.Value;
				if (speed >= 32)
				{
					term = "hurricane";
					if (!features.Contains(term))
					{
						col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
						featured.Columns.Add(col);
						features.Add(term);
					}
					return term;
				}
				else if (speed >= 25)
				{
					term = "stormy";
					if (!features.Contains(term))
					{
						col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
						featured.Columns.Add(col);
						features.Add(term);
					}
					return term;
				}
				else if (speed >= 17)
				{
					term = "windy";
					if (!features.Contains(term))
					{
						col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
						featured.Columns.Add(col);
						features.Add(term);
					}
					return term;
				}
			}
			return null;
		}
	}
}
