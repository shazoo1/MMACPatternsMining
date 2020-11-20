using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace MMACRulesMining.Desktop.Helpers
{
	public class DoubleRangeRule : ValidationRule
	{
		public double Min { get; set; }
		public double Max { get; set; } = 1;
		public bool Positive { get; set; } = false;
		public DoubleRangeRule() { }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (double.TryParse(value.ToString(), NumberStyles.Any, cultureInfo, out double parsed))
			{
				if (Positive && parsed <= 0)
					return new ValidationResult(false, $"Number must be greater than zero");
				if (parsed < Min || parsed > Max)
					return new ValidationResult(false, string.Format("Value must be in range {0} - {1}", Min, Max));
				return ValidationResult.ValidResult;
			}
			else
				return new ValidationResult(false, $"Number required");
		}
	}
}
