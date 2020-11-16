using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Windows;
using System.Windows.Media.Media3D;

namespace LogicCalculator
{
    internal class Evaluation
    {
        public bool Is_Valid { get; set; }
        private readonly List<Statement> statement_list;
        private readonly int current_line;
        private Regex predicate_regex;
        private static int PREDICATE_LENGTH = 4;

        //([a-z]*)+[∧,∨,¬,=,∀,∃][a-z]* \/ ([a-z]*)+[∧,∨,¬,=,∀,∃]([a-z]*)

        public Evaluation(List<Statement> statement_list, string rule)
        {
            Is_Valid = false;
            this.statement_list = statement_list;
            current_line = statement_list.Count - 1;
            predicate_regex = new Regex("[a-z|A-Z]+([a-z])");

            Handle_Rule(rule);
        }

        #region RULES

        private void Handle_Rule(string rule)
        {
            switch (rule)
            {
                case "Proveni":
                    //THIS ON VERIFIED IN MAINWINDOW\
                    Is_Valid = true;
                    break;
                case "Provene":
                    Proven_Elimination();
                    break;
                case "None":
                    None();
                    break;
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
                case "∧i":
                    And_Introduction();
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
                case var ai when new Regex(@"∀.*i").IsMatch(ai):
                    All_Introduction();
                    break;
                case var ae when new Regex(@"∀.*e").IsMatch(ae):
                    All_Elimination();
                    break;
                case var ei when new Regex(@"∃.*i").IsMatch(ei):
                    Exists_Introduction();
                    break;
                case var ee when new Regex(@"∃.*i").IsMatch(ee):
                    Exists_Elimination();
                    break;
            }
        }

        private HashSet<string> getData(string s)
        {
            HashSet<string> ret = new HashSet<string>();
            string[] splited = s.Split('⊢');
            string[] all_data = splited[0].Split(',');
            foreach (var d in all_data)
            {
                ret.Add(replaceAll(d));
            }
            return ret;
        }
        private string getGoal(string s)
        {
            if (!s.Contains('⊢'))
                return null;
            string[] splited = s.Split('⊢');
            return replaceAll(splited[1]);
        }

        private string replaceAll(string s)
        {
            return s.Trim().Replace('^', '∧').Replace('V', '∨').Replace('~', '¬').Replace(" ", "");
        }
        private void Proven_Elimination()
        {
            int proven_index = Get_Row(statement_list[current_line].First_segment);
            HashSet<string> proven_data = getData(statement_list[proven_index].Expression);
            string msg = "Proven elimination first segment must be positive integer\nSecond segment must be positive integers separate by ','";
            List<int> data_indexes = Get_Rows_For_Proven(statement_list[current_line].Second_segment);
            if (data_indexes == null)
            {
                DisplayErrorMsg(msg);
                return;
            }
            HashSet<string> provided_data = new HashSet<string>();
            foreach (int index in data_indexes)
            {
                provided_data.Add(replaceAll(statement_list[index].Expression));
            }
            string proven_goal = getGoal(statement_list[proven_index].Expression);
            string current_goal = replaceAll(statement_list[current_line].Expression);
            bool data_check = compare_sets(proven_data, provided_data);
            
            if (!data_check)
                msg = "Data given is not equivelant to the data needed" +
                    "";
            bool goal_check = !string.IsNullOrEmpty(proven_goal) &&
                !string.IsNullOrEmpty(current_goal) &&
                proven_goal.Equals(current_goal);
            if (!goal_check)
            {
                if (!string.IsNullOrEmpty(msg))
                    msg += "\nAlso, ";
                msg += "Goals are not the same";
            }
            Is_Valid = data_check && goal_check;
            if (!Is_Valid)
                DisplayErrorMsg(msg);
            return;
        }

        private bool compare_sets(HashSet<string> s1, HashSet<string> s2)
        {
            if (s1.Count != s2.Count)
                return false;
            foreach (var d in s1)
            {
                if (!s2.Contains(d))
                    return false;
            }
            return true;
        }

        private void None()
        {
            Is_Valid = statement_list[current_line].Expression.Equals("X0") ||
                statement_list[current_line].Expression.Equals("Y0");
            if (!Is_Valid)
                DisplayErrorMsg("Empty rule must be X0 or Y0");
            return;
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
                right_part = first_expression.Substring(index + 1);
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
                    right_part = second_expression.Substring(index + 1);
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
            right_part = expression.Substring(index + 1);
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
            string current = statement_list[current_line].Expression;

            if (first_row == -1 || second_row == -1)
                return;

            Is_Valid = current.Contains("^") || current.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and introduction");
                return;
            }

            string first = statement_list[first_row].Expression;
            string second = statement_list[second_row].Expression;
            Is_Valid = Equal_With_Operator(current, first, second, "^") ||
                Equal_With_Operator(current, first, second, "∧");
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
                   second_segment_end = statement_list[second_segment_lines[second_segment_lines.Count - 1]].Expression;

            Is_Valid = current_expression == first_segment_end && current_expression == second_segment_end;

            if (!Is_Valid)
            {
                DisplayErrorMsg(current_expression + " Should be equal to " + first_segment_end + " and to " + second_segment_end);
                return;
            }
            Is_Valid = Equal_With_Operator(base_expression, first_segment_start, second_segment_start, "∨")
            || Equal_With_Operator(base_expression, first_segment_start, second_segment_start, "v");

