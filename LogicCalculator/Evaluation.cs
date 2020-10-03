using System;
using System.Collections.Generic;
using System.Windows;

namespace LogicCalculator
{
    internal class Evaluation
    {
        public bool is_valid { get; set; }
        private readonly List<Statement> statement_list;
        private readonly int current_row;

        //"data", "^i", "^e", "¬¬e", "¬¬i", "→e","→i", "MP", "MT", "Copy", "Assumption"
        public Evaluation(List<Statement> statement_list, string rule)
        {
            this.statement_list = statement_list;
            current_row = statement_list.Count;
            switch (rule)
            {
                case "Data":
                    Data();
                    break;

                case "Assumption":
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

                case "⊥e":
                    Not_Elimination();
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
            foreach (string s in statement_list[0].expression.Split(','))
            {
                is_valid = is_valid || (s == statement_list[current_row].expression);
                return;
            }
            MessageBox.Show("Error at row: " + current_row + " Data doesn't exist in the original expression");
            is_valid = false;
        }

        private void Copy()
        {
            is_valid = !(statement_list[Get_Row(statement_list[current_row].first_segment)].expression != statement_list[current_row].expression);
            if(!is_valid)
                MessageBox.Show("Error at row " + current_row + "\nMisuse of or1 introduction");
        }

        private void MP()
        {
            string first_expression = statement_list[Get_Lines_From_Segment(statement_list[current_row].first_segment)[0]].expression;
            string second_expression = statement_list[Get_Lines_From_Segment(statement_list[current_row].first_segment)[1]].expression;
            is_valid = (first_expression == second_expression + "→" + statement_list[current_row].expression)
                || (first_expression == second_expression + "→(" + statement_list[current_row].expression + ")")
                || (second_expression == first_expression + "→" + statement_list[current_row].expression)
                || (second_expression == first_expression + "→(" + statement_list[current_row].expression + ")");
        }

        private void MT()
        {
            int index, first_expression_row = Get_Lines_From_Segment(statement_list[current_row].first_segment)[0],
                second_expression_row = Get_Lines_From_Segment(statement_list[current_row].second_segment)[1];
            string left_part, right_part,
                first_expression = statement_list[first_expression_row].expression,
                second_expression = statement_list[second_expression_row].expression,
                current_expression = statement_list[current_row].expression;
            index = first_expression.IndexOf("→");
            if (index != -1)
            {
                left_part = first_expression.Substring(0, index);
                right_part = first_expression.Substring(index, first_expression.Length);
                if (right_part.Contains("~") || right_part.Contains("¬"))
                {
                    if (second_expression != right_part.Substring(1))
                    {
                        MessageBox.Show("Error at rows: " + first_expression_row + " " + second_expression_row + "\n MT missing ¬");
                        is_valid = false;
                        return;
                    }
                }
                else
                {
                    if (second_expression != "~" + right_part || second_expression != "¬" + right_part)
                    {
                        MessageBox.Show("Error at rows: " + first_expression_row + " " + second_expression_row + "\n MT missing ¬");
                        is_valid = false;
                        return;
                    }
                }
            }
            else
            {
                index = second_expression.IndexOf("→");
                if (index != -1)
                {
                    left_part = second_expression.Substring(0, index);
                    right_part = second_expression.Substring(index, second_expression.Length);
                }
                else
                {
                    MessageBox.Show("Error at row: " + current_row + "\n MT was called without →");
                    is_valid = false;
                    return;
                }
            }
            if (left_part.Contains("~") || left_part.Contains("¬"))
            {
                is_valid = current_expression == left_part.Substring(1);
            }
            else
            {
                is_valid = current_expression == "~" + left_part || current_expression == "¬" + left_part;
            }
        }

        private void PBC()
        {
            List<int> rows = Get_Lines_From_Segment(statement_list[current_row].first_segment);
            is_valid = statement_list[rows[rows.Count - 1]].expression == "⊥";
            if (!is_valid)
            {
                MessageBox.Show("Error at row " + current_row + "\nMissing ⊥ at the previous line");
                return;
            }
            is_valid &= Check_If_Not(statement_list[current_row].expression,statement_list[rows[0]].expression);
            if(!is_valid)
                MessageBox.Show("Error at row " + current_row + "\nMisuse of PBC");
        }

        private void LEM()
        {
            string left_part,right_part,expression = statement_list[current_row].expression;
            int index = expression.IndexOf("v");
            if(index==-1)
                index= expression.IndexOf("∨");
            if (index == -1)
            {
                MessageBox.Show("Error at row " + current_row + "\nLEM without V or v");
            }
            left_part = expression.Substring(0, index);
            right_part = expression.Substring(index, expression.Length);
            is_valid = Check_If_Not(left_part, right_part);
            if (!is_valid)
            {
                MessageBox.Show("Error at row " + current_row + "\nMisuse of LEM");
            }
        }

        private void And_Introduction()
        {
            string first = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            string second = statement_list[Get_Row(statement_list[current_row].second_segment)].expression;
            is_valid = statement_list[current_row].expression == first + "^" + second ||
                statement_list[current_row].expression == first + "∧" + second ||
                statement_list[current_row].expression == second + "^" + first ||
                statement_list[current_row].expression == second + "∧" + first;
        }

        private void And_Elimination_One()
        {
            string original_expression = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            is_valid = original_expression.Contains("^" + statement_list[current_row].expression) ||
            original_expression.Contains("∧" + statement_list[current_row].expression);
        }

        private void And_Elimination_Two()
        {
            string original_expression = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            is_valid = original_expression.Contains(statement_list[current_row].expression + "^")
                || original_expression.Contains(statement_list[current_row].expression + "∧");
        }

        private void Or_Elimination()
        {
            throw new NotImplementedException();
        }

        private void Or_Introduction_One()
        {
            int original_row = Get_Row(statement_list[current_row].first_segment);
            if (original_row == -1)
            {
                MessageBox.Show("Error at row " + current_row+ "\nWrong row/segment pattern");
                is_valid = false;
                return;
            }
            is_valid = statement_list[current_row].expression.Contains(statement_list[original_row].expression + "V")
                || statement_list[current_row].expression.Contains(statement_list[original_row].expression + "v");
            if (!is_valid)
            {
                MessageBox.Show("Error at row " + current_row + "\nMisuse of or1 introduction");
            }
        }

        private void Or_Introduction_Two()
        {
            int original_row = Get_Row(statement_list[current_row].first_segment);
            if (original_row == -1)
            {
                MessageBox.Show("Error at row " + current_row + "\nWrong row/segment pattern");
                is_valid = false;
                return;
            }
            is_valid = statement_list[current_row].expression.Contains("V"+statement_list[original_row].expression)
                || statement_list[current_row].expression.Contains("v"+statement_list[original_row].expression);
            if (!is_valid)
            {
                MessageBox.Show("Error at row " + current_row + "\nMisuse of or2 introduction");
            }
        }

        private void Not_Introduction()
        {
            is_valid = statement_list[current_row - 1].expression == "⊥";
            if(!is_valid)
                MessageBox.Show("Error at row: "+current_row+ "\nMissing ⊥ at the previous row");            
            is_valid &= Check_If_Not(statement_list[current_row - 2].expression, statement_list[current_row -3].expression);
            if (!is_valid)
                MessageBox.Show("Error at row: " + current_row + "\nMissuse of Not Introduction");
        }

        private void Not_Elimination()
        {
            is_valid = statement_list[current_row - 1].expression == "⊥";
            if (!is_valid)
                MessageBox.Show("Error at row: " + current_row + "\nMissing ⊥ at the previous row");
            int first_row = Get_Lines_From_Segment(statement_list[current_row].first_segment)[0],
            second_row = Get_Lines_From_Segment(statement_list[current_row].first_segment)[1];
            is_valid &= Check_If_Not(statement_list[first_row].expression, statement_list[second_row].expression);
            if (!is_valid)
                MessageBox.Show("Error at row: " + current_row + "\nMissuse of Not Elimination");

        }

        private void Not_Not_Elimination()
        {
            string original_expression = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            is_valid = original_expression == "¬¬" + statement_list[current_row].expression
                || original_expression == "¬¬(" + statement_list[current_row].expression + ")"
                || original_expression == "~~" + statement_list[current_row].expression
                || original_expression == "~~(" + statement_list[current_row].expression + ")";
        }

        private void Not_Not_Introduction()
        {
            int original_row = Get_Row(statement_list[current_row].first_segment);
            is_valid = statement_list[current_row].expression == "¬¬" + statement_list[original_row]
                || statement_list[current_row].expression == "~~" + statement_list[original_row];
        }

        private void Arrow_Introduction()
        {
            int start_row = Get_Lines_From_Segment(statement_list[current_row].first_segment)[0],
            end_row = Get_Lines_From_Segment(statement_list[current_row].first_segment)[1];

            //TODO: add box check
            if (statement_list[current_row].expression.Contains("→"))
                is_valid = statement_list[start_row].rule == "Assumption" &&
                        statement_list[current_row].expression == statement_list[start_row].expression + "→" + statement_list[end_row].expression
                        || statement_list[current_row].expression == "(" + statement_list[start_row].expression + ")→" + statement_list[end_row].expression
                        || statement_list[current_row].expression == statement_list[start_row].expression + "→(" + statement_list[end_row].expression + ")"
                        || statement_list[current_row].expression == "(" + statement_list[start_row].expression + ")→(" + statement_list[end_row].expression + ")";
            else
            {
                is_valid = false;
            }
        }

        private int Get_Row(string s)
        {
            if (s.Contains("-"))
            {
                return -1;
            }
            return Int32.Parse(s);
        }

        private List<int> Get_Lines_From_Segment(string seg)
        {
            List<int> ret = new List<int>();
            string[] spli = seg.Split(',');
            foreach (string s in spli)
            {
                int index = s.IndexOf("-");
                if (index != -1)
                {
                    for (int i = (int)(s[index - 1]); i < (s[index + 1]); i++)
                    {
                        ret.Add(i);
                    }
                }
                else
                    ret.Add(Int32.Parse(s));
            }
            return ret;
        }

        private bool Check_If_Not(string first, string second)
        {
            return first == "~" + second|| first == "¬" + second || second == "~" + first || second == "¬" + first;
        }
    }
}