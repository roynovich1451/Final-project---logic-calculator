using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LogicCalculator
{
    internal class Evaluation
    {
        public bool Is_Valid { get; set; }
        private readonly List<Statement> statement_list;
        private readonly int current_line;

        public Evaluation(List<Statement> statement_list, string rule, List<Tuple<int, string>> box_pairs_list)
        {
          
            this.statement_list = statement_list;
            current_line = statement_list.Count - 1;
            Is_Valid = false;
            switch (rule)
            {
                case "Data":
                    Data();
                    break;

                case "Assumption":
                    Is_Valid = true;
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
                case "=i":
                    Equal_Introduction();
                    break;
                case "=e":
                    Equal_Elimination();
                    break;
                case "∀i":
                    All_Introduction();
                    break;
                case "∀e":
                    All_Elimination();
                    break;
                case "∃i":
                    Exists_Introduction();
                    break;
                case "∃e":
                    Exists_Elimination();
                    break;
            }
        }

        private void Data()
        {
            int index = statement_list[0].Expression.IndexOf("⊢");
            if (index != -1)
                statement_list[0].Expression = statement_list[0].Expression.Substring(0, index);
            foreach (string s in statement_list[0].Expression.Split(','))
            {
                Is_Valid = (s == statement_list[current_line].Expression);
                if (Is_Valid)
                    return;
            }
            DisplayErrorMsg("Data doesn't exist in the original expression");
        }

        private void Copy()
        {
            //TODO:add box checks
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            Is_Valid = statement_list[row].Expression == statement_list[current_line].Expression;
            if (!Is_Valid)
                DisplayErrorMsg("Values should be equal");
        }

        private void MP()
        {
            int first_row = Get_Row(statement_list[current_line].First_segment),
            second_row = Get_Row(statement_list[current_line].Second_segment);
            if (first_row == -1 || second_row == -1)
                return;
            string first_expression = statement_list[first_row].Expression,
                  second_expression = statement_list[second_row].Expression,
                  current_expression = statement_list[current_line].Expression;
            Is_Valid = (first_expression == second_expression + "→" + current_expression)
                || (first_expression == second_expression + "→(" + current_expression + ")")
                || (second_expression == first_expression + "→" + current_expression)
                || (second_expression == first_expression + "→(" + current_expression + ")");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of MP");
            }
        }

        private void MT()
        {
            int first_row = Get_Row(statement_list[current_line].First_segment),
            second_row = Get_Row(statement_list[current_line].Second_segment), index;
            if (first_row == -1 || second_row == -1)
                return;
            string left_part, right_part,
                first_expression = statement_list[first_row].Expression,
                second_expression = statement_list[second_row].Expression,
                current_expression = statement_list[current_line].Expression;
            index = first_expression.IndexOf("→");

            //if the first expression contains ->
            if (index != -1)
            {
                left_part = first_expression.Substring(0, index);
                right_part = first_expression.Substring(index + 1, first_expression.Length - (index + 1));
                if (second_expression != "~" + right_part && second_expression != "¬" + right_part)
                {
                    DisplayErrorMsg("MT missing ¬");
                    Is_Valid = false;
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
                        Is_Valid = false;
                        return;
                    }
                }
                else
                {
                    DisplayErrorMsg("MT was called without →");
                    Is_Valid = false;
                    return;
                }
            }

            Is_Valid = current_expression == "~" + left_part || current_expression == "¬" + left_part;

            if (!Is_Valid)
                DisplayErrorMsg("Misuse of MT");
        }

        private void PBC()
        {
            List<int> rows = Get_Lines_From_Segment(statement_list[current_line].First_segment);
            Is_Valid = statement_list[rows[rows.Count - 1]].Expression == "⊥";
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ⊥ at the previous line");
                return;
            }
            Is_Valid &= Check_If_Not(statement_list[current_line].Expression, statement_list[rows[0]].Expression);
            if (!Is_Valid)
                DisplayErrorMsg("Misuse of PBC");
        }

        private void LEM()
        {
            string left_part, right_part, expression = statement_list[current_line].Expression;
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
            Is_Valid = Check_If_Not(left_part, right_part);
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of LEM");
            }
        }

        private void And_Introduction()
        {
            int first_row = Get_Row(statement_list[current_line].First_segment),
            second_row = Get_Row(statement_list[current_line].Second_segment);
            if (first_row == -1 || second_row == -1)
                return;
            Is_Valid = statement_list[current_line].Expression.Contains("^")
                || statement_list[current_line].Expression.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and introduction");
                return;
            }
            string first = statement_list[first_row].Expression;
            string second = statement_list[second_row].Expression;
            Is_Valid = statement_list[current_line].Expression == first + "^" + second ||
                statement_list[current_line].Expression == first + "∧" + second ||
                statement_list[current_line].Expression == second + "^" + first ||
                statement_list[current_line].Expression == second + "∧" + first;
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of And Introduction");
            }
        }

        private void And_Elimination_One()
        {
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            string original_expression = statement_list[row].Expression,
                current_expression = statement_list[current_line].Expression;
            Is_Valid = original_expression.Contains("^") || original_expression.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and elimination");
                return;
            }
            Is_Valid = original_expression.Contains(current_expression + "^")
             || original_expression.Contains(current_expression + "∧")
             || original_expression.Contains("(" + current_expression + ")^")
             || original_expression.Contains("(" + current_expression + ")∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of And Elimination 1");
            }
        }

        private void And_Elimination_Two()
        {
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            string original_expression = statement_list[row].Expression,
                current_expression = statement_list[current_line].Expression;

            Is_Valid = original_expression.Contains("^") || original_expression.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and elimination");
                return;
            }
            Is_Valid = original_expression.Contains("^" + current_expression) ||
            original_expression.Contains("∧" + current_expression) ||
            original_expression.Contains("^(" + current_expression + ")") ||
            original_expression.Contains("∧(" + current_expression + ")");

            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of And Elimination 2");
            }
        }

        private void Or_Elimination()
        {
            List<int> first_segment_lines = Get_Lines_From_Segment(statement_list[current_line].Second_segment),
                      second_segment_lines = Get_Lines_From_Segment(statement_list[current_line].Third_segment);
            int base_row = Get_Row(statement_list[current_line].First_segment);
            string current_expression = statement_list[current_line].Expression,
                   base_expression = statement_list[base_row].Expression,
                   first_segment_start = statement_list[first_segment_lines[0]].Expression,
                   second_segment_start = statement_list[second_segment_lines[0]].Expression,
                   first_segment_end = statement_list[first_segment_lines[first_segment_lines.Count - 1]].Expression,
                   second_segment_end = statement_list[second_segment_lines[second_segment_lines.Count - 1]].Expression                   ;

            Is_Valid = (base_expression == first_segment_start + second_segment_start ||
                base_expression == "(" + first_segment_start + ")" + second_segment_start ||
               base_expression == first_segment_start + "(" + second_segment_start + ")" ||
              base_expression == "(" + first_segment_start + ")" + "(" + second_segment_start + ")");
            Is_Valid = first_segment_end == second_segment_end
                    && second_segment_end == current_expression;
            if (!Is_Valid)
            {
                DisplayErrorMsg(current_expression + " Should be equal to "+ second_segment_end+" and to "+ first_segment_end);
                return;
            }
        }

        private void Or_Introduction_One()
        {
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            string current_expression = statement_list[current_line].Expression;
            Is_Valid = current_expression.Contains("V") || current_expression.Contains("∨");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∨ in or introduction");
                return;
            }
            Is_Valid = current_expression.Contains(statement_list[row].Expression + "∨")
                || current_expression.Contains(statement_list[row].Expression + "V")
                || current_expression.Contains("(" + statement_list[row].Expression + ")∨")
                || current_expression.Contains("(" + statement_list[row].Expression + ")V");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of or1 introduction");
            }
        }

        private void Or_Introduction_Two()
        {
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            string current_statement = statement_list[current_line].Expression;
            Is_Valid = current_statement.Contains("V") || current_statement.Contains("∨");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∨ in or introduction");
                return;
            }

            Is_Valid = current_statement.Contains("∨" + statement_list[row].Expression)
                || current_statement.Contains("V" + statement_list[row].Expression)
                || current_statement.Contains("∨(" + statement_list[row].Expression + ")")
                || current_statement.Contains("V(" + statement_list[row].Expression + ")");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of or2 introduction");
            }
        }

        private void Not_Introduction()
        {
            Is_Valid = statement_list[current_line - 1].Expression == "⊥";
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ⊥ at the previous row");
                return;
            }

            //TODO check
            Is_Valid &= Check_If_Not(statement_list[current_line - 2].Expression, statement_list[current_line - 3].Expression);
            if (!Is_Valid)
                DisplayErrorMsg("Missuse of Not Introduction");
        }

        private void Not_Elimination()
        {
            Is_Valid = statement_list[current_line].Expression == "⊥";
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ⊥ at the current row");
                return;
            }
            int first_row = Get_Row(statement_list[current_line].First_segment),
                second_row = Get_Row(statement_list[current_line].Second_segment);

            Is_Valid &= Check_If_Not(statement_list[first_row].Expression, statement_list[second_row].Expression);
            if (!Is_Valid)
                DisplayErrorMsg("Missuse of Not Elimination");
        }

        private void Contradiction_Elimination()
        {
            Is_Valid = statement_list[current_line - 1].Expression == "⊥";
            if (!Is_Valid)
                DisplayErrorMsg("Missing ⊥ at the previous row");
        }

        private void Not_Not_Elimination()
        {
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            string original_expression = statement_list[row].Expression,
                current_expression = statement_list[current_line].Expression;
            Is_Valid = original_expression == "¬¬" + current_expression
                || original_expression == "¬¬(" + current_expression + ")"
                || original_expression == "~~" + current_expression
                || original_expression == "~~(" + current_expression + ")";
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of Not Not Elimination");
            }
        }

        private void Not_Not_Introduction()
        {
            int row = Get_Row(statement_list[current_line].First_segment);
            if (row == -1)
                return;
            Is_Valid = statement_list[current_line].Expression == "¬¬" + statement_list[row].Expression
                || statement_list[current_line].Expression == "~~" + statement_list[row].Expression;
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of Not Not Introduction");
            }
        }

        private void Arrow_Introduction()
        {
            List<int> lines = Get_Lines_From_Segment(statement_list[current_line].First_segment);
            int start_row = lines[0],
            end_row = lines[lines.Count - 1];
            string current_expression = statement_list[current_line].Expression;
            //TODO: add box check
            if (!current_expression.Contains("→"))
            {
                DisplayErrorMsg("Missing → in row");
                Is_Valid = false;
                return;
            }

            Is_Valid = statement_list[start_row].Rule == "Assumption" &&
                    current_expression == statement_list[start_row].Expression + "→" + statement_list[end_row].Expression
                    || current_expression == "(" + statement_list[start_row].Expression + ")→" + statement_list[end_row].Expression
                    || current_expression == statement_list[start_row].Expression + "→(" + statement_list[end_row].Expression + ")"
                    || current_expression == "(" + statement_list[start_row].Expression + ")→(" + statement_list[end_row].Expression + ")";

            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of Arrow Introduction");
            }
        }

        private void Equal_Introduction()        {
            string current_expression = statement_list[current_line].Expression;
            int index = current_expression.IndexOf('=');
            if (index == -1)
            {
                DisplayErrorMsg("Missing = in row");
                Is_Valid = false;
                return;
            }
            Is_Valid = current_expression.Substring(0, index) ==
                current_expression.Substring(index + 1, current_expression.Length - (index + 1));
            if(!Is_Valid)
                DisplayErrorMsg("Equal introduction format is t1=t1");
        }
        private void Equal_Elimination()        {
            int index, first_line = Get_Row(statement_list[current_line].First_segment)
            ,second_line = Get_Row(statement_list[current_line].Second_segment);
            

            string current_expression = statement_list[current_line].Expression,
                first_left,first_right,second_left,second_right;            
            index = current_expression.IndexOf('=');
            if (index == -1)
            {
                DisplayErrorMsg("Missing = in row");
                Is_Valid = false;
                return;
            }
            Is_Valid = current_expression ==
                current_expression.Substring(index + 1, current_expression.Length - (index + 1));
            if (!Is_Valid)
                DisplayErrorMsg("Equal introduction format is t1=t1");
        }
        private void All_Introduction() { throw new NotImplementedException(); }
        private void All_Elimination() { throw new NotImplementedException(); }
        private void Exists_Introduction() { throw new NotImplementedException(); }
        private void Exists_Elimination() { throw new NotImplementedException(); }


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