using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace LogicCalculator
{
    class Evaluation
    {

        public Evaluation(List<Statement> statement_list, int current_row, string rule)
        {
            switch (rule)
            {
                case "and":
                    And(statement_list, current_row);
                    break;
                case "or":
                    break;
                case "contra":
                    break;
                case "not":
                    break;
                case "mp":
                    break;
                case "mt":
                    break;
                case "given":
                    break;
               
                    /*         case "":
                                break;*/
            }
        }

        private void And(List<Statement> statement_list, int current_row)
        {
            Statement current_statement = statement_list[current_row];
            Statement start_statement = statement_list[current_statement.start_line];
            Statement end_statement = statement_list[current_statement.end_line];

            if (current_statement.rule.Contains("i"))
            {
                if (current_statement.expression.Contains(start_statement.expression) && start_statement.rule == "given"
                    && current_statement.expression.Contains(start_statement.expression) && end_statement.rule == "given")
                {
                }
            }
            else
            {
               // if ()
            }
        }


        private void Or(string statement, char rule, int startLine, int endLine)
        {

        }
        private void Contra(string statement, char rule, int startLine, int endLine)
        {

        }
        private void Not(string statement, char rule, int startLine, int endLine)
        {

        }
      
        private void MP(string statement, char rule, int startLine, int endLine)
        {

        }
        private void MT(string statement, char rule, int startLine, int endLine)
        {

        }
        private bool IsProvenAlready(string var)
        {
            return false;
        }
    }
}
