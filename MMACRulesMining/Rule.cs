using System;
using System.Collections.Generic;
using System.Text;

namespace MMACRulesMining
{
    [Serializable]
    public class Rule
    {
        public readonly (string attName, string attValue)[] antecedent;
        public readonly (string attName, string attValue)[] consequent;
        public readonly float confidence;
        public readonly float lift;

        public Rule() { }

        public Rule((string attName, string attValue)[] antecedent, (string attName, string attValue)[] consequent, float confidence)
        {
            this.antecedent = antecedent;
            this.consequent = consequent;
            this.confidence = confidence;
        }

        public Rule((string attName, string attValue)[] antecedent, (string attName, string attValue)[] consequent, float confidence, float lift) : this(antecedent, consequent, confidence)
        {
            this.lift = lift;
        }

        public override string ToString()
        {
            string leftPart = "[";
            string rightPart = "[";

            foreach (var item in antecedent)
                leftPart += string.Format("{0}:{1} | ", item.attName, item.attValue);
            foreach (var item in consequent)
                rightPart += string.Format("{0}:{1} | ", item.attName, item.attValue);

            leftPart = leftPart.Substring(0, leftPart.Length - 3) + "]";
            rightPart = rightPart.Substring(0, rightPart.Length - 3) + "]";

            return string.Format("{0} => {1}, {2}% (Lift: {3})", leftPart, rightPart, confidence * 100, lift);
        }
    }
}
