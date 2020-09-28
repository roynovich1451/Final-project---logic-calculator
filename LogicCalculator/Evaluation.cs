using System;
using System.Collections.Generic;

namespace LogicCalculator
{
    class Evaluation
    {
        //"data", "^i", "^e", "¬¬e", "¬¬i", "→e","→i", "MP", "MT", "Copy", "Assumption"
        public Evaluation(List<Statement> statement_list, int current_row, string rule)
        {
            switch (rule)
            {
                case "Data":
                    Data(statement_list, current_row);
                    break;
                case "Copy":
                    Copy(statement_list, current_row);
                    break;
                case "Assumption":
                    return;
                case "MP":
                    MP(statement_list, current_row);
                    break;
                case "MT":
                    MT(statement_list, current_row);
                    break;
                case "PBC":
                    PBC(statement_list, current_row);
                    break;
                case "LEM":
                    LEM(statement_list, current_row);
                    break;
                case "∧e1":
                    And_Elimination_One(statement_list, current_row);
                    break;
                case "∧e2":
                    And_Elimination_Two(statement_list, current_row);
                    break;
                case "∨i1":
                    Or_Introduction_One(statement_list, current_row);
                    break;
                case "∨i2":
                    Or_Introduction_Two(statement_list, current_row);
                    break;
                case "∨e":
                    Or_Elimination(statement_list, current_row);
                    break;
                case "∧i":
                    And_Introduction(statement_list, current_row);
                    break;
                case "¬i":
                    Not_Elimination(statement_list, current_row);
                    break;
                case "¬¬e":                    
                    Not_Not_Elimination(statement_list, current_row);
                    break;
                case "¬¬i":
                    Not_Introduction(statement_list, current_row);
                    break;
                case "→i":
                    Arrow_Introduction(statement_list, current_row);
                    break;
                case "⊥e":
                    Assumption(statement_list, current_row);
                    break;
            }
        }

        private bool Data(List<Statement> statement_list, int current_row)
        {
            foreach (string s in statement_list[0].expression.Split(','))
            {
                if (s == statement_list[current_row].expression)
                    return true;
            }
            return false;
        }
        private bool Assumption(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool Copy(List<Statement> statement_list, int current_row)
        {
            if (statement_list[Get_Row(statement_list[current_row].first_segment)].expression != statement_list[current_row].expression)
                return false;
            return true;
        }
        private bool MP(List<Statement> statement_list, int current_row) {
            throw new NotImplementedException();
        }
        private bool MT(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool PBC(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool LEM(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool And_Introduction(List<Statement> statement_list, int current_row)
        {
            string first = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            string second = statement_list[Get_Row(statement_list[current_row].second_segment)].expression;
            return statement_list[current_row].expression==first+"^"+second||
                statement_list[current_row].expression == first + "∧" + second ||
                statement_list[current_row].expression == second + "^" + first ||
                statement_list[current_row].expression == second + "∧" + first;
        }
        private bool And_Elimination_One(List<Statement> statement_list, int current_row) {
            string original_expression= statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            return original_expression.Contains("^" + statement_list[current_row].expression)||
            original_expression.Contains("∧" + statement_list[current_row].expression);
        }
        private bool And_Elimination_Two(List<Statement> statement_list, int current_row)
        {
            string original_expression = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            return statement_list[Get_Row(statement_list[current_row].first_segment)].expression.Contains(statement_list[current_row].expression+ "^" ) ||
             statement_list[Get_Row(statement_list[current_row].first_segment)].expression.Contains(statement_list[current_row].expression+ "∧");
        }
        private bool Or_Elimination(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool Or_Introduction_One(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool Or_Introduction_Two(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool Arrow_Introduction(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool Not_Not_Elimination(List<Statement> statement_list, int current_row) {
            string original_expression = statement_list[Get_Row(statement_list[current_row].first_segment)].expression;
            return original_expression.Contains("¬¬" + statement_list[current_row].expression) 
                || original_expression.Contains("¬¬(" + statement_list[current_row].expression + ")")
                || original_expression.Contains("~~" + statement_list[current_row].expression)
                || original_expression.Contains("~~(" + statement_list[current_row].expression + ")");
        }
        private bool Not_Elimination(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }
        private bool Not_Introduction(List<Statement> statement_list, int current_row) { throw new NotImplementedException(); }

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
    }
}
