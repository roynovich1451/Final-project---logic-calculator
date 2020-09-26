using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace LogicCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<string> calculations;
        private readonly HashSet<string> literals;
        private readonly HashSet<string> variables;
        List<Statement> statement_list = new List<Statement>();

        public MainWindow()
        {
            InitializeComponent();
            literals = new HashSet<string>();
            variables = new HashSet<string>();
            calculations = new ObservableCollection<string>();
            //Results.ItemsSource = calculations;
            //Test();
        }

        void HandleTableInput()
        {
            UIElementCollection children = GridUserSol.Children;
            for (int i = 0; i < children.Count; i += 4)
            {
                if (!(children[i] is TextBox))
                    continue;
                TextBox expression = children[i] as TextBox;
                TextBox start_line = children[i + 1] as TextBox;
                TextBox end_line = children[i + 2] as TextBox;
                TextBox rule = children[i + 3] as TextBox;

                if (IsValidStatement(expression, start_line, end_line, rule, i/4))
                {
                    int start = Int32.Parse(start_line.Text.Trim()), end = Int32.Parse(end_line.Text.Trim());
                    Statement s = new Statement(expression.Text, rule.Text, start, end);
                    statement_list.Add(s);
                }
                else
                {
                    return;
                }

                if (rule.Text.Contains("^") || rule.Text.Contains("∧") || rule.Text.Contains("&"))
                {
                    Evaluation e = new Evaluation(statement_list, i, "and");
                }
                //return c == '^' || c == '>' || c == 'v' || c == '&' || c == '|' || c == '¬' || c == '~' ||
                //       c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' || c == '⊥';
            }

            return;
        }

        #region button_clicks
        private void CheckButton_click(object sender, RoutedEventArgs e)
        {
            HandleTableInput();
        }
       

        private void NotButton_click(object sender, RoutedEventArgs e)
        {
            tbValue.Text = tbValue.Text + '¬';
        }
        private void SaveButton_click(object sender, RoutedEventArgs e)
        {
            string fileName = "wow";//TODO: get this from somewhere
            string path = "C:\\Oren\\LogicCalculator";//TODO: get this from somewhere
            //Check if the name has invalid chars
            fileName = fileName.Trim();
            path = path.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("File name is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("File name can not contain < > : \" / \\ | ? *", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //TODO add overwrite option
            if (File.Exists(Path.Combine(path, fileName)))
            {
                MessageBox.Show("File exists already", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var document = DocX.Create(@path + "\\" + fileName))
            {
                // Add a title.
                document.InsertParagraph("Logic Calculator Results").FontSize(16d).Bold(true).UnderlineStyle(UnderlineStyle.singleLine);

                //Add input
                document.InsertParagraph(tbValue.Text).FontSize(14d);

                // Save this document to disk.
                document.Save();
                Console.WriteLine("\tCreated Document: ApplyTemplate.docx\n");
            }
        }

        public void Calculate(string input)
        {
            variables.Add(input);
            Console.WriteLine("input: " + input);
            if (input.Length == 0)
                return;
            Console.WriteLine("input length: " + input.Length);
            int end_of_first, end_of_second, op_index;
            for (int i = 0; i < input.Length; i++)
            {
                Console.WriteLine("i: " + i + " input[i]: " + input[i]);

                if (IsOperator(input[i]))
                {
                    Console.WriteLine("skip");
                    Console.WriteLine();
                    continue;
                }

                if (input[i] == '(')
                {
                    op_index = FindOperator(input, i);
                    end_of_first = FindLiteralEnd(input, op_index + 1);
                    Console.WriteLine();
                    Console.WriteLine("op_index: " + op_index + " end: " + end_of_first);
                    if (input[op_index + 1] != '(')
                    {
                        Handle_Literals(input.Substring(i + 1), end_of_first - i - 1);
                    }
                    else
                    {
                        Calculate(input.Substring(op_index + 1));
                    }
                    i = input.IndexOf(')') + 2;
                    if (i == '(')
                    {

                    }
                    else
                    {
                        end_of_second = FindLiteralEnd(input, i);
                        literals.Add(input.Substring(i, end_of_second - i));
                        i = end_of_second;
                    }
                }
                else
                {
                    i = Handle_Literals(input, i) + 1;
                }
            }
        }
        private int Handle_Literals(string input, int i)
        {
            int end_of_first, end_of_second;
            end_of_first = FindLiteralEnd(input, i);
            if (end_of_first >= input.Length - 1)
                return input.Length;
            Console.WriteLine("i " + i);
            Console.WriteLine("end_of_first " + end_of_first);
            Console.WriteLine("\nadding to literals:" + input.Substring(i, end_of_first - i));
            literals.Add(input.Substring(i, end_of_first - i));
            if (i > input.Length - 2)
                return input.Length;
            if (input[end_of_first] == '(')
                end_of_second = input.IndexOf(')', end_of_first + 1);
            else
                end_of_second = FindLiteralEnd(input, end_of_first + 1);
            if (end_of_second == -1)
                end_of_second = input.Length - 1;
            Console.WriteLine("end_of_second " + end_of_second);
            Console.WriteLine();
            Console.WriteLine("\nadding to literals:" + input.Substring(end_of_first + 1, end_of_second - end_of_first - 1));
            literals.Add(input.Substring(end_of_first + 1, end_of_second - end_of_first - 1));

            Console.WriteLine("adding to variables:" + input.Substring(i, end_of_second - i));
            variables.Add(input.Substring(i, end_of_second - i));

            Console.WriteLine();

            i = end_of_first;
            return i;
        }

        #endregion

        #region utility
        private int FindLiteralEnd(string input, int start)
        {
            for (int i = start; i < input.Length; i++)
            {
                if (!Char.IsLetter(input[i]))
                {
                    return i;
                }
            }
            return input.Length;
        }

        public int FindOperator(string input, int start)
        {
            for (int i = start; i < input.Length; i++)
            {
                if (IsOperator(input[i]))
                {
                    return i;
                }
            }
            return -1;
        }
        #endregion

        #region input_check

        private bool IsValidStatement(TextBox expression, TextBox start_line, TextBox end_line, TextBox rule, int row)
        {
            int start, end;
            if (!IsValidExpression(expression.Text, row))
            {
                return false;
            }
            if (string.IsNullOrEmpty(start_line.Text))
            {
                Expression_Error(row, "Start Line is empty");
            }
            if (string.IsNullOrEmpty(end_line.Text))
            {
                Expression_Error(row, "End Line is empty");
            }
            if (!Int32.TryParse(start_line.Text.Trim(), out start))
            {
                Expression_Error(row, "Start Line is not a positive integer number");
                return false;
            }
            if (!Int32.TryParse(end_line.Text.Trim(), out end))
            {
                Expression_Error(row, "End Line is not a positive integer number");
                return false;
            }
            if (start > statement_list.Count || start < 0)
            {
                Expression_Error(row, "Start Line entered is not in the range of the table size");
                return false;
            }
            if (end > statement_list.Count || end < 0)
            {
                Expression_Error(row, "End Line entered is not in the range of the table size");
                return false;
            }
            return true;
        }

        private bool IsOperator(char c)
        {
            return c == '^' || c == '>' || c == 'v' || c == '&' || c == '|' || c == '¬' || c == '~' ||
                c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' || c == '⊥';
        }



        public void Expression_Error(int row, string error, int index = -1)
        {
            string error_message = "Error on row: " + row;
            if (index != -1)
            {
                error_message += " index: " + index;
            }
            error_message += "\nError is: " + error;

            MessageBox.Show(error_message, "Expression Check");
        }

        public bool IsValidExpression(string input, int row)
        {
            int parentheses_count = 0;
            bool after_operator = false;
            int i = 0;

            //Check if input is empty
            if (input.Length == 0)
                Expression_Error(row, "No input", i);

            for (; i < input.Length; i++)
            {
                char c = input[i];

                //Ignore spaces
                if (Char.IsWhiteSpace(c))
                    continue;

                if (Char.IsNumber(c))
                {
                    Expression_Error(row, "Entering digits is not allowed, problematic char is:"+c, i);
                    return false;
                }

                //Open parentheses
                else if (c == '(')
                {
                    if (i != 0 && input[i - 1] == ')')
                    {
                        Expression_Error(row, "Missing an operator, problematic char is:" + c, i);
                        return false;
                    }
                    after_operator = true;
                    parentheses_count++;
                }
                //Close parentheses
                else if (c == ')')
                {
                    if (after_operator)
                    {
                        Expression_Error(row, "Two operators in a row, problematic char is:" + c, i);
                        return false;
                    }
                    parentheses_count--;
                    if (parentheses_count < 0)
                    {
                        Expression_Error(row, "Too many closing parentheses, problematic char is:" + c, i);
                        return false;
                    }
                }
                else if (Char.IsLetter(c))
                {
                    if (!after_operator && i != 0)
                        Expression_Error(row, "Two variables in a row, problematic char is:" + c, i);
                    int j = i + 1;
                    for (; j < input.Length; j++)
                    {
                        c = input[j];
                        if (!Char.IsLetter(c))
                            break;
                    }
                    i = j - 1;

                    after_operator = false;
                }
                else if (IsOperator(c))
                {
                    if (after_operator)
                    {
                        Expression_Error(row, "Two operators in a row, problematic char is:" + c, i);
                        return false;
                    }
                    after_operator = true;
                }
                else
                {
                    Expression_Error(row, "An invalid character input, problematic char is:" + c, i);
                    return false;
                }
            }
            if (parentheses_count > 0)
            {
                Expression_Error(row, "Too many opening parentheses", i);
                return false;
            }
            return true;
        }

        #endregion input_check

        #region testing

        public void Test()
        {
            Hide();
            string input = "(a>b)>C";

            input = Regex.Replace(input, @"\s+", "");//remove spaces
            //string a=input.Split('(')[1];

            Console.WriteLine("");
            Console.WriteLine("");
            // IsValidExpression(input);
            Calculate(input);

            Console.WriteLine("");
            Console.WriteLine("Printing Literals");
            PrintStringSet(literals);
            Console.WriteLine("");
            Console.WriteLine("Printing Variables");
            PrintStringSet(variables);
            Console.WriteLine("");

            Environment.Exit(0);
        }

        public void PrintStringSet(HashSet<string> set)
        {
            Console.WriteLine("Size of set: " + set.Count);
            foreach (string s in set)
            {
                Console.WriteLine(s);
            }
        }

        public void PrintObservable(ObservableCollection<string> oc)
        {
            Console.WriteLine("Size of Observable Collection: " + oc.Count);
            foreach (string s in oc)
            {
                Console.WriteLine(s.ToString());
            }
        }

        #endregion testing

    }
}