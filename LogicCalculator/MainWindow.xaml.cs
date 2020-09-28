using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    public partial class MainWindow : Window
    {
        #region DEFINES
        const int COL_LABEL_WIDTH = 40;
        const int COL_STATEMENT_WIDTH = 160;
        const int COL_SEGMENT_WIDTH = 60;
        const int CHILD_MARGIN = 4;
        const int THICKNESS = 2;
        const int SPACES = 5;
        const int HYPHEN = 10;
        const int MAX_HYPHEN_CHUNKS = 8;
        const int MIN_HYPHEN_CHUNKS = 1;
        enum BoxState
        {
            Open,
            Close
        }
        #endregion

        #region VARIABLES
        readonly List<Statement> statement_list = new List<Statement>();
        private int table_row_num = 0;
        private static readonly int TABLE_COL_NUM = 6;
        TextBox elementWithFocus;
        private readonly List<string> rules = new List<string> { "Data", "Assumption", "LEM", "PBC", "MP", "MT", "Copy"
                                                                 ,"∧i", "∧e1", "∧e2", "∨i1", "∨i2", "∨e", "¬¬e",
                                                                 "¬¬i", "→i", "⊥e", "¬i", "→i"};
        private int hyphen_chunks = MAX_HYPHEN_CHUNKS;
        private int spaces_chunks = MIN_HYPHEN_CHUNKS;
        private int box_closers = 0;
        private int box_openers = 0;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            CreateLine();
        }

        #region MENUBAR_CLICKS
        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            spGridTable.Children.Clear();
            table_row_num = 0;
            CreateLine();
        }
        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            string path = "C:\\Oren\\Final_project_logic_calculator", file_name = "wow.docx";
            CheckPathAndFileName(path, file_name);

            using (var document = DocX.Load(path + "\\" + file_name))
            {
                //Clear Table
                spGridTable.Children.Clear();
                table_row_num = 0;

                Table proof_table = document.Tables[0];
   
                for (int i = 1; i < proof_table.Rows.Count; i++)
                {
                    CreateLine();
                }
                UIElementCollection grids = spGridTable.Children;

                for (int i = 0; i < proof_table.Rows.Count - 1; i++)
                {
                    Grid g = grids[i] as Grid;
                    for (int j = 1; j < TABLE_COL_NUM; j++)
                    {
                        if (g.Children[j] is ComboBox)
                        {
                            ((ComboBox)g.Children[j]).SelectedItem = proof_table.Rows[i + 1].Cells[j].Paragraphs[0].Text;
                        }
                        if (g.Children[j] is TextBox)
                        {
                            ((TextBox)g.Children[j]).Text = proof_table.Rows[i + 1].Cells[j].Paragraphs[0].Text;
                        }
                    }
                }
            }
        }
        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            string file_name = "wow";//TODO: get this from somewhere
            string path = "C:\\Oren\\Final_project_logic_calculator";//TODO: get this from somewhere

            //If the folder does not exist yet, it will be created.
            //If the folder exists already, the line will be ignored.
            System.IO.Directory.CreateDirectory(path);

            if (!CheckPathAndFileName(path, file_name))
            {
                return;
            }

            using (var document = DocX.Create(@path + "\\" + file_name))
            {
                // Add a title.
                document.InsertParagraph("Logic Calculator Results\n").FontSize(16d).Bold(true).UnderlineStyle(UnderlineStyle.singleLine);
                //Add the main expression
                document.InsertParagraph("Logical Expression: " + tbValue.Text + '\n').FontSize(14d);

                //Add the proof table
                Table proof_table = document.AddTable(table_row_num + 1, TABLE_COL_NUM);
                proof_table.Alignment = Alignment.center;

                proof_table.Rows[0].Cells[0].Paragraphs.First().Append("Line").UnderlineStyle(UnderlineStyle.singleLine);
                proof_table.Rows[0].Cells[1].Paragraphs.First().Append("Expression").UnderlineStyle(UnderlineStyle.singleLine);
                proof_table.Rows[0].Cells[2].Paragraphs.First().Append("Rule").UnderlineStyle(UnderlineStyle.singleLine);
                proof_table.Rows[0].Cells[3].Paragraphs.First().Append("First Segment").UnderlineStyle(UnderlineStyle.singleLine);
                proof_table.Rows[0].Cells[4].Paragraphs.First().Append("Second Segment").UnderlineStyle(UnderlineStyle.singleLine);
                proof_table.Rows[0].Cells[5].Paragraphs.First().Append("Third Segment").UnderlineStyle(UnderlineStyle.singleLine);

                List<String> input_list = GetAllTableInput();
                //Fill the proof table

                for (int i = 0; i < table_row_num; i++)
                {
                    proof_table.Rows[i + 1].Cells[0].Paragraphs.First().Append((i + 1).ToString());
                    for (int j = 0; j < TABLE_COL_NUM - 1; j++)
                    {
                        proof_table.Rows[i + 1].Cells[j + 1].Paragraphs.First().Append(input_list[i * (TABLE_COL_NUM - 1) + j]);
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
                MessageBox.Show("Created Document: " + path + "\\" + file_name + ".docx", "Documented Created");
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

        #region DYNAMIC_GUI
        private string createBoxLine(BoxState state)
        {
            StringBuilder line = new StringBuilder();

            if (state == BoxState.Open)
            {
                line.Append(' ', SPACES * spaces_chunks);
                line.Append('-', HYPHEN * hyphen_chunks);
                line[SPACES * spaces_chunks] = '┌';
                line[line.Length - 1] = '┐';
                if (hyphen_chunks > MIN_HYPHEN_CHUNKS)
                {
                    ++spaces_chunks;
                    --hyphen_chunks;
                }
            }
            else
            {
                if (hyphen_chunks < MAX_HYPHEN_CHUNKS)
                {
                    --spaces_chunks;
                    ++hyphen_chunks;
                }
                line.Append(' ', SPACES * spaces_chunks);
                line.Append('-', HYPHEN * hyphen_chunks);
                line[SPACES * spaces_chunks] = '└';
                line[line.Length - 1] = '┘';
            }
            return line.ToString();
        }

        private TextBlock createBox(BoxState state)
        {
            //textblock - opener
            TextBlock tbline = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(2),
                FontWeight = FontWeights.Bold,
                Width = 420
            };
            tbline.Text = createBoxLine(state);
            return tbline;
        }

        private bool removeTextBlock(int location, BoxState state)
        {
            if (location < 0 || location > spGridTable.Children.Count - 1) return false;
            if (spGridTable.Children[location] is TextBlock)
            {
                TextBlock tb = spGridTable.Children[location] as TextBlock;
                if (state == BoxState.Open && tb.Text.Contains('┌'))
                {
                    spGridTable.Children.RemoveAt(location);
                    --box_openers;
                    if (hyphen_chunks < MAX_HYPHEN_CHUNKS)
                    {
                        --spaces_chunks;
                        ++hyphen_chunks;
                    }
                    return true;
                }
                if (state == BoxState.Close && tb.Text.Contains('└'))
                {
                    spGridTable.Children.RemoveAt(location);
                    --box_closers;
                    if (hyphen_chunks > MIN_HYPHEN_CHUNKS)
                    {
                        ++spaces_chunks;
                        --hyphen_chunks;
                    }
                    return true;
                }
            }
            return false;
        }

        private void handleBox(ComboBox cmb, int location)
        {
            var rule = cmb.SelectedItem as string;
            switch (rule)
            {
                case "Assumption":
                    removeTextBlock(location + 1, BoxState.Close);
                    spGridTable.Children.Insert(location, createBox(BoxState.Open));
                    ++box_openers;
                    break;
                case "→i":
                    if (removeTextBlock(location - 1, BoxState.Open)) --location;
                    if (!ruleSelectionChecker(cmb, rule)) return;
                    spGridTable.Children.Insert(location + 1, createBox(BoxState.Close));
                    ++box_closers;
                    break;
                default:
                    removeTextBlock(location + 1, BoxState.Close);
                    removeTextBlock(location - 1, BoxState.Open);
                    break;

            }
        }

        private void CreateLine()
        {
            ++table_row_num;
            //create new line and define cols
            Grid newLine = new Grid();
            ColumnDefinition gridCol1 = new ColumnDefinition
            {
                Width = new GridLength(COL_LABEL_WIDTH)
            };
            ColumnDefinition gridCol2 = new ColumnDefinition
            {
                Width = new GridLength(COL_STATEMENT_WIDTH)
            };
            ColumnDefinition gridCol3 = new ColumnDefinition
            {
                Width = new GridLength(COL_SEGMENT_WIDTH)
            };
            ColumnDefinition gridCol4 = new ColumnDefinition
            {
                Width = new GridLength(COL_SEGMENT_WIDTH)
            };
            ColumnDefinition gridCol5 = new ColumnDefinition
            {
                Width = new GridLength(COL_SEGMENT_WIDTH)
            };
            ColumnDefinition gridCol6 = new ColumnDefinition
            {
                Width = new GridLength(COL_SEGMENT_WIDTH)
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
                Margin = new Thickness(THICKNESS),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lb, 0);

            //textblock - statement
            TextBox tbState = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbStatement{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_STATEMENT_WIDTH - CHILD_MARGIN
            };
            Grid.SetColumn(tbState, 1);

            //comboBox - rule
            ComboBox cmbRules = new ComboBox
            {
                ItemsSource = rules,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"cmbRules{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN
            };
            cmbRules.SelectionChanged += new SelectionChangedEventHandler(cmb_SelectedValueChanged);
            Grid.SetColumn(cmbRules, 2);

            //textblock - first segment
            TextBox tbFirstSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbFirstSeg{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN
            };
            Grid.SetColumn(tbFirstSeg, 3);

            //textblock - second segment
            TextBox tbSecondSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbSecondSeg{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN
            };
            Grid.SetColumn(tbSecondSeg, 4);

            //textblock - third segment
            TextBox tbThirdSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbThirdSeg{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN
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
        }
        #endregion

        #region EVENTS
        private void makeInvisible(Grid g, int needed)
        {
            foreach (UIElement child in g.Children)
            {
                switch (needed)
                {
                    case 0:
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == 1)
                            {
                                TextBox tbc = child as TextBox;
                                tbc.IsEnabled = true;
                                tbc.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                TextBox tbc = child as TextBox;
                                tbc.IsEnabled = false;
                                tbc.Visibility = Visibility.Hidden;
                                tbc.Text = "";
                            }
                        }
                        break;
                    case 1:
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == 1 ||
                                Grid.GetColumn(child) == 3)
                            {
                                TextBox tbc = child as TextBox;
                                tbc.IsEnabled = true;
                                tbc.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                TextBox tbc = child as TextBox;
                                tbc.IsEnabled = false;
                                tbc.Visibility = Visibility.Hidden;
                                tbc.Text = "";
                            }
                        }
                        break;
                    case 2:
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == 1 ||
                                Grid.GetColumn(child) == 3 ||
                                Grid.GetColumn(child) == 4)
                            {
                                TextBox tbc = child as TextBox;
                                tbc.IsEnabled = true;
                                tbc.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                TextBox tbc = child as TextBox;
                                tbc.IsEnabled = false;
                                tbc.Visibility = Visibility.Hidden;
                                tbc.Text = "";
                            }
                        }
                        break;
                    case 3:
                        if (child is TextBox)
                        {
                            TextBox tbc = child as TextBox;
                            tbc.IsEnabled = true;
                            tbc.Visibility = Visibility.Visible;
                        }
                        break;
                }
            }
        }
        private void cmb_SelectedValueChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            Grid parent = cmb.Parent as Grid;
            var location = spGridTable.Children.IndexOf(parent);
            switch (cmb.SelectedItem)
            {
                //0 seg
                case "Data":
                case "Assumption":
                case "LEM":
                    makeInvisible(parent, 0);
                    handleBox(cmb, location);
                    break;
                //1 seg
                case "PBC":
                case "Copy":
                case "∧e1":
                case "∧e2":
                case "¬¬e":
                case "¬¬i":
                case "→i":
                case "∨i1":
                case "∨i2":
                case "⊥e":
                case "¬i":
                    makeInvisible(parent, 1);
                    handleBox(cmb, location);
                    break;
                //2 seg
                case "MP":
                case "MT":
                case "∧i":
                    makeInvisible(parent, 2);
                    break;
                //3 seg
                case "∨e":
                    makeInvisible(parent, 3);
                    break;
                default:
                    makeInvisible(parent, 3);
                    break;
            }
        }
        #endregion

        #region BUTTON_CLICKS
        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            CreateLine();
        }
        private void CheckButton_click(object sender, RoutedEventArgs e)
        {
            HandleTableInput();
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
        private void btnClear_Click(object sender, RoutedEventArgs e)
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
        #endregion

        #region UTILITY 
        void HandleTableInput()
        {
            statement_list.Add(new Statement (tbValue.Text,"first","0"));
            List<String> text_boxes_list = GetAllTableInput();
            //One less column because of the line number column
            int col_to_check = TABLE_COL_NUM - 1;
            for (int i = 0; i < text_boxes_list.Count - col_to_check; i += col_to_check)
            {
                String expression = text_boxes_list[i];
                String rule = text_boxes_list[i + 1];
                String first_segment = text_boxes_list[i + 2];
                String second_segment = text_boxes_list[i + 3];
                String third_segment = text_boxes_list[i + 4];
                int current_row = i / col_to_check + 1;

                if (!IsValidStatement(expression, rule, first_segment, second_segment, third_segment, current_row))
                    return;
                Statement s = new Statement(expression, rule, first_segment, second_segment, third_segment);
                statement_list.Add(s);                
                new Evaluation(statement_list, current_row, rule); 
            }
        }
        private void displayMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        private bool ruleSelectionChecker(ComboBox cmb, string rule)
        {
            switch (rule)
            {
                case "→i":
                    if (box_closers == box_openers)
                    {
                        displayMsg("Error: more box closers then box openers", "Error");
                        cmb.SelectedItem = null;
                        return false;
                    }
                    return true;
                default:
                    return false;
            }
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

        #region KEYBOARD_FUNC
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

        #region INPUT_CHECKS

        private bool IsValidStatement(String expression, String rule, String first_segment,
           String second_segment, String third_segment, int row)
        {
            if (!IsValidExpression(expression, row))
            {
                return false;
            }

            if (string.IsNullOrEmpty(rule))
            {
                Expression_Error(row, "Rule is empty");
                return false;
            }
            if (string.IsNullOrEmpty(first_segment))
            {
                Expression_Error(row, "First segment is empty");
                return false;
            }

            if (!IsValidSegment(first_segment))
            {
                Expression_Error(row, "First segment is not a positive integer number");
                return false;
            }
            if (!IsValidSegment(second_segment))
            {
                Expression_Error(row, "Second segment is not a positive integer number");
                return false;
            }
            if (!IsValidSegment(third_segment))
            {
                Expression_Error(row, "Third segment is not a positive integer number");
                return false;
            }
            return true;
        }

        private bool IsOperator(char c)
        {
            return c == '^' || c == '>' || c == 'v' || c == '|' || c == '¬' || c == '~' ||
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

        public bool CheckPathAndFileName(string path, string file_name)
        {
            //Check if the name has invalid chars
            file_name = file_name.Trim();
            path = path.Trim();
            if (string.IsNullOrEmpty(file_name))
            {
                MessageBox.Show("File name is empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (file_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("File name can not contain < > : \" / \\ | ? *", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            //TODO add overwrite option
            /*if (File.Exists(Path.Combine(path, file_name)))
            {
                MessageBox.Show("File exists already", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }*/
            return true;
        }

        #endregion input_check

        #region TESTING
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