            if (!Is_Valid)
            {
                DisplayErrorMsg(base_expression + " Should be equal to " + first_segment_start + "∨" + second_segment_start);
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

            if (!current_expression.Contains("→"))
            {
                DisplayErrorMsg("Missing → in row");
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

        private void Equal_Introduction()
        {
            string current_expression = statement_list[current_line].Expression;
            int index = current_expression.IndexOf('=');
            if (index == -1)
            {
                DisplayErrorMsg("Missing = in row");
                return;
            }
            Is_Valid = current_expression.Substring(0, index) ==
                current_expression.Substring(index + 1);
            if (!Is_Valid)
                DisplayErrorMsg("Equal introduction format is t1=t1");
        }
        private void Equal_Elimination()
        {
            int index, first_line = Get_Row(statement_list[current_line].First_segment)
            , second_line = Get_Row(statement_list[current_line].Second_segment);


            string current_expression = statement_list[current_line].Expression,
                first_expression = statement_list[first_line].Expression,
                second_expression = statement_list[second_line].Expression,
                first_left, first_right, second_left, second_right, current_left, current_right;

            index = current_expression.IndexOf("=");
            if (index == -1)
            {
                DisplayErrorMsg("Missing = in row");
                return;
            }
            current_left = current_expression.Substring(0, index);
            current_right = current_expression.Substring(index + 1);

            index = first_expression.IndexOf("=");
            if (index == -1)
            {
                DisplayErrorMsg("Missing = in row");
                return;
            }
            first_left = first_expression.Substring(0, index);
            first_right = first_expression.Substring(index + 1);

            index = second_expression.IndexOf("=");
            if (index == -1)
            {
                DisplayErrorMsg("Missing = in row");
                return;
            }
            second_left = second_expression.Substring(0, index);
            second_right = second_expression.Substring(index + 1);



            //if()
            Is_Valid = current_expression ==
                current_expression.Substring(index + 1);
            if (!Is_Valid)
                DisplayErrorMsg("Equal introduction format is t1=t1");
        }

        private void All_Introduction()
        {
            Regex all_regex = new Regex("∀[a-z]+[a-z|A-Z]*([a-z])");
            int index;
            List<int> line_numbers = Get_Lines_From_Segment(statement_list[current_line].First_segment);
            string current_expression = statement_list[current_line].Expression,
                last_expression = statement_list[line_numbers[line_numbers.Count - 1]].Expression;
            char letter = Find_Letter(last_expression);
            current_expression.Replace(current_expression[4], letter);
            Is_Valid = current_expression.Substring(2) == last_expression;
        }
        private void All_Elimination()
        {
            int index, previous_line = Get_Row(statement_list[current_line].First_segment);
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;

            index = statement_list[current_line].Rule[previous_expression.IndexOf("∀")] + 1;
            char letter = statement_list[current_line].Rule[index];
            string all = "∀" + letter;

            index = previous_expression.IndexOf(all) + 2;
            if (index == -1)
            {
                DisplayErrorMsg("Missing " + all + " in row");
                return;
            }

            current_expression.Replace(Find_Letter(current_expression), letter);
            Is_Valid = My_Equal(current_expression, previous_expression);
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of all elimination");
            }
        }
        private void Exists_Introduction()
        {
            int previous_line = Get_Row(statement_list[current_line].First_segment);
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;
            char letter = Find_Letter(previous_expression);
            current_expression.Replace(current_expression[4], letter);
            Is_Valid = current_expression == previous_expression;
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of exist introduction");
            }
        }
        private void Exists_Elimination() {
            List<int> second_seg_rows= Get_Lines_From_Segment(statement_list[current_line].Second_segment);
            int previous_line=second_seg_rows[second_seg_rows.Count];
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;
            Is_Valid = current_expression == previous_expression;
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of exist introduction");
            }
        }

        #endregion

        #region UTILITY

        private char Find_Letter(string to_search)
        {
            char letter = '%';
            for (int i = 0; i < to_search.Length - PREDICATE_LENGTH; i++)
            {
                if (predicate_regex.IsMatch(to_search.Substring(i, PREDICATE_LENGTH)))
                {
                    letter = to_search[i + 2];
                    break;
                }
            }
            return letter;
        }


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

        static public List<int> Get_Lines_From_Segment(string seg)
        {
            List<int> ret = new List<int>();
            int index = seg.IndexOf("-");
            if (index != -1)
            {
                int starting_number = Int32.Parse(seg.Substring(0, index)),
                    last_number = Int32.Parse(seg.Substring(index + 1));
                ret.AddRange(Enumerable.Range(starting_number, last_number - starting_number + 1));
            }
            else
                ret.Add(Int32.Parse(seg));

            return ret;
        }

        private List<int> Get_Rows_For_Proven(string seg)
        {
            List<int> ret = new List<int>();
            
            string[] indexes = seg.Split(',');
            foreach (var s in indexes)
            {
                if (!Int32.TryParse(s, out int o))
                    return null;
                ret.Add(o);
            }
            return ret;
        }

        private bool Check_If_Not(string first, string second)
        {
            return first == "~" + second || first == "¬" + second || second == "~" + first || second == "¬" + first;
        }

        private void DisplayErrorMsg(string msg)
        {
            Is_Valid = false;
            MessageBox.Show("Error at row " + current_line + "\n" + msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        private bool My_Equal(string first, string second)
        {
            return first == second || '(' + first + ')' == second || first == '(' + second + ')' || '(' + first + ')' == '(' + second + ')';
        }

        private bool Equal_With_Operator(string expression, string first, string second, string op)
        {
            return expression == first + op + second || expression == '(' + first + ')' + op + second || expression == first + op + '(' + second + ')' || expression == '(' + first + ')' + op + '(' + second + ')' ||
                expression == second + op + first || expression == '(' + second + ')' + op + first || expression == second + op + '(' + first + ')' || expression == '(' + second + ')' + op + '(' + first + ')';
        }
        #endregion UTILITY
    }
}