namespace LogicCalculator
{
    class Statement
    {
        public string expression { get; set; }
        public string rule { get; set; }
        public string first_segment { get; set; }
        public string second_segment { get; set; }
        public string third_segment { get; set; }
        public Statement(string expression, string rule, string first_segment, string second_segment="", string third_segment="")
        {
            this.expression = expression;
            this.rule = rule;
            this.first_segment = first_segment;
            this.second_segment = second_segment;
            this.third_segment = third_segment;
        }

        public override string ToString()
        {
            return "Expression: " + expression + " Rule: " + rule + " First Segment: " + first_segment + " Second Segment: " + second_segment + " Third Segment: " + third_segment;
        }
    }
}
