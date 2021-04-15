using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicalProofTool
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
                case "X0/Y0i":
                    Var_Introduction();
                    break;
                case "Proveni":
                    //THIS ON VERIFIED IN MAINWINDOW
                    Is_Valid = true;
                    break;
                case "Provene":
                    Proven_Elimination();
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

        // X0/Y0 intro
        private void Var_Introduction()
        {
            Is_Valid = statement_list[current_line].Expression.Equals("X0") ||
                statement_list[current_line].Expression.Equals("Y0");
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Expression in X0/Y0i rule must be X0 or Y0", current_line);
            return;
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
        private void Proven_Elimination()
        {
            Is_Valid = !statement_list[current_line].Expression.Any(char.IsUpper);
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Proven elimination is not supported for predicates", current_line);
                return;
            }
            Dictionary<string, string> matches = new Dictionary<string, string>();
            int proven_index = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = proven_index != -1;
            if (!Is_Valid)
            {
                return;
            }
            if (statement_list[proven_index].Rule != "Proveni")
            {
                Utility.DisplayErrorMsg("Proven elimination first segment must point to 'Proven i' rule line", current_line);
                Is_Valid = false;
                return;
            }
            List<string> proven_data = GetData(statement_list[proven_index].Expression);
            List<int> data_indexes = Utility.Get_Lines_For_Proven(statement_list[current_line].Second_segment);
            if (data_indexes == null)
            {
                Utility.DisplayErrorMsg("Proven elimination first segment must be positive integer\nSecond segment must be positive integers separate by ','", current_line);
                Is_Valid = false;
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
                Is_Valid = false;
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
                {
                    Is_Valid = false;
                    return;
                }
            }
            // ------------CHECK RESULTS-------------//
            Tuple<bool, string> ret;
            for (int i = 0; i < provided_data.Count(); i++)
            {
                if (!(ret = ReplaceAndCompare(proven_data[i], provided_data[i], matches)).Item1)
                {
                    Utility.DisplayErrorMsg($"Line number {data_indexes[i]} expression expected to be: '{ret.Item2}' and not '{provided_data[i]}'", current_line);
                    Is_Valid = false;
                    return;
                }
            }
            if (!(ret = ReplaceAndCompare(proven_goal, current_goal, matches)).Item1)
            {
                Utility.DisplayErrorMsg($"Goal expression expected to be: '{ret.Item2}' and not '{current_goal}'", current_line);
                Is_Valid = false;
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
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = Utility.Equal_With_Parenthesis(statement_list[line].Expression, statement_list[current_line].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Copy: \"" + statement_list[line].Expression + "\" should be equal to \"" + statement_list[current_line].Expression + "\"", current_line);
        }
        private void MP()
        {
            int first_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line),
            index, second_line = Utility.Get_Line(statement_list[current_line].Second_segment, current_line);
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = second_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            string first_expression = statement_list[first_line].Expression,
                  second_expression = statement_list[second_line].Expression,
                  current_expression = statement_list[current_line].Expression;

            index = first_expression.IndexOf("→");

            //if the first expression contains ->
            if (index == -1)
            {
                index = second_expression.IndexOf("→");
                if (index == -1)
                {
                    Is_Valid = false;
                    Utility.DisplayErrorMsg("MP called without → in lines mentioned in the segment boxes", current_line);
                    return;
                }
            }

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
            int first_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line),
            second_line = Utility.Get_Line(statement_list[current_line].Second_segment, current_line), index;
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = second_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            string left_part, right_part,
                first_expression = statement_list[first_line].Expression,
                second_expression = statement_list[second_line].Expression,
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
                    index = second_expression.IndexOf("→");
                    Is_Valid = index != -1;
                    if (Is_Valid)
                    {
                        left_part = second_expression.Substring(0, index);
                        right_part = second_expression.Substring(index + 1);
                        Is_Valid = Utility.Check_If_Not(right_part, first_expression);
                                            }
                    if (!Is_Valid)
                    {
                        Utility.DisplayErrorMsg("MT: missing ¬", current_line);
                        return;
                    }
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
                        Utility.DisplayErrorMsg("MT: missing ¬", current_line);
                        return;
                    }
                }
                else
                {
                    Utility.DisplayErrorMsg("MT was called without → in lines mentioned in segment boxes", current_line);
                    return;
                }
            }
            if (!Is_Valid)
                Utility.DisplayErrorMsg("MT: \"" + left_part + "\" must be the negation of \"" + current_expression + "\"", current_line);
        }
        private void PBC()
        {
            int last_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = last_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int first_line = Utility.Get_First_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            Is_Valid = statement_list[last_line].Expression == "⊥";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("PBC: Missing ⊥ at the previous line", current_line);
                return;
            }
            Is_Valid &= Utility.Check_If_Not(statement_list[current_line].Expression, statement_list[first_line].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg("PBC: \"" + statement_list[current_line].Expression + "\" must be the negation of \"" + statement_list[first_line].Expression + "\"", current_line);
        }
        private void LEM()
        {
            string left_part, right_part, expression = statement_list[current_line].Expression;
            int index = expression.IndexOf("∨");
            Is_Valid = index != -1;
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("LEM must contain ∨", current_line);
                return;
            }
            left_part = expression.Substring(0, index);
            right_part = expression.Substring(index + 1);
            Is_Valid = Utility.Check_If_Not(left_part, right_part);
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("LEM \"" + left_part + "\" must be the negation of \"" + right_part + "\"", current_line);
            }
        }
        private void And_Introduction()
        {
            int first_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            int second_line = Utility.Get_Line(statement_list[current_line].Second_segment, current_line);
            Is_Valid = second_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current = statement_list[current_line].Expression;


            Is_Valid = current.Contains("∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∧ in and introduction", current_line);
                return;
            }

            string first = statement_list[first_line].Expression;
            string second = statement_list[second_line].Expression;
            Is_Valid = Utility.Equal_With_Operator(current, first, second, "∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∧i: \"" + current + "\" should be equal to \"" +
                    first + "∧" + second + "\" or \""
                    + second + "∧" + first + "\"", current_line);
            }
        }
        private void And_Elimination_One()
        {
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[line].Expression,
                current_expression = statement_list[current_line].Expression;
            Is_Valid = original_expression.Contains("∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∧ in 'and elimination 1'" + line, current_line);
                return;
            }
            Is_Valid = original_expression.Contains(current_expression + "∧")
             || original_expression.Contains("(" + current_expression + ")∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∧e1: \"" + original_expression + "\" should be equal to \"" +
                    current_expression + "\" ∧ <something>", current_line);
            }
        }
        private void And_Elimination_Two()
        {
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[line].Expression,
                current_expression = statement_list[current_line].Expression;

            Is_Valid = original_expression.Contains("∧");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∧ in 'and elimination 2'", current_line);
                return;
            }
            Is_Valid = original_expression.Contains("∧" + current_expression) ||
            original_expression.Contains("∧(" + current_expression + ")");

            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∧e2: \"" + original_expression + "\" should be equal to " +
                    " <something> ∧\"" + current_expression + "\" ", current_line);
            }
        }
        private void Or_Elimination()
        {
            int second_seg_last_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].Second_segment, current_line);
            Is_Valid = second_seg_last_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int second_seg_first_line = Utility.Get_First_Line_From_Segment(statement_list[current_line].Second_segment, current_line);
            Is_Valid = second_seg_first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int third_seg_last_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].Third_segment, current_line);
            Is_Valid = third_seg_last_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int third_seg_first_line = Utility.Get_First_Line_From_Segment(statement_list[current_line].Third_segment, current_line);
            Is_Valid = third_seg_first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int base_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = base_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            string current_expression = statement_list[current_line].Expression,
                   base_expression = statement_list[base_line].Expression,
                   second_segment_start = statement_list[second_seg_first_line].Expression,
                   second_segment_end = statement_list[second_seg_last_line].Expression,
                   third_segment_start = statement_list[third_seg_first_line].Expression,
                   third_segment_end = statement_list[third_seg_last_line].Expression;

            Is_Valid = current_expression == third_segment_end && current_expression == second_segment_end;

            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∨e: " + current_expression + " Should be equal to " + third_segment_end + " and to " + second_segment_end, current_line);
                return;
            }
            Is_Valid = Utility.Equal_With_Operator(base_expression, third_segment_start, second_segment_start, "∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∨e: " + base_expression + " Should be equal to " + third_segment_start + "∨" + second_segment_start, current_line);
                return;
            }
        }
        private void Or_Introduction_One()
        {
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current_expression = statement_list[current_line].Expression;
            Is_Valid = current_expression.Contains("∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∨ in 'or introduction 1'", current_line);
                return;
            }
            Is_Valid = current_expression.Contains(statement_list[line].Expression + "∨")
                || current_expression.Contains("(" + statement_list[line].Expression + ")∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∨i1: \"" + current_expression + "\" should be equal to " +
                   statement_list[line].Expression + "∨ <something>", current_line);
            }
        }
        private void Or_Introduction_Two()
        {
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current_expression = statement_list[current_line].Expression;
            Is_Valid = current_expression.Contains("∨");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ∨ in 'or introduction 2'", current_line);
                return;
            }

            Is_Valid = current_expression.Contains("∨" + statement_list[line].Expression)
                || current_expression.Contains("∨(" + statement_list[line].Expression + ")");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("∨i2: \"" + current_expression + "\" should be equal to <something> ∨ \"" +
                   statement_list[line].Expression + "\"", current_line);
            }
        }
        private void Not_Introduction()
        {
            int last_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = last_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int first_line = Utility.Get_First_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = statement_list[last_line].Expression == "⊥";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ⊥ at the provided line", current_line);
                return;
            }
            Is_Valid &= Utility.Check_If_Not(statement_list[first_line].Expression, statement_list[current_line].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg(statement_list[first_line].Expression + " should be the negation of " + statement_list[current_line].Expression, current_line);
        }
        private void Not_Elimination()
        {
            Is_Valid = statement_list[current_line].Expression == "⊥";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing ⊥ at the current line", current_line);
                return;
            }
            int first_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int second_line = Utility.Get_Line(statement_list[current_line].Second_segment, current_line);
            Is_Valid = second_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = Utility.Check_If_Not(statement_list[first_line].Expression, statement_list[second_line].Expression);
            if (!Is_Valid)
                Utility.DisplayErrorMsg(statement_list[first_line].Expression + " should be the negation of " + statement_list[second_line].Expression, current_line);
        }
        private void Contradiction_Elimination()
        {
            int prev_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = prev_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            Is_Valid = statement_list[prev_line].Expression == "⊥";
            if (!Is_Valid)
                Utility.DisplayErrorMsg("Missing ⊥ at the line mentioned in the segment", current_line);
        }
        private void Not_Not_Elimination()
        {
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[line].Expression,
                current_expression = statement_list[current_line].Expression;
            Is_Valid = original_expression.Contains("¬¬");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("'Not Not Elimination' called without ¬¬", current_line);
                return;
            }
            Is_Valid = Utility.Equal_With_Parenthesis(original_expression, "¬¬" + current_expression)
                || Utility.Equal_With_Parenthesis(original_expression, "¬¬(" + current_expression + ")");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg(original_expression + " should be equal to ¬¬(" + current_expression + ")", current_line);
            }
        }
        private void Not_Not_Introduction()
        {
            int line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string original_expression = statement_list[line].Expression,
               current_expression = statement_list[current_line].Expression;
            bool contains_operator = false;
            foreach (char c in statement_list[line].Expression)
            {
                if (Utility.IsOperator(c))
                {
                    contains_operator = true;
                    break;
                }
            }
            if (contains_operator)
                Is_Valid = current_expression == "¬¬(" + original_expression + ")";
            else
                Is_Valid = current_expression == "¬¬" + original_expression ||
                    current_expression == "¬¬(" + original_expression + ")";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg(current_expression + " should be equal to ¬¬(" + original_expression + ")", current_line);
            }
        }
        private void Arrow_Introduction()
        {
            int last_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = last_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int first_line = Utility.Get_First_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            string current_expression = statement_list[current_line].Expression,
                start_expression = statement_list[first_line].Expression,
                end_expression = statement_list[last_line].Expression;

            Is_Valid = current_expression.Contains("→");
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Missing → in 'arrow introduction", current_line);
                return;
            }
            Is_Valid = statement_list[first_line].Rule == "Assumption";
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("The first segment provided in arrow introduction must start with assumption", current_line);
                return;
            }

            Is_Valid = current_expression == start_expression + "→" + end_expression
                    || current_expression == "(" + start_expression + ")→" + end_expression
                    || current_expression == start_expression + "→(" + end_expression + ")"
                    || current_expression == "(" + start_expression + ")→(" + end_expression + ")";

            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg(current_expression + " should be equal to " + start_expression + "→" + end_expression, current_line);
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
                Utility.DisplayErrorMsg("Missing = in 'equal introduction' " + current_line, current_line);
                return;
            }
            Is_Valid = Utility.Equal_With_Parenthesis(current_expression.Substring(0, index),
                current_expression.Substring(index + 1));
            if (!Is_Valid)
                Utility.DisplayErrorMsg("=i:" + current_expression.Substring(0, index) +
                    " should be equal to " + current_expression.Substring(index + 1), current_line);
        }
        private void Equal_Elimination()
        {
            int index, first_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);

            Is_Valid = first_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            int second_line = Utility.Get_Line(statement_list[current_line].Second_segment, current_line);
            Is_Valid = second_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            string current_expression = statement_list[current_line].Expression,
                first_expression = statement_list[first_line].Expression,
                second_expression = statement_list[second_line].Expression,
                left, right, reverse;

            if (first_expression[0] == '(' && first_expression[first_expression.Length - 1] == ')')
                first_expression = first_expression.Substring(1, first_expression.Length - 2);
            if (second_expression[0] == '(' && second_expression[second_expression.Length - 1] == ')')
                second_expression = second_expression.Substring(1, second_expression.Length - 2);
            if (current_expression[0] == '(' && current_expression[current_expression.Length - 1] == ')')
                current_expression = current_expression.Substring(1, current_expression.Length - 2);

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
                else // '=' symbol is in second expression
                {
                    left = second_expression.Substring(0, index);
                    right = second_expression.Substring(index + 1);

                    Is_Valid = Utility.Equal_With_Parenthesis(first_expression, current_expression.Replace(left, right)) ||
                        Utility.Equal_With_Parenthesis(first_expression, current_expression.Replace(right, left));

                    if (!Is_Valid)
                    {
                        Utility.DisplayErrorMsg("Misuse of equal elimination", current_line);
                        return;
                    }
                    if (!Is_Valid && first_expression.Contains('='))
                    {
                        reverse = Utility.FlipByOperator(first_expression, '=');
                        Is_Valid = Utility.Equal_With_Parenthesis(reverse, current_expression.Replace(left, right)) ||
                            Utility.Equal_With_Parenthesis(reverse, current_expression.Replace(right, left));
                    }
                }
            }
            else // '=' symbol is in first expression
            {
                left = first_expression.Substring(0, index);
                right = first_expression.Substring(index + 1);
                Is_Valid = Utility.Equal_With_Parenthesis(second_expression, current_expression.Replace(left, right)) ||
                        Utility.Equal_With_Parenthesis(second_expression, current_expression.Replace(right, left));
                if (!Is_Valid && second_expression.Contains('='))
                {
                    reverse = Utility.FlipByOperator(first_expression, '=');
                    Is_Valid = Utility.Equal_With_Parenthesis(reverse, current_expression.Replace(left, right)) ||
                        Utility.Equal_With_Parenthesis(reverse, current_expression.Replace(right, left));
                }
            }
            if (!Is_Valid)
            {
                Utility.DisplayErrorMsg("Misuse of equal elimination", current_line);
                return;
            }
        }
        private void All_Elimination(string rule)
        {
            int previous_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = previous_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;

            if (!previous_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in mentioned line", previous_line);
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
            int last_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].First_segment, current_line);
            Is_Valid = last_line != -1;
            if (!Is_Valid)
            {
                return;
            }

            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[last_line].Expression;
            string previous_letter = Utility.Find_Letter(previous_expression),
                current_letter = Utility.Find_Letter(current_expression);

            if (!current_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in current line", current_line);
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
            int previous_line, original_line;
            if (statement_list[current_line].Second_segment.Contains('-'))
            {
                previous_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].Second_segment, current_line);
                Is_Valid = previous_line != -1;
                if (!Is_Valid)
                {
                    return;
                }
                original_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
                Is_Valid = original_line != -1;
                if (!Is_Valid)
                {
                    return;
                }
            }
            else
            {
                previous_line = Utility.Get_Last_Line_From_Segment(statement_list[current_line].First_segment, current_line);
                Is_Valid = previous_line != -1;
                if (!Is_Valid)
                {
                    return;
                }
                original_line = Utility.Get_Line(statement_list[current_line].Second_segment, current_line);
                Is_Valid = original_line != -1;
                if (!Is_Valid)
                {
                    return;
                }
            }
            if (!statement_list[original_line].Expression.Contains('∃'))
            {
                Utility.DisplayErrorMsg("Missing '∃' in the line given in the first segment", current_line);
                Is_Valid = false;
                return;
            }
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression,
                previous_contents = Utility.Get_Parenthesis_Contents(previous_expression, rule),
                current_contents = Utility.Get_Parenthesis_Contents(current_expression, rule);
            if (!previous_expression.Contains('∃'))
            {
                Utility.DisplayErrorMsg("Missing '∃' in the last line of the box that was given", current_line);
                Is_Valid = false;
                return;
            }
            if (!current_expression.Contains('∃'))
            {
                Utility.DisplayErrorMsg("Missing '∃' in current line", current_line);
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
            int previous_line = Utility.Get_Line(statement_list[current_line].First_segment, current_line);
            Is_Valid = previous_line != -1;
            if (!Is_Valid)
            {
                return;
            }
            string current_expression = statement_list[current_line].Expression,
                previous_expression = statement_list[previous_line].Expression;

            if (!current_expression.Contains(rule))
            {
                Utility.DisplayErrorMsg("Missing " + rule + " in current line", current_line);
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
    }
}