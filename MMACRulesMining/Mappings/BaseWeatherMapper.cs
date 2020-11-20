using MMACRulesMining.Data;
using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MMACRulesMining.Mappings
{
	public class BaseWeatherMapper
	{
		protected DataTable featured = new DataTable("fweather");
		protected List<string> features = new List<string>();
		protected Dictionary<string, string> termsDictionary = new Dictionary<string, string>();
		protected GlonassContext _context;
		protected FSHelper _fsHelper;

		public BaseWeatherMapper(GlonassContext context)
		{
			_context = context;
			_fsHelper = new FSHelper();
			featured.Columns.Add("DateTime", typeof(DateTime));
			FillDictionary();
		}

		protected virtual void FillDictionary()
		{
			// Weather events
			termsDictionary.Add("Морось.", "drizzle");
			termsDictionary.Add("Облака покрывали половину неба или менее в течение всего соответствующего периода.", "clouds_below_fifty");
			termsDictionary.Add("Облака покрывали более половины неба в течение одной части соответствующего периода и половину или менее в течение другой части периода.", "clouds_volatile");
			termsDictionary.Add("Метель", "snowstorm");
			termsDictionary.Add("Туман или ледяной туман или сильная мгла.", "fog");
			termsDictionary.Add("Дождь.", "rain");
			termsDictionary.Add("Явление, связанное с переносом ветром твердых частиц, видимость пониженная.", "wind_dust");
			termsDictionary.Add("Гроза (грозы) с осадками или без них.", "thunderstorm");
			termsDictionary.Add("Дождь со снегом или другими видами твердых осадков", "rain_snow");
			termsDictionary.Add("Буря", "hailstorm");
			termsDictionary.Add("Ливень (ливни).", "shower");
			termsDictionary.Add("Облака покрывали более половины неба в течение всего соответствующего периода.", "clouds_over_fifty");
			termsDictionary.Add("Снег и/или другие виды твердых осадков", "snow");

			// Groud surface
			termsDictionary.Add("Слежавшийся или мокрый снег (со льдом или без него), покрывающий по крайней мере половину поверхности почвы, но почва не покрыта полностью.", "surface_snow_wet_over_fifty");
			termsDictionary.Add("Cухая (без трещин, заметного количества пыли или сыпучего песка)", "surface_dry");
			termsDictionary.Add("Ровный слой слежавшегося или мокрого снега покрывает поверхность почвы полностью.", "surface_snow_wet_hundred");
			termsDictionary.Add("Слежавшийся или мокрый снег (со льдом или без него), покрывающий менее половины поверхности почвы.", "surface_snow_wet_below_fifty");
			termsDictionary.Add("Поверхность почвы влажная.", "surface_wet");
			termsDictionary.Add("Тонкий слой несвязанной сухой пыли или песка покрывает почву полностью.", "surface_dust_hundred");
			termsDictionary.Add("Ровный слой сухого рассыпчатого снега покрывает поверхность почвы полностью.", "surface_snow_dry_hundred");
			termsDictionary.Add("Поверхность почвы замерзшая.", "surface_frozen");
			termsDictionary.Add("Поверхность почвы сырая (вода застаивается на поверхности и образует малые или большие лужи).", "surface_wet_puddles");
		}

		public virtual void GetFeatures(GlonassContext context, string path = null)
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

			featured.TableName = "featured_weather";
			_context.SaveToTable(featured, true);
		}

		protected virtual void ProcessWindow(List<Wfilled> window)
		{

		}

		protected virtual string ProcessFeature(string term, ref List<string> features)
		{
			if (termsDictionary.TryGetValue(term, out string translation))
				term = translation;

			if (!features.Contains(term))
			{
				var col = new DataColumn(term, typeof(string)) { DefaultValue = "False" };
				featured.Columns.Add(col);
				features.Add(term);
			}
			return term;
		}
	}
}
