using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace MMACRulesMining.Helpers
{
	public class FSHelper
	{

		public List<Attribute> GetAttributesList(string fileName, string unusedConsequentsPath)
		{
			var attributesWithBadValues = GetDataSet(fileName, ";");
			var attributes = new List<Attribute>();
			var consequents = LoadUnusedConsequents(unusedConsequentsPath);

			if (attributesWithBadValues.Columns.Contains("attribute") && attributesWithBadValues.Columns.Contains("bad_value"))
			{
				foreach (DataRow attribute in attributesWithBadValues.Rows)
				{
					attributes.Add(new Attribute(attribute["attribute"].ToString(),
						attribute["bad_value"].ToString(), attributes.Count, consequents.Contains(attribute["attribute"])));
				}
			}
			return attributes;
		}

		public DataTable GetDataSet(string fileName, string delimiter)
		{
			using (var reader = new StreamReader(fileName))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				csv.Configuration.Delimiter = delimiter;
				csv.Configuration.HasHeaderRecord = true;
				csv.Configuration.BadDataFound = (context) => Console.WriteLine(string.Format("Bad field: {0}", context.Field));
				// Do any configuration to `CsvReader` before creating CsvDataReader.
				using (CsvDataReader dr = new CsvDataReader(csv))
				{
					var dt = new DataTable();
					dt.Load(dr);
					return dt;
				}
			}
		}

		/// <summary>
		/// Saves attributes combination to the disk.
		/// </summary>
		/// <param name="itemsets"></param>
		/// <param name="path"></param>
		public void SaveItemsets(List<Itemset> itemsets, string path)
		{
			BinaryFormatter binFormatter = new BinaryFormatter();
			var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);

			binFormatter.Serialize(fs, itemsets);
			fs.Close();
		}

		public void SaveObject<T>(T objects, string path)
		{
			BinaryFormatter binFormatter = new BinaryFormatter();
			var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);

			binFormatter.Serialize(fs, objects);
			fs.Close();
		}

		public T LoadObject<T>(string path)
		{
			try
			{
				BinaryFormatter binFormatter = new BinaryFormatter();
				var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

				var obj = (T)binFormatter.Deserialize(fs);
				return obj;
			}
			catch(Exception)
			{
				return default;
			}
		}

		public List<Itemset> LoadItemsets(string path)
		{
			BinaryFormatter binFormatter = new BinaryFormatter();
			var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

			var itemsets = (List<Itemset>)binFormatter.Deserialize(fs);
			fs.Close();
			return itemsets;
		}

		public List<string> LoadUnusedConsequents(string path)
		{
			var attr = GetDataSet(path, ";").AsEnumerable()
				.Select(x => x[0].ToString()).ToList();
			return attr;
		}

		public void SaveAttributes(List<string> attributes, string path, 
			string badValue = "False", string delimiter = ";")
		{
			using (var writer = new StreamWriter(path, false))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.Configuration.Delimiter = delimiter;
				csv.WriteRecord<(string attribute, string bad_value)>(("attribute", "bad_value"));
				foreach (var att in attributes)
				{
					csv.WriteRecord<(string attribute, string bad_value)>((att, badValue));
				}
			}
		}

		public void SaveList(List<string> entry, string path, string delimiter = ";")
		{
			using (var writer = new StreamWriter(path, false))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.Configuration.Delimiter = delimiter;
				csv.WriteRecords(entry);
			}
		}

		public string[] ReadLines(string path)
		{
			if (File.Exists(path))
				return System.IO.File.ReadAllLines(path);
			return new string[0];
		}
	}
}
