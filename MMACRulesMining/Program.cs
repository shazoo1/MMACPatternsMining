using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;
using MMACRulesMining.Mappings;
using MMACRulesMining.Data;
using MMACRulesMining.Helpers;

namespace MMACRulesMining
{
	class Program
	{
		private static List<Attribute> attributes = new List<Attribute>();
		private static List<Itemset> itemsets = new List<Itemset>();
		private static List<Itemset> currentTierItemsets = new List<Itemset>();
		private static List<Itemset> lastTierItemsets = new List<Itemset>();
		private static List<Rule> rules = new List<Rule>();
		private static List<string> consequents = new List<string>();
		private static Logger _logger;
		private static FSHelper _fsHelper;
		private static int lastItemsetTier;

		private static Miner _miner;

		public static float MinSupport = 0.01F;
		public static float MinConfidence = 0.01F;

		static void Main(string[] args)
		{
			_logger = Logger.GetInstance();
			_fsHelper = new FSHelper();
			_logger.PrintMilestone("Reading configuration...");
			string datasetPath = ConfigurationManager.AppSettings.Get("path_dataset");
			string attributesPath = ConfigurationManager.AppSettings.Get("path_attributes");
			string unused_consequents_path = ConfigurationManager.AppSettings.Get("path_unused");
			string itemsetPathFormat = ConfigurationManager.AppSettings.Get("path_itemset_format");
			string rulesPathFormat = ConfigurationManager.AppSettings.Get("path_rules_format");
			_logger.PrintMilestone("Configuration successfully read.");

			_miner = new Miner();

			_logger.PrintMilestone("Started execution. Reading dataset.");
			
			var dt = _fsHelper.GetDataSet(datasetPath, ";");
			_logger.PrintExecutionTime("Loaded dataset.");

			_logger.PrintMilestone("Reading attributes.");
			consequents = _fsHelper.LoadUnusedConsequents(unused_consequents_path);
			attributes = _fsHelper.GetAttributesList(attributesPath, unused_consequents_path);

			foreach (DataRow tRow in dt.Rows)
			{
				foreach(var att in attributes)
				{
					att.Add(tRow[att.Name].ToString(), tRow["id"].ToString());
				}
			}

			SaveAsFeatures(dt, @"E:\Data\featurebundle.txt");

			dt.Dispose();
			_logger.PrintExecutionTime("Attributes read.");
			Attribute.TotalElements = dt.Rows.Count;

			_logger.PrintMilestone("Starting to combine attributes...");
			_logger.BeginLap();

			// Get initial, 2-tier itemsets to build the rest on them.
			CombineAttributesAndValues(attributes.ToArray(), 0, attributes.Count - 1, 0, 2);
			_logger.PrintLap(string.Format("Generated {0}-tier itemsets ({1} entries).", 2, currentTierItemsets.Count));
			SubmitItemset();
			_fsHelper.SaveItemsets(lastTierItemsets, string.Format(itemsetPathFormat, 2));

			for (int i = 3; i < attributes.Count; i++)
			{
				foreach (Itemset itemset in lastTierItemsets)
				{
					CombineAttributeWithItemset(itemset, attributes.ToArray());
				}
				SubmitItemset();
				if (lastTierItemsets.Count > 0)
				{
					_logger.PrintLap(string.Format("Generated {0}-tier itemsets ({1} entries).", i, lastTierItemsets.Count));
					_fsHelper.SaveItemsets(lastTierItemsets, string.Format(itemsetPathFormat, i));
				}
				else
				{
					lastItemsetTier = i - 1;
					_logger.PrintMilestone(string.Format("No itemsets found for Tier {0}. No further itemsets will be generated.", i));
					break;
				}
			}
			_logger.PrintExecutionTime(string.Format("Generated itemsets, {0} entries total.", itemsets.Count));

			_logger.PrintMilestone("Starting rules generation...");
			_logger.BeginLap();
			for (var i = 2; i <= lastItemsetTier; i++)
			{
				var tier = _fsHelper.LoadItemsets(string.Format(itemsetPathFormat, i)).OrderBy(x => x.Support).ToArray();
				foreach (Itemset itemset in tier)
				{
					BuildRules(itemset);
				}
				_logger.PrintLap(string.Format("Generated {0}-tier rules.", i));
			}
			_logger.PrintMilestone(string.Format("Ended rules generation. Total: {0} rules.", rules.Count()));
			var ordered = rules.OrderByDescending(x => x.confidence).ToArray();
			System.IO.File.WriteAllLines(string.Format(rulesPathFormat, MinSupport, MinConfidence), ordered.Select(x => x.ToString()));
		}

		/// <summary>
		/// Stores last itemset to itemsets collection, moves current to last and clears current.
		/// </summary>
		public static void SubmitItemset()
		{
			itemsets.AddRange(lastTierItemsets);
			lastTierItemsets = currentTierItemsets;
			currentTierItemsets = new List<Itemset>();
		}


		/// <summary>
		/// Checks, whether itemset passes MinSupport condition.
		/// </summary>
		/// <param name="itemset">Pairs of the attributes and the values of it.</param>
		/// <returns>False, if support is 0 or less than minSupport. True otherwise.</returns>
		static bool CheckSupport((Attribute, string)[] itemset, out float support, out List<int> intersection)
		{
			bool started = false;
			intersection = new List<int>();
			support = 0;
			
			foreach((Attribute att, string val) item in itemset)
			{
				if (item.att != null && item.val != null)
				{
					if (!started)
					{
						intersection = item.att.Values[item.val];
						started = true;
					}
					else
					{
						intersection = intersection.Intersect(item.att.Values[item.val]).ToList();
					}

					support = (float)intersection.Count() / (float)Attribute.TotalElements;
					if (support == 0 || support < MinSupport)
					{
						return false;
					}
				}
			}
			return true;
		}

