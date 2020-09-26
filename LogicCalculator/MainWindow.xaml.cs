using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        private int nextLine = 1;
        private List<string> rules = new List<string> { "data", "∧i", "∧e", "¬¬e", "¬¬i", "→e",
                                                        "→i", "MP", "MT", "Copy", "Assumption" };

        public MainWindow()
        {
            InitializeComponent();
            List<TextBox> text_boxes_list = GetAllTextBoxes();
            PrintTextBoxList(text_boxes_list);
            createLine();
        }
        #region dynamic_gui
        private void createLine()
        {
            //create new line and define cols
            Grid newLine = new Grid();
            ColumnDefinition gridCol1 = new ColumnDefinition();
            gridCol1.Width = new GridLength(40);
            ColumnDefinition gridCol2 = new ColumnDefinition();
            gridCol2.Width = new GridLength(160);
            ColumnDefinition gridCol3 = new ColumnDefinition();
            gridCol3.Width = new GridLength(60);
            ColumnDefinition gridCol4 = new ColumnDefinition();
            gridCol4.Width = new GridLength(60);
            ColumnDefinition gridCol5 = new ColumnDefinition();
            gridCol5.Width = new GridLength(60);
            ColumnDefinition gridCol6 = new ColumnDefinition();
            gridCol6.Width = new GridLength(60);

            // ADD col to new line
            newLine.ColumnDefinitions.Add(gridCol1);
            newLine.ColumnDefinitions.Add(gridCol2);
            newLine.ColumnDefinitions.Add(gridCol3);
            newLine.ColumnDefinitions.Add(gridCol4);
            newLine.ColumnDefinitions.Add(gridCol5);
            newLine.ColumnDefinitions.Add(gridCol6);

            //label
            Label lb = new Label();
            lb.Content = $"{nextLine}";
            lb.Margin = new Thickness(2);
            lb.HorizontalContentAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(lb, 0);

            //textblock - statement
            TextBox tbState = new TextBox();
            tbState.HorizontalAlignment = HorizontalAlignment.Center;
            tbState.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tbState.Name = $"tbStatement{nextLine}";
            tbState.Margin = new Thickness(2);
            tbState.Width = 150;
            Grid.SetColumn(tbState, 1);

            //comboBox - rule
            ComboBox cmbRules = new ComboBox();
            cmbRules.ItemsSource = rules;
            cmbRules.HorizontalAlignment = HorizontalAlignment.Center;
            cmbRules.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            cmbRules.Name = $"cmbRules{nextLine}";
            cmbRules.Margin = new Thickness(2);
            cmbRules.Width = 50;
            Grid.SetColumn(cmbRules, 2);

            //textblock - first segment
            TextBox tbFirstSeg = new TextBox();
            tbFirstSeg.HorizontalAlignment = HorizontalAlignment.Center;
            tbFirstSeg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tbFirstSeg.Name = $"tbFirstSeg{nextLine}";
            tbFirstSeg.Margin = new Thickness(2);
            tbFirstSeg.Width = 50;
            Grid.SetColumn(tbFirstSeg, 3);

            //textblock - second segment
            TextBox tbSecondSeg = new TextBox();
            tbSecondSeg.HorizontalAlignment = HorizontalAlignment.Center;
            tbSecondSeg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tbSecondSeg.Name = $"tbSecondSeg{nextLine}";
            tbSecondSeg.Margin = new Thickness(2);
            tbSecondSeg.Width = 50;
            Grid.SetColumn(tbSecondSeg, 4);

            //textblock - third segment
            TextBox tbThirdSeg = new TextBox();
            tbThirdSeg.HorizontalAlignment = HorizontalAlignment.Center;
            tbThirdSeg.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            tbThirdSeg.Name = $"tbThirdSeg{nextLine}";
            tbThirdSeg.Margin = new Thickness(2);
            tbThirdSeg.Width = 50;
            Grid.SetColumn(tbThirdSeg, 5);

            //add children to new line
            newLine.Children.Add(lb);
            newLine.Children.Add(tbState);
            newLine.Children.Add(cmbRules);
            newLine.Children.Add(tbFirstSeg);
            newLine.Children.Add(tbSecondSeg);
            newLine.Children.Add(tbThirdSeg);

            //add new line to StackPanel
            spGridTable.Children.Add(newLine);
            nextLine++;
        }
        #endregion

       

        void HandleTableInput()
        {
            List<TextBox> text_boxes_list = GetAllTextBoxes();
            PrintTextBoxList(text_boxes_list);
            for (int i = 0; i < text_boxes_list.Count - 4; i += 4)
            {
                TextBox expression = text_boxes_list[i] as TextBox;
                TextBox start_line = text_boxes_list[i + 1] as TextBox;
                TextBox end_line = text_boxes_list[i + 2] as TextBox;
                TextBox rule = text_boxes_list[i + 3] as TextBox;
                int current_row = i / 4 + 1;

                if (IsValidStatement(expression, start_line, end_line, rule, current_row))
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
                    Evaluation e = new Evaluation(statement_list, current_row, "and");
                }
                //return c == '^' || c == '>' || c == 'v' || c == '&' || c == '|' || c == '¬' || c == '~' ||
                //       c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' || c == '⊥';
            }

            return;
        }

        #region button_clicks

        private void btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            createLine();
        }

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

        public List<TextBox> GetAllTextBoxes()
        {
            UIElementCollection children = GridMain.Children;
            List<TextBox> ret = new List<TextBox>();
            foreach (var child in children)
            {
                if (child is TextBox)
                    ret.Add(child as TextBox);
            }
            return ret;
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
                return false;
            }
            if (string.IsNullOrEmpty(end_line.Text))
            {
                Expression_Error(row, "End Line is empty");
                return false;
            }
            if (string.IsNullOrEmpty(rule.Text))
            {
                Expression_Error(row, "Rule is empty");
                return false;
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
                    Expression_Error(row, "Entering digits is not allowed, problematic char is:" + c, i);
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
                    {
                        Expression_Error(row, "Two variables in a row, problematic char is:" + c, i);
                        return false;
                    }
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


        public void PrintTextBoxList(List<TextBox> l)
        {
            foreach (TextBox t in l)
            {
                Console.WriteLine(t.Text);
            }
        }

        #endregion testing

    }
}