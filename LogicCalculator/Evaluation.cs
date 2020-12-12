using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace LogicCalculator
{
    internal class Evaluation
    {
        public bool Is_Valid { get; set; }
        private readonly List<Statement> statement_list;
        private readonly int current_line;
        private static readonly int PREDICATE_LENGTH = 4;

        //([a-z]*)+[∧,∨,¬,=,∀,∃][a-z]* \/ ([a-z]*)+[∧,∨,¬,=,∀,∃]([a-z]*)

        public Evaluation(List<Statement> statement_list, string rule)
        {
            Is_Valid = false;
            this.statement_list = statement_list;
            current_line = statement_list.Count - 1;
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

        private HashSet<string> GetData(string s)
        {
            HashSet<string> ret = new HashSet<string>();
            string[] splited = s.Split('⊢');
            string[] all_data = splited[0].Split(',');
            foreach (var d in all_data)
            {
                ret.Add(ReplaceAll(d));
            }
            return ret;
        }
        private string GetGoal(string s)
        {
            if (!s.Contains('⊢'))
                return null;
            string[] splited = s.Split('⊢');
            return ReplaceAll(splited[1]);
        }

        private string ReplaceAll(string s)
        {
            return s.Trim().Replace('^', '∧').Replace('V', '∨').Replace('~', '¬').Replace(" ", "");
        }
        private void Proven_Elimination()
        {
            int proven_index = Get_Row(statement_list[current_line].First_segment);
            if (statement_list[proven_index].Rule != "Proveni")
            {
                DisplayErrorMsg("Proven elimination first segment must point to 'Proven i' rule line");
                return;
            }
            HashSet<string> proven_data = GetData(statement_list[proven_index].Expression);
            List<int> data_indexes = Get_Rows_For_Proven(statement_list[current_line].Second_segment);
            if (data_indexes == null)
            {
                DisplayErrorMsg("Proven elimination first segment must be positive integer\nSecond segment must be positive integers separate by ','");
                return;
            }
            HashSet<string> provided_data = new HashSet<string>();
            foreach (int index in data_indexes)
            {
                provided_data.Add(ReplaceAll(statement_list[index].Expression));
            }
            string proven_goal = GetGoal(statement_list[proven_index].Expression);
            string current_goal = ReplaceAll(statement_list[current_line].Expression);
            Is_Valid = Compare_Sets(proven_data, provided_data);

            if (!Is_Valid)
            {
                DisplayErrorMsg("Data given is not equivelant to the data needed");
                return;
            }
            Is_Valid = !string.IsNullOrEmpty(proven_goal) &&
                !string.IsNullOrEmpty(current_goal) &&
                proven_goal.Equals(current_goal);
            if (!Is_Valid)
            {
                DisplayErrorMsg($"Expression in current line must be '{proven_goal}'");
                return;
            }
            return;
        }

        private bool Compare_Sets(HashSet<string> s1, HashSet<string> s2)
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
            string current_expression = statement_list[current_line].Expression;
            foreach (string s in statement_list[0].Expression.Split(','))
            {
                Is_Valid = (s == current_expression);
                if (Is_Valid)
                    return;
            }
            DisplayErrorMsg("Data: "+current_expression+" doesn't exist in the original expression");
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
                || (second_expression == first_expression + "→(" + current_expression + ")")
                || Equal_With_Operator(first_expression,second_expression,current_expression, "→")
                || Equal_With_Operator(second_expression, first_expression, current_expression, "→");
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
                if (!Check_If_Not(right_part,second_expression))
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
                    if (!Check_If_Not(right_part,first_expression))
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

            Is_Valid =  current_expression == "¬" + left_part;

            if (!Is_Valid)
                DisplayErrorMsg("Misuse of MT");
        }

        private void PBC()
        {
            List<int> rows = Get_Lines_From_Segment(statement_list[current_line].First_segment);
            if (rows == null)
            {
                Is_Valid = false;
                return;
            }
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
            int index = expression.IndexOf("∨");
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

            Is_Valid = current.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and introduction");
                return;
            }

            string first = statement_list[first_row].Expression;
            string second = statement_list[second_row].Expression;
            Is_Valid =                Equal_With_Operator(current, first, second, "∧");
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
            Is_Valid = original_expression.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and elimination");
                return;
            }
            Is_Valid =  original_expression.Contains(current_expression + "∧")
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

            Is_Valid =  original_expression.Contains("∧");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∧ in and elimination");
                return;
            }
            Is_Valid =             original_expression.Contains("∧" + current_expression) ||
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
            if (first_segment_lines == null|| second_segment_lines==null)
            {
                Is_Valid = false;
                return;
            }
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
            Is_Valid = Equal_With_Operator(base_expression, first_segment_start, second_segment_start, "∨");
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
            Is_Valid = current_expression.Contains("∨");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∨ in or introduction");
                return;
            }
            Is_Valid = current_expression.Contains(statement_list[row].Expression + "∨")
                || current_expression.Contains("(" + statement_list[row].Expression + ")∨");
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
            Is_Valid = current_statement.Contains("∨");
            if (!Is_Valid)
            {
                DisplayErrorMsg("Missing ∨ in or introduction");
                return;
            }

            Is_Valid = current_statement.Contains("∨" + statement_list[row].Expression)
                || current_statement.Contains("∨(" + statement_list[row].Expression + ")"); 
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
            int first_line = Get_First_Line_From_Segment(statement_list[current_line].First_segment);           
            Is_Valid=
            Is_Valid &= Check_If_Not(statement_list[first_line].Expression, statement_list[current_line].Expression);
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
                || original_expression == "¬¬(" + current_expression + ")";
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
            Is_Valid = statement_list[current_line].Expression == "¬¬" + statement_list[row].Expression;
            if (!Is_Valid)
            {
                DisplayErrorMsg("Misuse of Not Not Introduction");
            }
        }

        private void Arrow_Introduction()
        {
            List<int> rows = Get_Lines_From_Segment(statement_list[current_line].First_segment);
            if (rows == null)
            {
                Is_Valid = false;
                return;
            }
            int start_row = rows[0],
            end_row = rows[rows.Count - 1];
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
            
            //first case
            if (first_left == current_left)
            {
                if (first_right == second_left)
                {
                    Is_Valid = second_right == current_right;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else if (first_right == second_right)
                {
                    Is_Valid = second_left == current_right;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else
                {
                    Is_Valid = false;
                    DisplayErrorMsg("Misuse of equal elimination");
                    return;
                }
            }
            //second case
            else if (first_left == current_right)
            {
                if (first_right == second_left)
                {
                    Is_Valid = second_right == current_left;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else if (first_right == second_right)
                {
                    Is_Valid = second_left == current_left;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else
                {
                    Is_Valid = false;
                    DisplayErrorMsg("Misuse of equal elimination");
                    return;
                }
            }

            //third case
            else if (first_right == current_left) {
                if (first_left == second_left)
                {
                    Is_Valid = second_right == current_right;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else if (first_left == second_right)
                {
                    Is_Valid = second_left == current_right;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else
                {
                    Is_Valid = false;
                    DisplayErrorMsg("Misuse of equal elimination");
                    return;
                }
            }
            //fourth case
            else if (first_right == current_right) {
                if (first_left == second_left) {
                    Is_Valid = second_right == current_left;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else if (first_left == second_right) {
                    Is_Valid = second_left == current_left;
                    if (!Is_Valid)
                    {
                        DisplayErrorMsg("Misuse of equal elimination");
                        return;
                    }
                }
                else
                {
                    Is_Valid = false;
                    DisplayErrorMsg("Misuse of equal elimination");
                    return;
                }
            }
            else {
                Is_Valid = false;
                DisplayErrorMsg("Misuse of equal elimination");
            }           
        }

        private void All_Introduction()
        {
            List<int> rows = Get_Lines_From_Segment(statement_list[current_line].First_segment);
            if (rows == null)
            {
                Is_Valid = false;
                return;
            }

            string current_expression = statement_list[current_line].Expression,
                last_expression = statement_list[rows[rows.Count - 1]].Expression;
            char letter = Find_Letter(last_expression);
            current_expression.Replace(current_expression[4], letter);
            Is_Valid = current_expression.Substring(2) == last_expression;
        }
        private void All_Elimination()
        {
            int previous_line = Get_Row(statement_list[current_line].First_segment);
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;

            char original_letter= statement_list[current_line].Rule[1],
            current_letter =Find_Letter(statement_list[current_line].Expression);
    
            if (!previous_expression.Contains("∀"))
            {
                DisplayErrorMsg("Missing ∀ in previous row");
                return;
            }           

            string to_check= "∀"+original_letter+"("+current_expression.Replace(Find_Letter(current_expression), original_letter)+")";
            Is_Valid = previous_expression.Contains(to_check);
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
            if (second_seg_rows==null)
            {
                Is_Valid = false;
                return;
            }
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
                if (Char.IsUpper(to_search[i]))
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

        public int Get_First_Line_From_Segment(string seg)
        {
            int index = seg.IndexOf("-");
            if (index == -1)
            {
                return -1;
            }   
            return Int32.Parse(seg.Substring(0, index));
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
            return first == "¬" + second || second == "¬" + first
            || first == "¬(" + second+")" ||  second == "¬(" + first+")";
        }

        private void DisplayErrorMsg(string msg)
        {
            Is_Valid = false;
            MessageBox.Show("Error at row " + current_line + "\n" + msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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