using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        readonly List<Statement> statement_list = new List<Statement>();
        private int table_row_num = 0;

        private static readonly int TABLE_COL_NUM = 5;
        TextBox elementWithFocus;
        private readonly List<string> rules = new List<string> { "data", "∧i", "∧e", "¬¬e", "¬¬i", "→e",
                                                        "→i", "MP", "MT", "Copy", "Assumption" };

        public MainWindow()
        {
            InitializeComponent();
            CreateLine();
        }

        #region MenuBar_Click
        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            tbValue.Text = String.Empty;
            UIElementCollection grids = spGridTable.Children;
            foreach (var row in grids)
            {
                Grid g = row as Grid;
                foreach (var child in g.Children)
                {
                    if (child is TextBox)
                    {
                        ((TextBox)child).Text = String.Empty;
                    }
                    if (child is ComboBox)
                    {
                        ((ComboBox)child).Text = String.Empty;
                    }
                }
            }
        }
        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            string path = "";
            using (var document2 = DocX.Load(path + @"Second.docx"))
            {

            }

        }
        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "wow";//TODO: get this from somewhere
            string path = "C:\\Oren\\Final_project_logic_calculator";//TODO: get this from somewhere

            //If the folder does not exist yet, it will be created.
            //If the folder exists already, the line will be ignored.
            System.IO.Directory.CreateDirectory(path);

            if (!CheckPathAndFilename(path, fileName))
            {
                return;
            }

            using (var document = DocX.Create(@path + "\\" + fileName))
            {
                // Add a title.
                document.InsertParagraph("Logic Calculator Results\n").FontSize(16d).Bold(true).UnderlineStyle(UnderlineStyle.singleLine);
                //Add the main expression
                document.InsertParagraph("Logical Expression: "+tbValue.Text+'\n').FontSize(14d);

                //Add the proof table
                Table proof_table = document.AddTable(table_row_num, TABLE_COL_NUM);
                proof_table.Alignment=Alignment.center;
                List<String> input_list = GetAllTableInput();
                PrintInputList(input_list);
                Console.WriteLine("count " + input_list.Count);
                Console.WriteLine("");

                //Fill the proof table
                for (int i = 0; i < table_row_num; i++)
                {
                    for (int j = 0; j < TABLE_COL_NUM; j++)
                    {
                        proof_table.Rows[i].Cells[j].Paragraphs.First().Append(input_list[i * TABLE_COL_NUM + j]);
                        Console.WriteLine("input_list[i * TABLE_COL_NUM + j]"+ input_list[i * TABLE_COL_NUM + j]);
                    }
                }
                document.InsertTable(proof_table);


                // Save this document to disk.
                try
                {
                    document.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                MessageBox.Show("Created Document: " + path + "\\" + fileName + ".docx","Documented Created");
            }
        }
        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItemManual_Click(object sender, RoutedEventArgs e)
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.FullName;
            Console.WriteLine(projectDirectory);
            try
            {
                Process.Start(projectDirectory + "\\User_Manual.docx", "-p");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        #endregion

        #region dynamic_gui
        private void CreateLine()
        {
            //create new line and define cols
            Grid newLine = new Grid();
            ColumnDefinition gridCol1 = new ColumnDefinition
            {
                Width = new GridLength(40)
            };
            ColumnDefinition gridCol2 = new ColumnDefinition
            {
                Width = new GridLength(160)
            };
            ColumnDefinition gridCol3 = new ColumnDefinition
            {
                Width = new GridLength(60)
            };
            ColumnDefinition gridCol4 = new ColumnDefinition
            {
                Width = new GridLength(60)
            };
            ColumnDefinition gridCol5 = new ColumnDefinition
            {
                Width = new GridLength(60)
            };
            ColumnDefinition gridCol6 = new ColumnDefinition
            {
                Width = new GridLength(60)
            };

            // ADD col to new line
            newLine.ColumnDefinitions.Add(gridCol1);
            newLine.ColumnDefinitions.Add(gridCol2);
            newLine.ColumnDefinitions.Add(gridCol3);
            newLine.ColumnDefinitions.Add(gridCol4);
            newLine.ColumnDefinitions.Add(gridCol5);
            newLine.ColumnDefinitions.Add(gridCol6);

            //label
            Label lb = new Label
            {
                Content = $"{table_row_num}",
                Margin = new Thickness(2),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lb, 0);

            //textblock - statement
            TextBox tbState = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbStatement{table_row_num}",
                Margin = new Thickness(2),
                Width = 150
            };
            Grid.SetColumn(tbState, 1);

            //comboBox - rule
            ComboBox cmbRules = new ComboBox
            {
                ItemsSource = rules,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"cmbRules{table_row_num}",
                Margin = new Thickness(2),
                Width = 50
            };
            Grid.SetColumn(cmbRules, 2);

            //textblock - first segment
            TextBox tbFirstSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbFirstSeg{table_row_num}",
                Margin = new Thickness(2),
                Width = 50
            };
            Grid.SetColumn(tbFirstSeg, 3);

            //textblock - second segment
            TextBox tbSecondSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbSecondSeg{table_row_num}",
                Margin = new Thickness(2),
                Width = 50
            };
            Grid.SetColumn(tbSecondSeg, 4);

            //textblock - third segment
            TextBox tbThirdSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbThirdSeg{table_row_num}",
                Margin = new Thickness(2),
                Width = 50
            };
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
            table_row_num++;
        }
        #endregion

        /*   void HandleTableInput()
           {
               List<TextBox> text_boxes_list = GetAllTableInput();
               PrintInputList(text_boxes_list);
               for (int i = 0; i < text_boxes_list.Count - 4; i += 5)
               {
                   TextBox expression = text_boxes_list[i] as TextBox;
                   TextBox rule = text_boxes_list[i + 1] as TextBox;
                   TextBox first_segment = text_boxes_list[i + 2] as TextBox;
                   TextBox second_segment = text_boxes_list[i + 3] as TextBox;
                   TextBox third_segment = text_boxes_list[i + 4] as TextBox;
                   int current_row = i / 5 + 1;

                   if (!second_segment.IsEnabled)
                   {

                   }

                   if (IsValidStatement(expression, rule, first_segment, second_segment, third_segment, current_row))
                   {
                       /* int start = Int32.Parse(start_line.Text.Trim()), end = Int32.Parse(end_line.Text.Trim());
                        Statement s = new Statement(expression.Text, rule.Text, start, end);
                        statement_list.Add(s);
                   }
                   else
                   {
                       return;
                   }

                   if (rule.Text.Contains("^") || rule.Text.Contains("∧") || rule.Text.Contains("&"))
                   {
                       new Evaluation(statement_list, current_row, "and");
                   }
                   //return c == '^' || c == '>' || c == 'v' || c == '&' || c == '|' || c == '¬' || c == '~' ||
                   //       c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' || c == '⊥';
               }

               return;
           }
   */
        #region button_clicks

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            CreateLine();
        }

        private void CheckButton_click(object sender, RoutedEventArgs e)
        {
            //HandleTableInput();
        }

        private void BtnOr_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "∨");
            }
        }
        private void BtnAnd_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "∧");
            }
        }
        private void Btnbothdirections_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "↔️");
            }
        }
        private void BtnTurnstile_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "⊢");
            }
        }
        private void BtnFalsum_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "⊥");
            }
        }
        private void BtnNot_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "¬");
            }
        }
        #endregion

        #region utility 
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

        public List<String> GetAllTableInput()
        {
            UIElementCollection grids = spGridTable.Children;
            List<String> ret = new List<String>();
            foreach (var row in grids)
            {
                Grid g = row as Grid;
                foreach (var child in g.Children)
                {
                    if (child is TextBox)
                    {
                        ret.Add(((TextBox)child).Text);
                    }
                    if (child is ComboBox)
                    {
                        ret.Add(((ComboBox)child).Text);
                    }
                }
            }
            return ret;
        }

        #endregion

        #region Keyboard_func
        private void AppendKeyboardChar(TextBox tb, string sign)
        {
            tb.Text += sign;
        }
        private void SpKeyboard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox temp)
                elementWithFocus = temp;
        }
        #endregion

        #region input_check

        private bool IsValidStatement(TextBox expression, TextBox rule, TextBox first_segment,
           TextBox second_segment, TextBox third_segment, int row)
        {
            if (!IsValidExpression(expression.Text, row))
            {
                return false;
            }
            if (string.IsNullOrEmpty(first_segment.Text))
            {
                Expression_Error(row, "Start Line is empty");
                return false;
            }

            if (string.IsNullOrEmpty(rule.Text))
            {
                Expression_Error(row, "Rule is empty");
                return false;
            }
            if (!Int32.TryParse(first_segment.Text.Trim(), out int start))
            {
                Expression_Error(row, "first_segment is not a positive integer number");
                return false;
            }
            if (!Int32.TryParse(second_segment.Text.Trim(), out int end))
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

        public bool IsValidSegment(string seg)
        {
            int index = seg.IndexOf('-');
            if (index == -1)
            {
                return Int32.TryParse(seg, out _);
            }
            return Int32.TryParse(seg.Substring(0, index), out _) && Int32.TryParse(seg.Substring(index + 1, seg.Length - index), out _);

        }

        public bool CheckPathAndFilename(string path, string fileName)
        {
            //Check if the name has invalid chars
            fileName = fileName.Trim();
            path = path.Trim();
            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("File name is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("File name can not contain < > : \" / \\ | ? *", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            //TODO add overwrite option
            if (File.Exists(Path.Combine(path, fileName)))
            {
                MessageBox.Show("File exists already", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        #endregion input_check

        #region testing
        public void PrintInputList(List<String> l)
        {
            foreach (String t in l)
            {
                Console.WriteLine(t);
            }
        }

        #endregion testing

    }
}