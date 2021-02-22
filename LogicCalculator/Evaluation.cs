using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicCalculator
{
    internal class Evaluation
    {
        public bool Is_Valid { get; set; }
        private readonly List<Statement> statement_list;
        private readonly int current_line;
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
                    //THIS ON VERIFIED IN MAINWINDOW
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
                case "∀xi":
                    All_Introduction("∀x");
                    break;
                case "∀yi":
                    All_Introduction("∀y");
                    break;
                case "∀xe":
                    All_Elimination("∀x");
                    break;
                case "∀ye":
                    All_Elimination("∀y");
                    break;
                case "∃xi":
                    Exists_Introduction("∃x");
                    break;
                case "∃yi":
                    Exists_Introduction("∃y");
                    break;
                case "∃xe":
                    Exists_Elimination("∃x");
                    break;
                case "∃ye":
                    Exists_Elimination("∃y");
                    break;
                default:
                    Utility.DisplayErrorMsg("Unknown rule (This is a programming bug and should not happen)\n rule is: " + rule, current_line);
                    break;
            }
        }
        private List<string> GetData(string s)
        {
            List<string> ret = new List<string>();
            string[] splited = s.Split('⊢');
            string[] all_data = splited[0].Split(',');
            foreach (var d in all_data)
            {
                ret.Add(d);
            }
            return ret;
        }
        private string GetGoal(string s)
        {
            if (!s.Contains('⊢'))
                return null;
            string[] splited = s.Split('⊢');
            return splited[1];
        }
        private void Proven_Elimination() //TODO: change this function for using 'Match_Data'
        {
            Dictionary<string, string> matches = new Dictionary<string, string>();
            int proven_index = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            if (statement_list[proven_index].Rule != "Proveni")
            {
                Utility.DisplayErrorMsg("Proven elimination first segment must point to 'Proven i' rule line", current_line);
                return;
            }
            List<string> proven_data = GetData(statement_list[proven_index].Expression);
            List<int> data_indexes = Utility.Get_Rows_For_Proven(statement_list[current_line].Second_segment);
            if (data_indexes == null)
            {
                Utility.DisplayErrorMsg("Proven elimination first segment must be positive integer\nSecond segment must be positive integers separate by ','", current_line);
                return;
            }
            List<string> provided_data = new List<string>();
            foreach (int index in data_indexes)
            {
                provided_data.Add(statement_list[index].Expression);
            }
            if (proven_data.Count() != provided_data.Count())
            {
                Utility.DisplayErrorMsg($"Missmatch between provided data expected ({proven_data.Count()}) to actual given ({provided_data.Count()})", current_line);
                return;
            }
            string proven_goal = GetGoal(statement_list[proven_index].Expression);
            string current_goal = statement_list[current_line].Expression;
            // ------------ALL DATA RECIEVED-------------//
            // ------------BUILD DICT-------------//
            for (int i = 0; i < provided_data.Count(); i++)
            {
                matches = BuildDict(proven_data[i], provided_data[i], matches);
                if (matches == null)
                    return;
            }
            // ------------CHECK RESULTS-------------//
            Tuple<bool, string> ret;
            for (int i = 0; i < provided_data.Count(); i++)
            {
                if (!(ret = ReplaceAndCompare(proven_data[i], provided_data[i], matches)).Item1)
                {
                    Utility.DisplayErrorMsg($"Line number {data_indexes[i]} expression expected to be: '{ret.Item2}' and not '{provided_data[i]}'", current_line);
                    return;
                }
            }
            if (!(ret = ReplaceAndCompare(proven_goal, current_goal, matches)).Item1)
            {
                Utility.DisplayErrorMsg($"Goal expression expected to be: '{ret.Item2}' and not '{current_goal}'", current_line);
                return;
            }
            // ------------SUCCESS-------------//
            Is_Valid = true;
            return;
        }
        private Dictionary<string, string> BuildDict(string proven, string data, Dictionary<string, string> matches)
        {
            int p_proven = 0, p_data = 0; //string pointers
            int proven_brackets_counter = 0; //check brackets level
            if (proven.Length > data.Length)
            {
                Utility.DisplayErrorMsg($"Unable to generate dictionary, '{proven}' pattern don't match to '{data}'", current_line);
                return null;
            }
            for (; p_proven < proven.Length && proven.Length != 1; p_proven++)
            {
                if (Utility.IsOperator(proven[p_proven]))
                {
                    if (!proven[p_proven].Equals(data[p_data]))
                    {
                        Utility.DisplayErrorMsg($"Unable to generate dictionary, string patterns don't match\nIndex {p_data} of '{data}' expected to be '{proven[p_proven]}' not '{data[p_data]}'", current_line);
                        return null;
                    }
                    p_data++;
                }
                else if (proven[p_proven].Equals('('))
                {
                    if (!proven[p_proven].Equals(data[p_data]))
                    {
                        Utility.DisplayErrorMsg($"Unable to generate dictionary, string patterns don't match\nIndex {p_data} of '{data}' expected to be '{proven[p_proven]}' not '{data[p_data]}'", current_line);
                        return null;
                    }
                    p_data++;
                }
                else if (proven[p_proven].Equals(')'))
                {
                    if (!proven[p_proven].Equals(data[p_data]))
                    {
                        Utility.DisplayErrorMsg($"Unable to generate dictionary, string patterns don't match\nIndex {p_data} of '{data}' expected to be '{proven[p_proven]}' not '{data[p_data]}'", current_line);
                        return null;
                    }
                    p_data++;
                }
                else // VARIABLE
                {
                    string k = proven[p_proven].ToString();
                    string v = "";
                    int data_brackets_counter = proven_brackets_counter;
                    for (; p_data < data.Length; p_data++)
                    {
                        if (Utility.IsOperator(data[p_data]) && proven_brackets_counter == data_brackets_counter)
                        {
                            break;
                        }
                        else if (data[p_data].Equals('('))
                        {

                            data_brackets_counter++;
                            if (proven_brackets_counter < data_brackets_counter - 1)
                            {
                                v += data[p_data];
                            }
                        }
                        else if (data[p_data].Equals(')'))
                        {
                            data_brackets_counter--;
                            if (proven_brackets_counter < data_brackets_counter)
                            {
                                v += data[p_data];
                            }
                            else if (proven_brackets_counter > data_brackets_counter)
                            {
                                break;
                            }

                        }
                        else
                        {
                            v += data[p_data];
                        }
                    }
                    if (matches.ContainsKey(k))
                    {
                        if (!matches[k].Equals(v))
                        {
                            Utility.DisplayErrorMsg($"Duplicate key - value: '{k}' is already '{matches[k]}', can't be '{v}' also", current_line);
                            return null;
                        }
                    }
                    else
                    {
                        matches.Add(k, v);
                    }
                }

            }
            if (proven.Length == 1)
                if (!matches.ContainsKey(proven))
                    matches.Add(proven, data);
                else
                {
                    if (!matches[proven].Equals(data))
                    {
                        Utility.DisplayErrorMsg($"Duplicate key - value: '{proven}' is already '{matches[proven]}', can't be '{data}' also", current_line);
                        return null;
                    }
                }
            return matches;
        }
        private Tuple<bool, string> ReplaceAndCompare(string proven, string data, Dictionary<string, string> matches)
        {
            foreach (string k in matches.Keys)
            {
                if (proven.Contains(k))
                {
                    if (matches[k].Length > 1 && proven.Length > 1)
                        proven = proven.Replace(k, $"({matches[k]})");
                    else
                        proven = proven.Replace(k, matches[k]);
                }
            }
            return Tuple.Create(proven.Equals(data), proven);
        }
        private void None()
        {
            Is_Valid = statement_list[current_line].Expression.Equals("X0") ||
                statement_list[current_line].Expression.Equals("Y0");
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Empty rule must be X0 or Y0", current_line);
            return;
        }
        private void Data()
        {
            int index = statement_list[0].Expression.IndexOf("⊢");
            if (index != -1)
                statement_list[0].Expression = statement_list[0].Expression.Substring(0, index);
            string current_expression = statement_list[current_line].Expression;
            string[] split = statement_list[0].Expression.Split(',');
            for (int i = 0; i < split.Length; i++)
            {
                Is_Valid = Utility.Equal_With_Parenthesis(split[i], current_expression);
                if (Is_Valid)
                    return;
                string check = split[i];
                if (i == split.Length - 1)
                    continue;
                for (int j = i + 1; j < split.Length; j++)
                {
                    check += "," + split[j];
                    if (check.Length > current_expression.Length)
                        continue;
                    Is_Valid = Utility.Equal_With_Parenthesis(check, current_expression);
                    if (Is_Valid)
                        return;
                }
            }
            Is_Valid = false;
            Utility.DisplayErrorMsg("Data: " + current_expression + " doesn't exist in the original expression", current_line);
        }
        private void Copy()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("copy fk", current_line);
                return;
            }
            Is_Valid = statement_list[row].Expression == statement_list[current_line].Expression;
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Values should be equal", current_line);
        }
        private void MP()
        {
            int first_row = Utility.Get_Row(statement_list[current_line].First_segment, current_line),
            second_row = Utility.Get_Row(statement_list[current_line].Second_segment, current_line);
            Is_Valid = first_row != -1 && second_row != -1;
            if (!Is_Valid)
            {
                return;
            }

            string first_expression = statement_list[first_row].Expression,
                  second_expression = statement_list[second_row].Expression,
                  current_expression = statement_list[current_line].Expression;
            Is_Valid = (first_expression == second_expression + "→" + current_expression)
                || (first_expression == second_expression + "→(" + current_expression + ")")
                || (second_expression == first_expression + "→" + current_expression)
                || (second_expression == first_expression + "→(" + current_expression + ")")
                || Utility.Equal_With_Operator(first_expression, second_expression, current_expression, "→")
                || Utility.Equal_With_Operator(second_expression, first_expression, current_expression, "→");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of MP", current_line);
            }
        }
        private void MT()
        {
            int first_row = Utility.Get_Row(statement_list[current_line].First_segment, current_line),
            second_row = Utility.Get_Row(statement_list[current_line].Second_segment, current_line), index;
            Is_Valid = first_row != -1 && second_row != -1;
            if (!Is_Valid)
            {
                return;
            }

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

                Is_Valid = Utility.Check_If_Not(right_part, second_expression);
                if (!Is_Valid)
                {
                    Utility.DisplayErrorMsg("MT missing ¬", current_line);
                    return;
                }
            }
            else //check if the second expression contains ->
            {
                index = second_expression.IndexOf("→");
                Is_Valid = index != -1;
                if (Is_Valid)
                {
                    left_part = second_expression.Substring(0, index);
                    right_part = second_expression.Substring(index + 1);
                    Is_Valid = Utility.Check_If_Not(right_part, first_expression);
                    if (!Is_Valid)
                    {
                        Utility.DisplayErrorMsg("MT missing ¬", current_line);
                        return;
                    }
                }
                else
                {
                    Utility.DisplayErrorMsg("MT was called without →", current_line);
                    return;
                }
            }
            Is_Valid = current_expression == "¬" + left_part;
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Misuse of MT", current_line);
        }
        private void PBC()
        {
            List<int> rows = Utility.Get_Lines_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = rows != null;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = statement_list[rows[rows.Count - 1]].Expression == "⊥";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ⊥ at the previous line", current_line);
                return;
            }
            Is_Valid &= Utility.Check_If_Not(statement_list[current_line].Expression, statement_list[rows[0]].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Misuse of PBC", current_line);
        }
        private void LEM()
        {
            string left_part, right_part, expression = statement_list[current_line].Expression;
            int index = expression.IndexOf("∨");
            Is_Valid = index != -1;
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("LEM without V or ∨", current_line);
                return;
            }
            left_part = expression.Substring(0, index);
            right_part = expression.Substring(index + 1);
            Is_Valid = Utility.Check_If_Not(left_part, right_part);
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of LEM", current_line);
            }
        }
        private void And_Introduction()
        {
            int first_row = Utility.Get_Row(statement_list[current_line].First_segment, current_line),
            second_row = Utility.Get_Row(statement_list[current_line].Second_segment, current_line);
            string current = statement_list[current_line].Expression;
            Is_Valid = first_row != -1 && second_row != -1;
            if (!Is_Valid)
            {
                return;
            }

            Is_Valid = current.Contains("∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∧ in and introduction", current_line);
                return;
            }

            string first = statement_list[first_row].Expression;
            string second = statement_list[second_row].Expression;
            Is_Valid = Utility.Equal_With_Operator(current, first, second, "∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of And Introduction", current_line);
            }
        }
        private void And_Elimination_One()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[row].Expression,
                current_expression = statement_list[current_line].Expression;
            Is_Valid = original_expression.Contains("∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∧ in row "+row, current_line);
                return;
            }
            Is_Valid = original_expression.Contains(current_expression + "∧")
             || original_expression.Contains("(" + current_expression + ")∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of And Elimination 1", current_line);
            }
        }
        private void And_Elimination_Two()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[row].Expression,
                current_expression = statement_list[current_line].Expression;

            Is_Valid = original_expression.Contains("∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∧ in and elimination", current_line);
                return;
            }
            Is_Valid = original_expression.Contains("∧" + current_expression) ||
            original_expression.Contains("∧(" + current_expression + ")");

            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of And Elimination 2", current_line);
            }
        }
        private void Or_Elimination()
        {
            List<int> first_segment_lines = Utility.Get_Lines_From_Segment(statement_list[current_line].Second_segment, current_line),
                      second_segment_lines = Utility.Get_Lines_From_Segment(statement_list[current_line].Third_segment, current_line);
            Is_Valid = first_segment_lines != null && second_segment_lines != null;
            if (!Is_Valid)
            {
                return;
            }
            int base_row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            string current_expression = statement_list[current_line].Expression,
                   base_expression = statement_list[base_row].Expression,
                   first_segment_start = statement_list[first_segment_lines[0]].Expression,
                   second_segment_start = statement_list[second_segment_lines[0]].Expression,
                   first_segment_end = statement_list[first_segment_lines[first_segment_lines.Count - 1]].Expression,
                   second_segment_end = statement_list[second_segment_lines[second_segment_lines.Count - 1]].Expression;

            Is_Valid = current_expression == first_segment_end && current_expression == second_segment_end;

            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg(current_expression + " Should be equal to " + first_segment_end + " and to " + second_segment_end, current_line);
                return;
            }
            Is_Valid = Utility.Equal_With_Operator(base_expression, first_segment_start, second_segment_start, "∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg(base_expression + " Should be equal to " + first_segment_start + "∨" + second_segment_start, current_line);
                return;
            }
        }
        private void Or_Introduction_One()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current_expression = statement_list[current_line].Expression;
            Is_Valid = current_expression.Contains("∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∨ in or introduction", current_line);
                return;
            }
            Is_Valid = current_expression.Contains(statement_list[row].Expression + "∨")
                || current_expression.Contains("(" + statement_list[row].Expression + ")∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of or1 introduction", current_line);
            }
        }
        private void Or_Introduction_Two()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current_statement = statement_list[current_line].Expression;
            Is_Valid = current_statement.Contains("∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∨ in or introduction", current_line);
                return;
            }

            Is_Valid = current_statement.Contains("∨" + statement_list[row].Expression)
                || current_statement.Contains("∨(" + statement_list[row].Expression + ")");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of or2 introduction", current_line);
            }
        }
        private void Not_Introduction()
        {
            Is_Valid = statement_list[current_line - 1].Expression == "⊥";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ⊥ at the previous row", current_line);
                return;
            }
            int first_line = Utility.Get_First_Line_From_Segment(statement_list[current_line].First_segment);
            Is_Valid =
            Is_Valid &= Utility.Check_If_Not(statement_list[first_line].Expression, statement_list[current_line].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Missuse of Not Introduction", current_line);
        }
        private void Not_Elimination()
        {
            Is_Valid = statement_list[current_line].Expression == "⊥";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ⊥ at the current row", current_line);
                return;
            }
            int first_row = Utility.Get_Row(statement_list[current_line].First_segment, current_line),
                second_row = Utility.Get_Row(statement_list[current_line].Second_segment, current_line);
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = Utility.Check_If_Not(statement_list[first_row].Expression, statement_list[second_row].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Missuse of Not Elimination (⊥e)", current_line);
        }
        private void Contradiction_Elimination()
        {
            Is_Valid = statement_list[current_line - 1].Expression == "⊥";
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Missing ⊥ at the previous row", current_line);
        }
        private void Not_Not_Elimination()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[row].Expression,
                current_expression = statement_list[current_line].Expression;
            Is_Valid = original_expression == "¬¬" + current_expression
                || original_expression == "¬¬(" + current_expression + ")";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of Not Not Elimination", current_line);
            }
        }
        private void Not_Not_Introduction()
        {
            int row = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            Is_Valid = row != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = statement_list[current_line].Expression == "¬¬" + statement_list[row].Expression;
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of Not Not Introduction", current_line);
            }
        }
        private void Arrow_Introduction()
        {
            List<int> rows = Utility.Get_Lines_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = rows != null;
            if (!Is_Valid)
            {
                return;
            }
            int start_row = rows[0],
            end_row = rows[rows.Count - 1];
            string current_expression = statement_list[current_line].Expression;

            Is_Valid = current_expression.Contains("→");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing → in row", current_line);
                return;
            }

            Is_Valid = statement_list[start_row].Rule == "Assumption" &&
                    current_expression == statement_list[start_row].Expression + "→" + statement_list[end_row].Expression
                    || current_expression == "(" + statement_list[start_row].Expression + ")→" + statement_list[end_row].Expression
                    || current_expression == statement_list[start_row].Expression + "→(" + statement_list[end_row].Expression + ")"
                    || current_expression == "(" + statement_list[start_row].Expression + ")→(" + statement_list[end_row].Expression + ")";

            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of Arrow Introduction", current_line);
            }
        }

        #region PREDICATES
        private void Equal_Introduction()
        {
            string current_expression = statement_list[current_line].Expression;
            int index = current_expression.IndexOf('=');

            Is_Valid = index != -1;
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing = in row " + current_line, current_line);
                return;
            }
            Is_Valid = current_expression.Substring(0, index) ==
                current_expression.Substring(index + 1);
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Equal introduction format is t1=t1", current_line);
        }
        private void Equal_Elimination()
        {
            int index, first_line = Utility.Get_Row(statement_list[current_line].First_segment, current_line)
            , second_line = Utility.Get_Row(statement_list[current_line].Second_segment, current_line);

            string current_expression = statement_list[current_line].Expression,
                first_expression = statement_list[first_line].Expression,
                second_expression = statement_list[second_line].Expression,
                left, right;

            index = first_expression.IndexOf("=");
            if (index == -1)
            {
                index = second_expression.IndexOf("=");
                Is_Valid = index != -1;
                if (!Is_Valid)
                {
                    Utility.DisplayErrorMsg("= symbol must be present in line " + first_line + " or " + second_line, current_line);
                    return;
                }
                else // = symbol is in second expression
                {
                    left = second_expression.Substring(0, index);
                    right = second_expression.Substring(index + 1);
                    Is_Valid = first_expression == current_expression.Replace(left, right) ||
                   first_expression == current_expression.Replace(right, left);
                }
            }
            else // = symbol is in first expression
            {
                left = first_expression.Substring(0, index);
                right = first_expression.Substring(index + 1);
                Is_Valid = second_expression == current_expression.Replace(left, right) ||
                    second_expression == current_expression.Replace(right, left);
            }
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of equal elimination", current_line);
                return;
            }
        }
        private void All_Elimination(string rule)
        {
            int previous_line = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;

            if (!previous_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in previous row", previous_line);
                Is_Valid = false;
                return;
            }

            Is_Valid = Utility.Predicate_Check(previous_expression, rule, current_expression);
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of all elimination", current_line);
            }
        }
        private void All_Introduction(String rule)
        {
            List<int> rows = Utility.Get_Lines_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = rows != null;
            if (!Is_Valid)
            {
                return;
            }

            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[rows[rows.Count - 1]].Expression;
            string previous_letter = Utility.Find_Letter(previous_expression),
                current_letter = Utility.Find_Letter(current_expression);

            if (!current_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in current row", current_line);
                Is_Valid = false;
                return;
            }

            current_expression = current_expression.Replace(current_letter, previous_letter);
            Is_Valid = current_expression == "∀" + previous_letter + previous_expression ||
                current_expression == "∀" + previous_letter + "(" + previous_expression + ")";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of all introduction", current_line);
            }
        }

        private void Exists_Elimination(string rule)
        {
            //TODO check for double cases ∃x(P(x))^∃x(Q(x))
            int previous_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].Second_segment);
            Is_Valid = previous_line != 1;
            if (!Is_Valid)
            {
                return;
            }
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression,
                previous_contents = Utility.Get_Parenthesis_Contents(previous_expression, rule),
                current_contents = Utility.Get_Parenthesis_Contents(current_expression, rule);
            if (!previous_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in previous row", current_line);
                Is_Valid = false;
                return;
            }
            if (!current_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in current row", current_line);
                Is_Valid = false;
                return;
            }

            current_expression = current_expression.Replace(current_contents, previous_contents);
            Is_Valid = Utility.Equal_With_Parenthesis(current_expression, previous_expression);
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of exist elimination", current_line);
            }
        }
        private void Exists_Introduction(string rule)
        {
            int previous_line = Utility.Get_Row(statement_list[current_line].First_segment, current_line);
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;

            if (!current_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in current row", current_line);
                Is_Valid = false;
                return;
            }

            Is_Valid = Utility.Predicate_Check(current_expression, rule, previous_expression);
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of exist introduction", current_line);
            }
        }
        #endregion 
        #endregion
        #region UTILITY
        private Dictionary<string, string> Match_Data(string gen, string actual, Dictionary<string, string> known)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            int act_p = 0;
            for (int gen_p = 0; gen_p < gen.Length; gen_p++)
            {
                char gen_c = gen[gen_p];
                if (!Utility.IsOperator(gen_c)) //variable
                {
                    int k = gen_p + 1;
                    while (!Utility.IsOperator(gen[k]) && k < gen.Length)
                        k++;
                    char stop = gen[k];
                    int r = act_p;
                    while (!actual[r].Equals(stop) && r < actual.Length)
                        r++;
                    string key = gen.Substring(gen_p, k - gen_p);
                    string value = actual.Substring(act_p, r - act_p);
                    ret.Add(key, value);
                }
                else
                    act_p++;
            }
            return ret;
        }
        #endregion UTILITY
    }
}