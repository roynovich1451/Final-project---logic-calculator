using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LogicCalculator
{
    internal class Evaluation
    {
        public bool is_valid { get; set; }
        private readonly List<Statement> statement_list;
        private readonly int current_line;

        public Evaluation(List<Statement> statement_list, string rule)
        {
            this.statement_list = statement_list;
            current_line = statement_list.Count - 1;
            is_valid = false;
            switch (rule)
            {
                case "Data":
                    Data();
                    break;

                case "Assumption":
                    is_valid = true;
                    return;

                case "MP":
                    MP();
                    break;

                case "Copy":
                    Copy();
                    break;

                case "MT":
                    MT();
                    break;

                case "PBC":
                    PBC();
                    break;

                case "LEM":
                    LEM();
                    break;

                case "∧e1":
                    And_Elimination_One();
                    break;

                case "∧e2":
                    And_Elimination_Two();
                    break;

                case "∨i1":
                    Or_Introduction_One();
                    break;

                case "∨i2":
                    Or_Introduction_Two();
                    break;

                case "∨e":
                    Or_Elimination();
                    break;

                case "∧i":
                    And_Introduction();
                    break;

                case "¬i":
                    Not_Introduction();
                    break;

                case "¬e":
                    Not_Elimination();
                    break;

                case "⊥e":
                    Contradiction_Elimination();
                    break;

                case "¬¬e":
                    Not_Not_Elimination();
                    break;

                case "¬¬i":
                    Not_Not_Introduction();
                    break;

                case "→i":
                    Arrow_Introduction();
                    break;
            }
        }

        private void Data()
        {
            int index = statement_list[0].expression.IndexOf("⊢");
            if (index != -1)
                statement_list[0].expression = statement_list[0].expression.Substring(0, index);
            foreach (string s in statement_list[0].expression.Split(','))
            {
                is_valid = (s == statement_list[current_line].expression);
                if (is_valid)
                    return;
            }
            DisplayErrorMsg("Data doesn't exist in the original expression");
        }

        private void Copy()
        {
            //TODO:add box checks
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            is_valid = statement_list[row].expression == statement_list[current_line].expression;
            if (!is_valid)
                DisplayErrorMsg("Values should be equal");
        }

        private void MP()
        {
            int first_row = Get_Row(statement_list[current_line].first_segment),
            second_row = Get_Row(statement_list[current_line].second_segment);
            if (first_row == -1 || second_row == -1)
                return;
            string first_expression = statement_list[first_row].expression,
                  second_expression = statement_list[second_row].expression,
                  current_expression = statement_list[current_line].expression;
            is_valid = (first_expression == second_expression + "→" + current_expression)
                || (first_expression == second_expression + "→(" + current_expression + ")")
                || (second_expression == first_expression + "→" + current_expression)
                || (second_expression == first_expression + "→(" + current_expression + ")");
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of MP");
            }
        }

        private void MT()
        {
            int first_row = Get_Row(statement_list[current_line].first_segment),
            second_row = Get_Row(statement_list[current_line].second_segment), index;
            if (first_row == -1 || second_row == -1)
                return;
            string left_part, right_part,
                first_expression = statement_list[first_row].expression,
                second_expression = statement_list[second_row].expression,
                current_expression = statement_list[current_line].expression;
            index = first_expression.IndexOf("→");

            //if the first expression contains ->
            if (index != -1)
            {
                left_part = first_expression.Substring(0, index);
                right_part = first_expression.Substring(index + 1, first_expression.Length - (index + 1));
                if (second_expression != "~" + right_part && second_expression != "¬" + right_part)
                {
                    DisplayErrorMsg("MT missing ¬");
                    is_valid = false;
                    return;
                }
            }
            else //check if the second expression contains ->
            {
                index = second_expression.IndexOf("→");
                if (index != -1)
                {
                    left_part = second_expression.Substring(0, index);
                    right_part = second_expression.Substring(index + 1, second_expression.Length - (index + 1));
                    if (first_expression != "~" + right_part && first_expression != "¬" + right_part)
                    {
                        DisplayErrorMsg("MT missing ¬");
                        is_valid = false;
                        return;
                    }
                }
                else
                {
                    DisplayErrorMsg("MT was called without →");
                    is_valid = false;
                    return;
                }
            }

            is_valid = current_expression == "~" + left_part || current_expression == "¬" + left_part;

            if (!is_valid)
                DisplayErrorMsg("Misuse of MT");
        }

        private void PBC()
        {
            List<int> rows = Get_Lines_From_Segment(statement_list[current_line].first_segment);
            is_valid = statement_list[rows[rows.Count - 1]].expression == "⊥";
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ⊥ at the previous line");
                return;
            }
            is_valid &= Check_If_Not(statement_list[current_line].expression, statement_list[rows[0]].expression);
            if (!is_valid)
                DisplayErrorMsg("Misuse of PBC");
        }

        private void LEM()
        {
            string left_part, right_part, expression = statement_list[current_line].expression;
            int index = expression.IndexOf("V");
            if (index == -1)
                index = expression.IndexOf("∨");
            if (index == -1)
            {
                DisplayErrorMsg("LEM without V or ∨");
                return;
            }
            left_part = expression.Substring(0, index);
            right_part = expression.Substring(index + 1, expression.Length - (index + 1));
            is_valid = Check_If_Not(left_part, right_part);
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of LEM");
            }
        }

        private void And_Introduction()
        {
            int first_row = Get_Row(statement_list[current_line].first_segment),
            second_row = Get_Row(statement_list[current_line].second_segment);
            if (first_row == -1 || second_row == -1)
                return;
            is_valid = statement_list[current_line].expression.Contains("^")
                || statement_list[current_line].expression.Contains("∧");
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ∧ in and introduction");
                return;
            }
            string first = statement_list[first_row].expression;
            string second = statement_list[second_row].expression;
            is_valid = statement_list[current_line].expression == first + "^" + second ||
                statement_list[current_line].expression == first + "∧" + second ||
                statement_list[current_line].expression == second + "^" + first ||
                statement_list[current_line].expression == second + "∧" + first;
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of And Introduction");
            }
        }

        private void And_Elimination_One()
        {
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            string original_expression = statement_list[row].expression,
                current_expression = statement_list[current_line].expression;
            is_valid = original_expression.Contains("^") || original_expression.Contains("∧");
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ∧ in and elimination");
                return;
            }
            is_valid = original_expression.Contains(current_expression + "^")
             || original_expression.Contains(current_expression + "∧")
             || original_expression.Contains("(" + current_expression + ")^")
             || original_expression.Contains("(" + current_expression + ")∧");
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of And Elimination 1");
            }
        }

        private void And_Elimination_Two()
        {
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            string original_expression = statement_list[row].expression,
                current_expression = statement_list[current_line].expression;

            is_valid = original_expression.Contains("^") || original_expression.Contains("∧");
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ∧ in and elimination");
                return;
            }
            is_valid = original_expression.Contains("^" + current_expression) ||
            original_expression.Contains("∧" + current_expression) ||
            original_expression.Contains("^(" + current_expression + ")") ||
            original_expression.Contains("∧(" + current_expression + ")");

            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of And Elimination 2");
            }
        }

        private void Or_Elimination()
        {
            List<int> first_segment_lines = Get_Lines_From_Segment(statement_list[current_line].second_segment),
                      second_segment_lines = Get_Lines_From_Segment(statement_list[current_line].third_segment);
            int base_row = Get_Row(statement_list[current_line].first_segment);
            string current_expression = statement_list[current_line].expression,
                   base_expression = statement_list[base_row].expression,
                   first_segment_start = statement_list[first_segment_lines[0]].expression,
                   second_segment_start = statement_list[second_segment_lines[0]].expression,
                   first_segment_end = statement_list[first_segment_lines[first_segment_lines.Count - 1]].expression,
                   second_segment_end = statement_list[second_segment_lines[second_segment_lines.Count - 1]].expression                   ;

            //Check the base row

            is_valid = (base_expression == first_segment_start + second_segment_start ||
                base_expression == "(" + first_segment_start + ")" + second_segment_start ||
               base_expression == first_segment_start + "(" + second_segment_start + ")" ||
              base_expression == "(" + first_segment_start + ")" + "(" + second_segment_start + ")");
            is_valid = first_segment_end == second_segment_end
                    && second_segment_end == current_expression;
            if (!is_valid)
            {
                DisplayErrorMsg(current_expression + " Should be equal to "+ second_segment_end+" and to "+ first_segment_end);
                return;
            }
        }

        private void Or_Introduction_One()
        {
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            string current_expression = statement_list[current_line].expression;
            is_valid = current_expression.Contains("V") || current_expression.Contains("∨");
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ∨ in or introduction");
                return;
            }
            is_valid = current_expression.Contains(statement_list[row].expression + "∨")
                || current_expression.Contains(statement_list[row].expression + "V")
                || current_expression.Contains("(" + statement_list[row].expression + ")∨")
                || current_expression.Contains("(" + statement_list[row].expression + ")V");
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of or1 introduction");
            }
        }

        private void Or_Introduction_Two()
        {
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            string current_statement = statement_list[current_line].expression;
            is_valid = current_statement.Contains("V") || current_statement.Contains("∨");
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ∨ in or introduction");
                return;
            }

            is_valid = current_statement.Contains("∨" + statement_list[row].expression)
                || current_statement.Contains("V" + statement_list[row].expression)
                || current_statement.Contains("∨(" + statement_list[row].expression + ")")
                || current_statement.Contains("V(" + statement_list[row].expression + ")");
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of or2 introduction");
            }
        }

        private void Not_Introduction()
        {//TODO check more
            is_valid = statement_list[current_line - 1].expression == "⊥";
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ⊥ at the previous row");
                return;
            }

            is_valid &= Check_If_Not(statement_list[current_line - 2].expression, statement_list[current_line - 3].expression);
            if (!is_valid)
                DisplayErrorMsg("Missuse of Not Introduction");
        }

        private void Not_Elimination()
        {
            is_valid = statement_list[current_line].expression == "⊥";
            if (!is_valid)
            {
                DisplayErrorMsg("Missing ⊥ at the current row");
                return;
            }
            int first_row = Get_Row(statement_list[current_line].first_segment),
                second_row = Get_Row(statement_list[current_line].second_segment);

            is_valid &= Check_If_Not(statement_list[first_row].expression, statement_list[second_row].expression);
            if (!is_valid)
                DisplayErrorMsg("Missuse of Not Elimination");
        }

        private void Contradiction_Elimination()
        {
            is_valid = statement_list[current_line - 1].expression == "⊥";
            if (!is_valid)
                DisplayErrorMsg("Missing ⊥ at the previous row");
        }

        private void Not_Not_Elimination()
        {
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            string original_expression = statement_list[row].expression,
                current_expression = statement_list[current_line].expression;
            is_valid = original_expression == "¬¬" + current_expression
                || original_expression == "¬¬(" + current_expression + ")"
                || original_expression == "~~" + current_expression
                || original_expression == "~~(" + current_expression + ")";
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of Not Not Elimination");
            }
        }

        private void Not_Not_Introduction()
        {
            int row = Get_Row(statement_list[current_line].first_segment);
            if (row == -1)
                return;
            is_valid = statement_list[current_line].expression == "¬¬" + statement_list[row].expression
                || statement_list[current_line].expression == "~~" + statement_list[row].expression;
            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of Not Not Introduction");
            }
        }

        private void Arrow_Introduction()
        {
            List<int> lines = Get_Lines_From_Segment(statement_list[current_line].first_segment);
            int start_row = lines[0],
            end_row = lines[lines.Count - 1];
            string current_expression = statement_list[current_line].expression;
            //TODO: add box check
            if (!current_expression.Contains("→"))
            {
                DisplayErrorMsg("Missing → in row");
                is_valid = false;
                return;
            }

            is_valid = statement_list[start_row].rule == "Assumption" &&
                    current_expression == statement_list[start_row].expression + "→" + statement_list[end_row].expression
                    || current_expression == "(" + statement_list[start_row].expression + ")→" + statement_list[end_row].expression
                    || current_expression == statement_list[start_row].expression + "→(" + statement_list[end_row].expression + ")"
                    || current_expression == "(" + statement_list[start_row].expression + ")→(" + statement_list[end_row].expression + ")";

            if (!is_valid)
            {
                DisplayErrorMsg("Misuse of Arrow Introduction");
            }
        }

        #region UTILITY

        private int Get_Row(string s)
        {
            if (s.Contains("-"))
            {
                DisplayErrorMsg("Segment contains '-' when it should not");
                return -1;
            }
            int ret = Int32.Parse(s);
            if (ret < 1)
            {
                DisplayErrorMsg("Line number must be bigger than 0");
                return -1;
            }
            if (ret > statement_list.Count - 1)
            {
                DisplayErrorMsg("Line number can't be bigger than current line number");
            }
            if (ret == current_line)
            {
                DisplayErrorMsg("Line number provided is equal to current line");
                return -1;
            }
            return ret;
        }

        private List<int> Get_Lines_From_Segment(string seg)
        {
            List<int> ret = new List<int>();
            int index = seg.IndexOf("-");
            if (index != -1)
            {
                int starting_number = Int32.Parse(seg.Substring(0, index)),
                    last_number = Int32.Parse(seg.Substring(index + 1, seg.Length - (index + 1)));
                ret.AddRange(Enumerable.Range(starting_number, last_number - starting_number + 1));
            }
            else
                ret.Add(Int32.Parse(seg));

            return ret;
        }

        private bool Check_If_Not(string first, string second)
        {
            return first == "~" + second || first == "¬" + second || second == "~" + first || second == "¬" + first;
        }

        private void DisplayErrorMsg(string msg)
        {
            MessageBox.Show("Error at row " + current_line + "\n" + msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        #endregion UTILITY
    }
}