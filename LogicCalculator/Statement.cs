namespace LogicCalculator
{
    class Statement
    {
        public string Expression { get; set; }
        public string Rule { get; set; }
        public string First_segment { get; set; }
        public string Second_segment { get; set; }
        public string Third_segment { get; set; }
        public Statement(string expression, string rule, string first_segment, string second_segment="", string third_segment="")
        {
            this.Expression = expression;
            this.Rule = rule;
            this.First_segment = first_segment;
            this.Second_segment = second_segment;
            this.Third_segment = third_segment;
        }

        public override string ToString()
        {
            return "Expression: " + Expression + " Rule: " + Rule + " First Segment: " + First_segment + " Second Segment: " + Second_segment + " Third Segment: " + Third_segment;
        }
    }
}
