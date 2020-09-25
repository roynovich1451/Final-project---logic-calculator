using System.Windows;

namespace LogicCalculator
{
    class Evaluation
    {
        public Evaluation(string statement, string extra, int startLine, int endLine)
        {
            And(statement, startLine, endLine);

        }

        private void And(string statement, int startLine, int endLine)
        {
            if (statement.Contains("^") || statement.Contains("∧") || statement.Contains("&"))
            {                
                MessageBox.Show("Statement does not contain 'And'", "Rule Check");
            }
        }


        private void Or(string statement, char rule, int startLine, int endLine)
        {

        }
        private void Not(string statement, char rule, int startLine, int endLine)
        {

        }
        private void Z(string statement, char rule, int startLine, int endLine)
        {

        }
        private void D(string statement, char rule, int startLine, int endLine)
        {

        }

    }
}
