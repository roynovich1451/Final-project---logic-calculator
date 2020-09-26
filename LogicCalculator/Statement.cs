namespace LogicCalculator
{
    class Statement
    {
        public string expression { get; set; }
        public string rule { get; set; }
        public int start_line { get; set; }
        public int end_line { get; set; }
        public Statement(string expression, string rule, int start_line, int end_line)
        {
            this.expression = expression;
            this.rule = rule;
            this.start_line = start_line;
            this.end_line = end_line;
        }
    }
}
