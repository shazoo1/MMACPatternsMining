using System;
using System.Collections.Generic;
using System.Text;

namespace MMACRulesMining
{
    [Serializable]
    public class Attribute
    {
        /// <summary>
        /// Total elements in the dataset. Should be set once.
        /// </summary>
        public static int TotalElements { get; set; }
        public string Name { get; private set; }
        public string BadValue { get; private set; }

        //Indicates, that element may participate only on the left side of the rule.
        public bool Consequent { get; private set; }
        public int Position { get; set; }
        /// <summary>
        /// Attribute value and ids of the responsive entries.
        /// </summary>
        public IDictionary<string, List<int>> Values { get; set; }

        public Attribute(string name, string badValue, int position)
        {
            Name = name;
            BadValue = badValue;
            Position = position;
            Values = new Dictionary<string, List<int>>();
        }

        public Attribute(string name, string badValue, int position, bool consequent) : this(name, badValue, position)
        {
            Consequent = consequent;
        }

        public void Add(string value, string tid)
        {
            if (value != BadValue)
            {
                if (!Values.ContainsKey(value))
                    Values.Add(value, new List<int>());
                Values[value].Add(int.Parse(tid));
            }
        }

        /// <summary>
        /// Returns Support of the attribute value.
        /// </summary>
        /// <param name="attributeValue"></param>
        /// <returns>Support</returns>
        public float Support(string attributeValue)
        {
            if (Values.ContainsKey(attributeValue))
            {
                return (float)Values[attributeValue].Count / (float)TotalElements;
            }
            return 0;
        }

        public override string ToString()
        {
            return string.Format("{0} | {1}: {2} valid entries.", Position, Name, Values.Values.Count);
        }
    }
}
