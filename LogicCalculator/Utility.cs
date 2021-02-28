using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LogicCalculator
{
    static class Utility
    {
        internal static readonly int PREDICATE_LENGTH = 3;
        internal static string FlipByOperator(string s,char op)        {
           
            if (s[0] == '(')
                s = s.Substring(1, s.Length - 2);
            int index = s.IndexOf(op);
            s = s.Substring(index + 1) + op + s.Substring(0, index);
            return s;
        }
        internal static string ReplaceAll(string s)
        {
            return s.Trim().Replace('^', '∧').Replace('V', '∨').Replace('~', '¬').Replace(" ", "").Replace('>', '→');
        }
        internal static string RemoveRedundant(string s)
        {
            while (s.Length>2&&s[0] == '(' && s[s.Length - 1] == ')')
                s = s.Substring(1, s.Length - 2);
            return s;
        }
        internal static string Find_Letter(string to_search)
        {
            string letter = "error in Find_Letter";
            for (int i = 0; i < to_search.Length - PREDICATE_LENGTH; i++)
            {
                if (Char.IsUpper(to_search[i]))
                {
                    if (to_search[i + 3] == '0')
                        letter = to_search.Substring(i + 2, 2);
                    else
                        letter = to_search.Substring(i + 2, 1);
                    break;
                }
            }
            return letter;
        }
        internal static string Get_Parenthesis_Contents(string s, string start)
        {
            ///searches for parameter start in string s. if found searches for the closest '(' and returns the string between matching ')'
            int i, j, counter = 1;

            i = s.IndexOf(start);

            if (i == -1)
            {
                return "Error in Get_Parenthesis_Contents: could not find " + start + " in " + s;
            }

            for (; i < s.Length; i++)
            {
                if (s[i] == '(')
                    break;
            }

            for (j = i; j < s.Length; j++)
            {
                if (s[j] == '(')
                    counter++;
                if (s[j] == ')')
                    counter--;
                if (counter <= 0)
                    break;
            }
            return s.Substring(i, j - i);
        }
        internal static bool Predicate_Check(string original, string rule, string to_check)
        {
            int i, j, counter = 0, start;
            string temp;
            List<string> ret = new List<string>();
           
            i = original.IndexOf(rule);            //find index of predicate rule
            if (i == -1 || original.Substring(0, i) != to_check.Substring(0, i))
                return false;

            for (; i < original.Length; i++)       //skip until reaching '('
            {
                if (original[i] == '(')
                {
                    counter++;
                    break;
                }
            }
            start = i+2;                           //save starting index
            for (j = i + 1; j < original.Length; j++)
            {
                if (original[j] == '(')
                    counter++;
                if (original[j] == ')')
                    counter--;
                if (counter <= 0)
                    break;
                if (original[j] == rule[1])        //save all needed strings in the list
                {
                    ret.Add(original.Substring(i + 1, j - i - 1));
                    i = j;
                }
            }
            temp = original.Substring(i + 1, j - i - 1);
            if (temp.Length > 1)
                ret.Add(temp.Substring(0, temp.Length - 1));//add the last string only if its legit
            j++;           
            if (j < original.Length && original.Substring(j, original.Length - j) != to_check.Substring(j - start, to_check.Length - j + start))
                return false;
            foreach (string s in ret)
            {
                if (!to_check.Contains(s))
                    return false;
            }
            return true;
        }

        #region Segment_Handeling
        internal static int Get_Row(string s, int current_line)
        {
            if (s.Contains("-"))
            {
                DisplayErrorMsg("Segment contains '-' when it should not", current_line);
                return -1;
            }
            int ret = Int32.Parse(s);
            if (ret < 1)
            {
                DisplayErrorMsg("Line number must be bigger than 0", current_line);
                return -1;
            }
            if (ret > current_line)
            {
                DisplayErrorMsg("Line number can't be bigger than current line number", current_line);
            }
            if (ret == current_line)
            {
                DisplayErrorMsg("Line number provided can't be equal to current line", current_line);
                return -1;
            }
            return ret;
        }
        internal static List<int> Get_Lines_From_Segment(string seg, int current_line = -1)
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
            if (!ret.Any())
            {
                DisplayErrorMsg("Could not get line numbers from segment", current_line);
                return null;
            }

            return ret;
        }
        internal static int Get_First_Line_From_Segment(string seg)
        {
            int index = seg.IndexOf("-");
            if (index == -1)
            {
                return -1;
            }
            return Int32.Parse(seg.Substring(0, index));
        }
        internal static int Get_Last_Line_From_Segment(string seg)
        {
            int index = seg.IndexOf("-");
            if (index == -1)
            {
                return -1;
            }
            return Int32.Parse(seg.Substring(index + 1, seg.Length - index - 1));
        }
        internal static List<int> Get_Rows_For_Proven(string seg)
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
        #endregion
        #region Checks
        internal static bool IsOperator(char c)
        {
            return  c == '∧' || c == '→' || c == '∨' || c == '⊢' || c == '⊥' || c == '¬' || c == '=';
        }
        internal static bool HasOperatorInside(string s)
        {
            if (s[0] == '(')
                return false;
            foreach(char c in s)
            {
                if (c == '∧' || c == '→' || c == '∨' || c == '⊢' || c == '⊥' || c == '=')//this is different from IsOperator because we don't check for '¬' 
                    return true;
            }
            return false;
        }
        internal static bool Check_If_Not(string left, string right)
        {
            //left = RemoveRedundant(left);
            //right = RemoveRedundant(right);
            
            while(left[0]== '¬'&&right[0]== '¬')
            {
                right = right.Substring(1);
                left = left.Substring(1);
            }
            if (left[0] == '¬')
                left = left.Substring(1);
            else if (right[0] == '¬')
                right = right.Substring(1);
            else return false;
            return Equal_With_Parenthesis(left, right);
        }
        internal static bool Equal_With_Parenthesis(string first, string second)
        {
            //first = RemoveRedundant(first);
            //second = RemoveRedundant(second);
            return first == second || '(' + first + ')' == second || first == '(' + second + ')' || '(' + first + ')' == '(' + second + ')';
        }
        internal static bool Equal_With_Operator(string expression, string first, string second, string op)
        {
            //first = RemoveRedundant(first);
            //second = RemoveRedundant(second);
            return expression == first + op + second || expression == '(' + first + ')' + op + second || expression == first + op + '(' + second + ')' || expression == '(' + first + ')' + op + '(' + second + ')' ||
                expression == second + op + first || expression == '(' + second + ')' + op + first || expression == second + op + '(' + first + ')' || expression == '(' + second + ')' + op + '(' + first + ')';
        }
        #endregion
        #region Error_Messages
        internal static void DisplayErrorMsg(string msg, int current_line = -1)
        {
            if (current_line != -1)
                msg = "Error at row " + (current_line) + "\n" + msg;
            MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        internal static void DisplayWarningMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        internal static void DisplayInfoMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        #endregion
    }
}