		static float GetMathExpectation((Attribute, string)[] itemset)
		{
			float expectation = 1;
			foreach (var item in itemset)
			{
				expectation *= item.Item1.Support(item.Item2);
			}
			return expectation;
		}

		#region Combination

		static void CombineAttributeWithItemset(Itemset itemset, Attribute[] dataset)
		{
			CombineAttributesAndValues(dataset, itemset.LastIndex + 1, dataset.Length - 1,
				itemset.GetItems().Length, itemset.GetItems().Length + 1, 
				itemset.GetItems().Select(x => x.attribute).ToArray().Append(null).ToArray());
		}

		/// <summary>
		/// Combines attributes and calls <see cref="CombineValues(Attribute[], int, Dictionary{Attribute, string})"/> on each combination.
		/// Doesn't store attribute combinations.
		/// </summary>
		/// <param name="dataset">Dataset to search for combinations.</param>
		/// <param name="start">Starting position in the dataset.</param>
		/// <param name="end">Ending position in the dataset.</param>
		/// <param name="index">Current index in combination.</param>
		/// <param name="combLength">Desired length of the combination.</param>
		/// <param name="combination">Combination at current state.</param>
		static void CombineAttributesAndValues(Attribute[] dataset, int start, int end,
								int index, int combLength, Attribute[] combination = null)
		{
			if (combination == null)
				combination = new Attribute[combLength];

			// Add current combination, if it's ready.
			if (index == combLength)
			{
				if (combination.Any(x => !x.Consequent))
					CombineValues(combination, 0);
				return;
			}

			// Call self to add further combinations.
			for (int i = start; i <= end &&
					  end - i + 1 >= combLength - index; i++)
			{
				combination[index] = dataset[i];
				CombineAttributesAndValues(dataset, i + 1,
								end, index + 1, combLength, combination);
			}
		}

		static void CombineValues(Attribute[] dataset, int index = 0, (Attribute, string)[] data = null)
		{
			if (data == null)
				data = new (Attribute, string)[dataset.Length];
			if (index == dataset.Length)
			{
				if (CheckSupport(data, out float support, out List<int> intersection))
					currentTierItemsets.Add(new Itemset(data, support, intersection.ToArray()));
				return;
			}
			foreach (var val in dataset[index].Values)
			{
				if (CheckSupport(data, out float sup, out List<int> intersection))
				{
					data[index] = (dataset[index], val.Key);
					CombineValues(dataset, index + 1, data);
				}
			}
		}

		static void CombineIntoRules((Attribute, string)[] itemset, int antecedentLength,
			float itemsetSupport, int index = 0, int start = 0, (Attribute, string)[] antecedent = null,
			(Attribute, string)[] conditionBase = null)
		{
			if (antecedent == null)
				antecedent = new (Attribute, string)[antecedentLength];
			if (conditionBase == null)
				conditionBase = new (Attribute, string)[0];

			if (index == antecedentLength - conditionBase.Length)
			{
				for (int i = index; i < antecedentLength; i++)
				{
					antecedent[i] = conditionBase[i - index];
				}
				CheckSupport(antecedent, out float antecedentSupport, out List<int> intersection);
				CheckSupport(itemset.Except(antecedent).ToArray(), out float consequentSupport, out List<int> conInter);
				
				float confidence = itemsetSupport / antecedentSupport;
				float lift = itemsetSupport / (antecedentSupport * consequentSupport);

				if (confidence > MinConfidence)
				{
					var textCondition = antecedent.Select(x => (x.Item1.Name, x.Item2)).ToArray();
					var textConsequent = itemset.Except(antecedent).Select(x => (x.Item1.Name, x.Item2)).ToArray();

					rules.Add(new Rule(textCondition, textConsequent, confidence, lift));
				}
				return;
			}
			for (int i = start; i < itemset.Length && antecedentLength - conditionBase.Length - index - 1 <= itemset.Length - i;
				i++)
			{
				antecedent = ((Attribute, string)[])antecedent.Clone();
				antecedent[index] = itemset[i];
				CombineIntoRules(itemset, antecedentLength, itemsetSupport, index + 1, i + 1, antecedent, conditionBase);
			}
		}

		public static void BuildRules(Itemset itemset)
		{
			var ruleBase = itemset.GetItems().TakeWhile(x => x.attribute.Consequent).ToArray();
			var itemsetItems = itemset.GetItems().SkipWhile(x => x.attribute.Consequent).ToArray();
			for (int i = 1; i <= itemsetItems.Length + ruleBase.Length - 1; i++)
			{
				if (itemset.Support >= MinSupport && i >= ruleBase.Length)
					CombineIntoRules(itemsetItems, i, itemset.Support, conditionBase: ruleBase);
			}
		}

		public static void SaveAsFeatures(DataTable dt, string path)
		{
			List<string> features = new List<string>();
			foreach(DataRow row in dt.Rows)
			{
				List<string> line = new List<string>();
				foreach(DataColumn col in dt.Columns)
				{
					if (row[col].ToString() == "True")
						line.Add(col.ColumnName);
				}
				features.Add(string.Join(';',line.ToArray()));
			}
			System.IO.File.WriteAllLines(path, features);
		}

		#endregion
	}
}
