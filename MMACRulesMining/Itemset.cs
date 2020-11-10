using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMACRulesMining
{
    [Serializable]
    public class Itemset
    {
        public Itemset((Attribute, string)[] items, float support, int[] ids)
        {
            SetItems(items);
            Support = support;
            SetIds(ids);
        }

        private (Attribute attribute, string value)[] items;

        public (Attribute attribute, string value)[] GetItems()
        {
            var ordered = items.OrderByDescending(x => x.attribute.Consequent).ToArray();
            return ordered;
        }

        public void SetItems((Attribute attribute, string value)[] value)
        {
            items = value;
        }

        public float Support { get; set; }
        private int[] ids;

        public int[] GetIds()
        {
            return ids;
        }

        public void SetIds(int[] value)
        {
            ids = value;
        }

        // Index of the last element in the dataset, that has been added to current itemset.
        public int LastIndex => GetItems().Select(x => x.attribute.Position).Max();

        //public Itemset Add(Attribute attribute, string value)
        //{
        //    var ids = GetIds().Intersect(attribute.Values[value]);
        //    if (ids.Count() > 0)
        //    {
        //        Dictionary<Attribute, string> newItems = new Dictionary<Attribute, string>(Items);
        //        newItems.Add(attribute, value);
        //        int[] newIntersection = GetIds().Intersect(attribute.Values[value]).ToArray();
        //        float newSupport = (float)newIntersection.Length / (float)Attribute.TotalElements;
        //        return new Itemset(newItems, newSupport, newIntersection);
        //    }
        //    else
        //        return null;
        //}
    }
}
