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
                    Or(statement_list, current_row);
                    break;
                case "pbc":
                    PBC(statement_list, current_row);
                    break;
                case "not":
                    Not(statement_list, current_row);
                    break;
                case "mp":
                    MP(statement_list, current_row);
                    break;
                case "mt":
                    MT(statement_list, current_row);
                    break;
                case "given":
                    Given(statement_list, current_row);
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
                    && current_statement.expression.Contains(end_statement.expression) && end_statement.rule == "given")
                {
                    MessageBox.Show("correct and");
                }
            }
            else
            {
               // if ()
            }
        }


        private void Or(List<Statement> statement_list, int current_row)
        {

        }
        private void PBC(List<Statement> statement_list, int current_row)
        {

        }
        private void Not(List<Statement> statement_list, int current_row)
        {

        }
      
        private void MP(List<Statement> statement_list, int current_row)
        {

        }
        private void MT(List<Statement> statement_list, int current_row)
        {

        }
        private void Given(List<Statement> statement_list, int current_row)
        {
            //if()
        }
        private bool IsProvenAlready(string var)
        {
            return false;
        }
    }
}
