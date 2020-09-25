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

        public MainWindow()
        {
            InitializeComponent();
            literals = new HashSet<string>();
            variables = new HashSet<string>();
            calculations = new ObservableCollection<string>();
            //Results.ItemsSource = calculations;
            //Test();
        }

        private void CheckButton_click(object sender, RoutedEventArgs e)
        {
            int tableSize = 4;
            for (int i = 0; i < tableSize; i++)
            {
                string input = tbValue.Text.Trim();
                input = Regex.Replace(input, @"\s+", "");//remove spaces
                if (IsValidExpression(input))
                {
                    MessageBox.Show("Correct", "Expression Check");
                }
            }
        }


        private List<string> HandleTableInput()
        {
            List<string> statementList = new List<string>();
            UIElementCollection children = GridUserSol.Children;
            for (int i = 0; i < children.Count; i++)
            {
                if (!(children[i] is TextBox))
                    continue;
                TextBox statement = children[i] as TextBox;
                TextBox startLine = children[i + 1] as TextBox;
                TextBox endLine = children[i + 2] as TextBox;
                TextBox extra = children[i + 3] as TextBox;

                int start, end;
                IsValidExpression(statement.Text);
                if (!Int32.TryParse(startLine.Text.Trim(),out start))
                {
                    string error_message = "Error at row: " + i + "\nError: Start Line is not an integer number";
                    MessageBox.Show(error_message, "Start Line Check");
                }

                if (!Int32.TryParse(startLine.Text.Trim(), out end))
                {
                    string error_message = "Error at row: " + i + "\nError: End Line is not an integer number";
                    MessageBox.Show(error_message, "End Line Check");
                }

                if (extra.Text.Contains("^") || extra.Text.Contains("∧") || extra.Text.Contains("&"))
                    {
                        Evaluation e = new Evaluation(statement.Text, '^', Int32.Parse(startLine.Text), Int32.Parse(endLine.Text));
                    }
                //return c == '^' || c == '>' || c == 'v' || c == '&' || c == '|' || c == '¬' || c == '~' ||
                //       c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' || c == '⊥';
            }
            return statementList;
        }


        private void CalculateButton_click(object sender, RoutedEventArgs e)
        {
            try
            {
                //TODO:TableWindow statement = new TableWindow();
                //statement.Show();
                string input = tbValue.Text.Trim();
                input = Regex.Replace(input, @"\s+", "");//remove spaces
                IsValidExpression(input);
                Calculate(input);
                //calculations.Add(input);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private int FindOperator(string input, int start)
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

        private bool IsOperator(char c)
        {
            return c == '^' || c == '>' || c == 'v' || c == '&' || c == '|' || c == '¬' || c == '~' ||
                c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' || c == '⊥';
        }

        public void Error(int index, string error)
        {
            string error_message = "Error at index: " + index + "\nError: " + error;
            MessageBox.Show(error_message, "Expression Check");
        }

        public bool IsValidExpression(string input)
        {
            int parentheses_count = 0;
            bool after_operator = false;
            int i = 0;

            //Check if input is empty
            if (input.Length == 0)
                Error(i, "No input");

            for (; i < input.Length; i++)
            {
                char c = input[i];

                //Ignore spaces
                if (Char.IsWhiteSpace(c))
                    continue;

                if (Char.IsNumber(c))
                {
                    Error(i, "Entering digits is not allowed");
                    return false;
                }

                //Open parentheses
                else if (c == '(')
                {
                    if (i != 0 && input[i - 1] == ')')
                    {
                        Error(i, "Missing an operator");
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
                        Error(i, "Two operators in a row");
                        return false;
                    }
                    parentheses_count--;
                    if (parentheses_count < 0)
                    {
                        Error(i, "Too many closing parentheses");
                        return false;
                    }
                }
                else if (Char.IsLetter(c))
                {
                    if (!after_operator && i != 0)
                        Error(i, "Two variables in a row");
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
                        Error(i, "Two operators in a row");
                        return false;
                    }
                    after_operator = true;
                }
                else
                {
                    Error(i, "An invalid character input");
                    return false;
                }
            }
            if (parentheses_count > 0)
            {
                Error(i, "Too many opening parentheses");
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
            IsValidExpression(input);
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