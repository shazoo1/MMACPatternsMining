using MMACRulesMining.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;

namespace MMACRulesMining
{
	public class Miner
	{
		private Logger _logger;
		private FSHelper _fsHelper;
		public List<Itemset> currentTierItemsets { get; private set; } = new List<Itemset>();
		public List<Itemset> lastTierItemsets { get; private set; } = new List<Itemset>();
		public List<Itemset> itemsets { get; private set; } = new List<Itemset>();
		public List<Rule> Rules { get; private set; } = new List<Rule>();
		public static float MinSupport = 0.01F;
		public static float MinConfidence = 0.01F;


		public Miner()
		{
			_logger = Logger.GetInstance();
			_fsHelper = new FSHelper();
		}

		private DataTable ReadDataSet(string path)
		{
			_logger.PrintMilestone("Started execution. Reading dataset.");
			var dataset = _fsHelper.GetDataSet(path, ";");
			_logger.PrintExecutionTime("Loaded dataset.");
			return dataset;
		}

		public Rule[] MineRules(DataTable dataset, string attributesPath, string antecedentPath, double minSupport, double minConfidence,
			string localPath)
		{

			_logger.PrintMilestone("Reading attributes.");
			var antecedents = _fsHelper.LoadUnusedConsequents(antecedentPath);
			var attributes = _fsHelper.GetAttributesList(attributesPath, antecedentPath);

			System.IO.Directory.CreateDirectory(localPath + "\\itemsets");

			foreach (DataRow tRow in dataset.Rows)
			{
				foreach (var att in attributes)
				{
					att.Add(tRow[att.Name].ToString(), tRow["id"].ToString());
				}
			}
			_logger.PrintExecutionTime("Attributes read.");
			Attribute.TotalElements = dataset.Rows.Count;

			_logger.PrintMilestone("Starting to combine attributes...");
			_logger.BeginLap();


			/// <summary>
			/// Stores last itemset to itemsets collection, moves current to last and clears current.
			/// </summary>
			void SubmitItemset()
			{
				itemsets.AddRange(lastTierItemsets);
				lastTierItemsets = currentTierItemsets;
				currentTierItemsets = new List<Itemset>();
			}

			// Get initial, 2-tier itemsets to build the rest on them.
			CombineAttributesAndValues(attributes.ToArray(), 0, attributes.Count - 1, 0, 2);
			_logger.PrintLap(string.Format("Generated {0}-tier itemsets ({1} entries).", 2, currentTierItemsets.Count));
			SubmitItemset();
			_fsHelper.SaveItemsets(lastTierItemsets, string.Format(localPath + "\\itemsets\\tier{0}", 2));
			int lastItemsetTier = 2;

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
					_fsHelper.SaveItemsets(lastTierItemsets, string.Format(localPath + "\\itemsets\\tier{0}", i));
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
				var tier = _fsHelper.LoadItemsets(string.Format("{0}\\itemsets\\tier{1}", localPath, i)).OrderBy(x => x.Support).ToArray();
				foreach (Itemset itemset in tier)
				{
					BuildRules(itemset);
				}
				_logger.PrintLap(string.Format("Generated {0}-tier rules.", i));
			}
			_logger.PrintMilestone(string.Format("Ended rules generation. Total: {0} rules.", Rules.Count()));
			var ordered = Rules.OrderByDescending(x => x.confidence).ToArray();
			System.IO.File.WriteAllLines(string.Format(localPath + "\\rules\\minsupp{0}_minConf{1}_lines.txt", MinSupport, MinConfidence), ordered.Select(x => x.ToString()));
			_fsHelper.SaveObject<List<Rule>>(ordered.ToList(), string.Format(localPath + "\\rules\\minsupp{0}_minConf{1}.rules", MinSupport, MinConfidence));
			return ordered;

		}

		/// <summary>
		/// Checks, whether itemset passes MinSupport condition.
		/// </summary>
		/// <param name="itemset">Pairs of the attributes and the values of it.</param>
		/// <returns>False, if support is 0 or less than minSupport. True otherwise.</returns>
		internal bool CheckSupport((Attribute, string)[] itemset, out float support, out List<int> intersection)
		{
			bool started = false;
			intersection = new List<int>();
			support = 0;

			foreach ((Attribute att, string val) item in itemset)
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

		internal float GetMathExpectation((Attribute, string)[] itemset)
		{
			float expectation = 1;
			foreach (var item in itemset)
			{
				expectation *= item.Item1.Support(item.Item2);
			}
			return expectation;
		}

		internal void CombineAttributeWithItemset(Itemset itemset, Attribute[] dataset)
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
		internal void CombineAttributesAndValues(Attribute[] dataset, int start, int end,
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

		internal void CombineValues(Attribute[] dataset, int index = 0, (Attribute, string)[] data = null)
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

		internal void CombineIntoRules((Attribute, string)[] itemset, int antecedentLength,
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

					Rules.Add(new Rule(textCondition, textConsequent, confidence, lift));
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

		public void BuildRules(Itemset itemset)
		{
			var ruleBase = itemset.GetItems().TakeWhile(x => x.attribute.Consequent).ToArray();
			var itemsetItems = itemset.GetItems().SkipWhile(x => x.attribute.Consequent).ToArray();
			for (int i = 1; i <= itemsetItems.Length + ruleBase.Length - 1; i++)
			{
				if (itemset.Support >= MinSupport && i >= ruleBase.Length)
					CombineIntoRules(itemsetItems, i, itemset.Support, conditionBase: ruleBase);
			}
		}
	}
}
