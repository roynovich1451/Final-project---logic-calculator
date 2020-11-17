using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xceed.Document.NET;
using Xceed.Words.NET;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;

namespace LogicCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region DEFINES

        #region GUI_DEFINES
        private const int COL_LABEL_WIDTH = 35;
        private const int COL_STATEMENT_WIDTH = 360;
        private const int COL_SEGMENT_WIDTH = 60;
        private const int COL_COMBOBOX_WIDTH = 95;
        private const int COL_TEXTBLOCK_WIDTH = COL_LABEL_WIDTH + COL_STATEMENT_WIDTH + COL_COMBOBOX_WIDTH + (3 * COL_SEGMENT_WIDTH);
        private const int CHILD_MARGIN = 4;
        private const int THICKNESS = 2;
        #endregion

        #region BOX_DEFINES
        private enum BoxState
        {
            Open,
            Close,
            None
        }

        private const int SPACES = 6;
        private const int HYPHEN = 8;
        private const int MAX_HYPHEN_CHUNKS = 14;
        private const int MIN_HYPHEN_CHUNKS = 3;
        //private const int MAX_BOX_TEXT_LENGTH = 134;
        #endregion

        #region INDEX_DEFINES
        private const int CHECKBOX_INDEX = 0;
        private const int LABEL_INDEX = 1;
        private const int STATEMENT_INDEX = 2;
        private const int COMBOBOX_INDEX = 3;
        private const int SEGMENT1_INDEX = 4;
        private const int SEGMENT2_INDEX = 5;
        private const int SEGMENT3_INDEX = 6;
        private const int TEXT_BLOCK_INDEX = 1;
        private const int TAB_PROOF_INDEX = 0;
        private const int TAB_EDITOR_INDEX = 1;
        private const int OPEN_BOX_LIST_INDEX = 0;
        private const int CLOSE_BOX_LIST_INDEX = 1;
        //private const int TAB_EDITOR_INDEX = 1;
        #endregion

        #region ERROR_DEFINES
        private const int SUCCESS = 0;
        private const int ERRMISSLINE = 1;
        private const int ERRCOUNTBRAKETS = 2;
        private const int ERRBOXMISSOPEN = 3;
        private const int ERRBOXMISSCLOSE = 4;
        private const int ERRBOXPADDING = 5;
        private const int ERRNOTFOUND = 6;
        private const int ERRMISSTURNSTILE = 7;
        #endregion

        #endregion DEFINES

        #region VARIABLES

        private int checked_checkboxes = 0;
        private readonly List<Statement> statement_list = new List<Statement>();
        private int table_row_num = 0;
        private static readonly int TABLE_COL_NUM = 6;
        private TextBox elementWithFocus;
        private readonly List<string> rules = new List<string> { "None","Data","Proven i","Proven e", "Assumption", "LEM", "PBC", "MP", "MT", "Copy"
                                                                 ,"∧i", "∧e1", "∧e2", "∨i1", "∨i2", "∨e", "¬¬e",
                                                                 "¬¬i", "→i", "⊥e", "¬i", "¬e", "→i",
                                                                 "=i","=e","∀x i","∀x e","∃x i","∃x e","∀y i","∀y e","∃y i","∃y e" };

        private int hyphen_chunks = MAX_HYPHEN_CHUNKS;
        private int spaces_chunks = MIN_HYPHEN_CHUNKS;
        private int box_closers = 0;
        private int box_openers = 0;

        #endregion VARIABLES

        public MainWindow()
        {
            InitializeComponent();
        }

        #region MENUBAR_CLICKS
        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tbValue.Text) || spGridTable.Children.Count != 0 || !string.IsNullOrEmpty(tbEditor.Text))
            {
                MessageBoxResult res = MessageBox.Show("Warning: You are about to open new file,\nYou will use not saved data\ncontinue?"
                , "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Cancel)
                    return;
            }
            NewFile();
            elementWithFocus = tbValue;
            Keyboard.Focus(elementWithFocus);
        }
        private void NewFile()
        {
            //Proof side
            tbValue.Text = "";
            spGridTable.Children.Clear();
            table_row_num = 0;
            hyphen_chunks = MAX_HYPHEN_CHUNKS;
            spaces_chunks = MIN_HYPHEN_CHUNKS;
            CheckMode(false);
            checked_checkboxes = 0;
            MasterCheck.Visibility = Visibility.Hidden;
            //Editor side
            tbEditor.Text = "";
        }
        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "doc files (*.docx)|*.docx",
                FilterIndex = 2
            };
            if (openFileDialog.ShowDialog() == false)
                return;
            string openFilePath = openFileDialog.FileName;
            if (FileInUse(openFilePath))
                return;

            using (var document = DocX.Load(openFilePath))
            {
                if (document.Tables.Count == 0)
                {
                    tbEditor.Text = document.Text;
                    mainTab.SelectedIndex = TAB_EDITOR_INDEX;
                    return;
                }
                else
                {
                    mainTab.SelectedIndex = TAB_PROOF_INDEX;
                    string expression, rule, first_segment, second_segment, third_segment;
                    //Clear Table

                    spGridTable.Children.Clear();
                    table_row_num = 0;
                    tbValue.Text = document.Paragraphs[1].Text.Substring(20).Trim();
                    Table proof_table = document.Tables[0];

                    for (int i = 1; i < proof_table.Rows.Count; i++)
                    {
                        if (proof_table.Rows[i].Cells[0].Paragraphs.First().Text.Contains("┌"))
                        {
                            spGridTable.Children.Add(CreateBox(BoxState.Open));
                        }
                        else if (proof_table.Rows[i].Cells[0].Paragraphs.First().Text.Contains("┘"))
                        {
                            spGridTable.Children.Add(CreateBox(BoxState.Close));
                        }
                        else
                        {
                            Grid current_row = CreateRow(-1);

                            expression = proof_table.Rows[i].Cells[1].Paragraphs.First().Text.Replace(" ", string.Empty);
                            rule = proof_table.Rows[i].Cells[2].Paragraphs.First().Text;
                            first_segment = proof_table.Rows[i].Cells[3].Paragraphs.First().Text.Replace(" ", string.Empty);
                            second_segment = proof_table.Rows[i].Cells[4].Paragraphs.First().Text.Replace(" ", string.Empty);
                            third_segment = proof_table.Rows[i].Cells[5].Paragraphs.First().Text.Replace(" ", string.Empty);
                            ((TextBox)current_row.Children[STATEMENT_INDEX]).Text = expression;
                            ((ComboBox)current_row.Children[COMBOBOX_INDEX]).SelectedItem = rule;
                            ((TextBox)current_row.Children[SEGMENT1_INDEX]).Text = first_segment;
                            ((TextBox)current_row.Children[SEGMENT2_INDEX]).Text = second_segment;
                            ((TextBox)current_row.Children[SEGMENT3_INDEX]).Text = third_segment;
                        }
                    }
                }
            }
            HandleMasterCheck();
        }
        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                RestoreDirectory = true,
                Filter = "doc files (*.docx)|*.docx",
                FilterIndex = 2
            };
            if (saveFileDialog.ShowDialog() == false) return;
            string saveFilePath = saveFileDialog.FileName;

            using (var document = DocX.Create(saveFilePath))
            {
                // Add a title.
                document.InsertParagraph("Logic Tool Results\n").FontSize(16d).Bold(true).UnderlineStyle(UnderlineStyle.singleLine);

                //Add the proof table
                switch (mainTab.SelectedIndex)
                {
                    case TAB_PROOF_INDEX:
                        //Add the main expression
                        document.InsertParagraph("Logical Expression: " + tbValue.Text + '\n').FontSize(14d);
                        int row_num = spGridTable.Children.Count;
                        Table proof_table = document.AddTable(row_num + 1, TABLE_COL_NUM);
                        proof_table.AutoFit = AutoFit.Contents;
                        proof_table.Alignment = Alignment.center;
                        proof_table.Design = TableDesign.None;
                        Xceed.Document.NET.Border b = new Xceed.Document.NET.Border(BorderStyle.Tcbs_double, BorderSize.one, 1, Color.Transparent);
                        proof_table.SetBorder(TableBorderType.InsideH, b);


                        proof_table.Rows[0].Cells[0].Paragraphs.First().Append("Line").UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
                        proof_table.Rows[0].Cells[1].Paragraphs.First().Append("Expression").UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
                        proof_table.Rows[0].Cells[2].Paragraphs.First().Append("Rule").UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
                        proof_table.Rows[0].Cells[3].Paragraphs.First().Append("First Segment").UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
                        proof_table.Rows[0].Cells[4].Paragraphs.First().Append("Second Segment").UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;
                        proof_table.Rows[0].Cells[5].Paragraphs.First().Append("Third Segment").UnderlineStyle(UnderlineStyle.singleLine).Alignment = Alignment.center;

                        string expression, rule, first_segment, second_segment, third_segment, line_num;
                        //Fill the proof table

                        for (int i = 1; i < row_num + 1; i++)
                        {
                            Grid row = spGridTable.Children[i - 1] as Grid;
                            if (!(row.Children[TEXT_BLOCK_INDEX] is TextBlock))
                            {
                                line_num = ((Label)row.Children[LABEL_INDEX]).Content.ToString();
                                expression = ((TextBox)row.Children[STATEMENT_INDEX]).Text.Replace(" ", string.Empty);
                                rule = ((ComboBox)row.Children[COMBOBOX_INDEX]).Text;
                                first_segment = ((TextBox)row.Children[SEGMENT1_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT1_INDEX]).Text.Replace(" ", string.Empty) : null;
                                second_segment = ((TextBox)row.Children[SEGMENT2_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT2_INDEX]).Text.Replace(" ", string.Empty) : null;
                                third_segment = ((TextBox)row.Children[SEGMENT3_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT3_INDEX]).Text.Replace(" ", string.Empty) : null;
                                proof_table.Rows[i].Cells[0].Paragraphs.First().Append(line_num).Alignment = Alignment.center;
                                proof_table.Rows[i].Cells[1].Paragraphs.First().Append(expression).Alignment = Alignment.center;
                                proof_table.Rows[i].Cells[2].Paragraphs.First().Append(rule).Alignment = Alignment.center;
                                proof_table.Rows[i].Cells[3].Paragraphs.First().Append(first_segment).Alignment = Alignment.center;
                                proof_table.Rows[i].Cells[4].Paragraphs.First().Append(second_segment).Alignment = Alignment.center;
                                proof_table.Rows[i].Cells[5].Paragraphs.First().Append(third_segment).Alignment = Alignment.center;
                            }
                            else
                            {
                                proof_table.Rows[i].MergeCells(0, TABLE_COL_NUM - 1);
                                proof_table.Rows[i].Cells[0].Paragraphs.First().Append(((TextBlock)row.Children[TEXT_BLOCK_INDEX]).Text).Alignment = Alignment.center;
                            }
                        }

                        document.InsertTable(proof_table);
                        // Save this document to disk.
                        break;

                    case TAB_EDITOR_INDEX:
                        document.InsertParagraph(tbEditor.Text).FontSize(12d);
                        break;
                }
                try
                {
                    document.Save();
                }
                catch (Exception ex)
                {
                    DisplayErrorMsg(ex.Message, "Error while trying to save");
                }
                DisplayInfoMsg("Created Document: " + saveFilePath, "Documented Created");
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
                DisplayErrorMsg(ex.Message, "Error while opening manual");
            }
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            DisplayInfoMsg("This software was made by Roy Novich and Oren Or", "About");
        }

        #endregion MENUBAR_CLICKS

        #region DYNAMIC_GUI

        private void HandleGridVisability(Grid g, int needed)
        {
            foreach (UIElement child in g.Children)
            {
                switch (needed)
                {
                    case -1:
                        if (child is Label)
                        {
                            Label lb = child as Label;
                            lb.Visibility = Visibility.Hidden;
                        }
                        if (child is TextBox)
                        {
                            TextBox tbc = child as TextBox;
                            tbc.IsEnabled = false;
                            tbc.Visibility = Visibility.Hidden;
                            tbc.Text = "";
                        }
                        break;

                    case 0:
                        if (Grid.GetColumn(child) == LABEL_INDEX)
                        {
                            ((Label)child).Visibility = Visibility.Visible;
                        }
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == LABEL_INDEX)
                            {
                                ((Label)child).Visibility = Visibility.Visible;
                            }
                            if (Grid.GetColumn(child) == STATEMENT_INDEX)
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
                        if (Grid.GetColumn(child) == LABEL_INDEX)
                        {
                            ((Label)child).Visibility = Visibility.Visible;
                        }
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == STATEMENT_INDEX ||
                                Grid.GetColumn(child) == SEGMENT1_INDEX)
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
                        if (Grid.GetColumn(child) == LABEL_INDEX)
                        {
                            ((Label)child).Visibility = Visibility.Visible;
                        }
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == STATEMENT_INDEX ||
                                Grid.GetColumn(child) == SEGMENT1_INDEX ||
                                Grid.GetColumn(child) == SEGMENT2_INDEX)
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
                        if (Grid.GetColumn(child) == LABEL_INDEX)
                        {
                            ((Label)child).Visibility = Visibility.Visible;
                        }
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
        private void HandleLabelsAfterCheckMode()
        {
            int index = 0;
            foreach (Grid row in spGridTable.Children)
            {
                if (row.Children[LABEL_INDEX] is Label)
                {
                    Label label = row.Children[LABEL_INDEX] as Label;
                    label.Content = ++index;
                }
            }
            //re-initial variables
            if (spGridTable.Children.Count == 0)
                table_row_num = 0;
            else
                table_row_num = index;
        }
        private List<Grid> GetChecked()
        {
            List<Grid> ret = new List<Grid>();
            foreach (var child in spGridTable.Children)
            {
                if (child is Grid)
                {
                    Grid row = child as Grid;
                    CheckBox chb = row.Children[CHECKBOX_INDEX] as CheckBox;
                    if (chb.IsChecked == true)
                    {
                        ret.Add(row);
                    }
                }
            }
            return ret;
        }
        private string CreateBoxLine(BoxState state)
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
        private Grid CreateBox(BoxState state)
        {
            //textblock - opener
            Grid newLine = new Grid();
            ColumnDefinition gridCol0 = new ColumnDefinition
            {
                Width = new GridLength(COL_LABEL_WIDTH)
            };
            ColumnDefinition gridCol1 = new ColumnDefinition
            {
                Width = new GridLength(COL_TEXTBLOCK_WIDTH)
            };
            newLine.ColumnDefinitions.Add(gridCol0);
            newLine.ColumnDefinitions.Add(gridCol1);

            //checkbox - clear/remove
            CheckBox chb = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Visibility = Visibility.Visible,
                IsChecked = false,
                Margin = new Thickness(THICKNESS)
            };
            chb.Click += new RoutedEventHandler(Chb_click);
            Grid.SetColumn(chb, CHECKBOX_INDEX);

            TextBlock tbline = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(THICKNESS),
                FontWeight = FontWeights.Bold,
                Width = COL_TEXTBLOCK_WIDTH
            };
            Grid.SetColumn(tbline, TEXT_BLOCK_INDEX);
            if (state != BoxState.None)
                tbline.Text = CreateBoxLine(state);

            newLine.Children.Add(chb);
            newLine.Children.Add(tbline);
            return newLine;
        }
        private void CheckMode(bool state)
        {
            if (state == true)
            {
                btnAddBefore.Visibility = Visibility.Visible;
                btnRemove.Visibility = Visibility.Visible;
                btnClear.Visibility = Visibility.Visible;
                btnAddLine.Visibility = Visibility.Hidden;
                btncheckButton.Visibility = Visibility.Hidden;
            }
            else
            {
                btnAddBefore.Visibility = Visibility.Hidden;
                btnRemove.Visibility = Visibility.Hidden;
                btnClear.Visibility = Visibility.Hidden;
                btnAddLine.Visibility = Visibility.Visible;
                btncheckButton.Visibility = Visibility.Visible;
            }
        }
        private void HandleBox(BoxState state, int index)
        {

            switch (state)
            {
                case BoxState.Close:
                    spGridTable.Children.Insert(index, CreateBox(BoxState.Close));
                    ++box_closers;
                    break;

                case BoxState.Open:
                    spGridTable.Children.Insert(index, CreateBox(BoxState.Open));
                    ++box_openers;
                    break;
            }
            MasterCheck.Visibility = Visibility.Visible;

        }
        private Grid CreateRow(int index)
        {
            ++table_row_num;
            //create new line and define cols
            Grid newLine = new Grid();
            ColumnDefinition gridCol0 = new ColumnDefinition
            {
                Width = new GridLength(COL_LABEL_WIDTH)
            };
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
                Width = new GridLength(COL_COMBOBOX_WIDTH)
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
            newLine.ColumnDefinitions.Add(gridCol0);
            newLine.ColumnDefinitions.Add(gridCol1);
            newLine.ColumnDefinitions.Add(gridCol2);
            newLine.ColumnDefinitions.Add(gridCol3);
            newLine.ColumnDefinitions.Add(gridCol4);
            newLine.ColumnDefinitions.Add(gridCol5);
            newLine.ColumnDefinitions.Add(gridCol6);

            //checkbox - clear/remove
            CheckBox chb = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Visibility = Visibility.Visible,
                IsChecked = false,
                Margin = new Thickness(THICKNESS)
            };
            chb.Click += new RoutedEventHandler(Chb_click);
            Grid.SetColumn(chb, CHECKBOX_INDEX);

            //label
            Label lb = new Label
            {
                Content = $"{table_row_num}",
                Margin = new Thickness(THICKNESS),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lb, LABEL_INDEX);

            //textblock - statement
            TextBox tbState = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbStatement{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_STATEMENT_WIDTH - CHILD_MARGIN
            };
            Grid.SetColumn(tbState, STATEMENT_INDEX);

            //comboBox - rule
            ComboBox cmbRules = new ComboBox
            {
                ItemsSource = rules,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"cmbRules{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_COMBOBOX_WIDTH - CHILD_MARGIN
            };
            cmbRules.SelectionChanged += new SelectionChangedEventHandler(Cmb_SelectedValueChanged);
            Grid.SetColumn(cmbRules, COMBOBOX_INDEX);

            //textblock - first segment
            TextBox tbFirstSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbFirstSeg{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN,
                Visibility = Visibility.Hidden
            };
            Grid.SetColumn(tbFirstSeg, SEGMENT1_INDEX);

            //textblock - second segment
            TextBox tbSecondSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbSecondSeg{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN,
                Visibility = Visibility.Hidden
            };
            Grid.SetColumn(tbSecondSeg, SEGMENT2_INDEX);

            //textblock - third segment
            TextBox tbThirdSeg = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Name = $"tbThirdSeg{table_row_num}",
                Margin = new Thickness(THICKNESS),
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN,
                Visibility = Visibility.Hidden
            };
            Grid.SetColumn(tbThirdSeg, SEGMENT3_INDEX);

            //add children to new line
            newLine.Children.Add(chb);
            newLine.Children.Add(lb);
            newLine.Children.Add(tbState);
            newLine.Children.Add(cmbRules);
            newLine.Children.Add(tbFirstSeg);
            newLine.Children.Add(tbSecondSeg);
            newLine.Children.Add(tbThirdSeg);

            //add new line to StackPanel
            if (index == -1) //append
                spGridTable.Children.Add(newLine);
            else
                spGridTable.Children.Insert(index, newLine);

            return newLine;
        }
        private void CheckAll(bool state)
        {
            checked_checkboxes = 0;
            foreach (Grid child in spGridTable.Children)
            {
                ((CheckBox)child.Children[0]).IsChecked = state;
                if (state == true)
                    ++checked_checkboxes;
            }
        }
        #endregion DYNAMIC_GUI

        #region EVENTS
        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainTab.SelectedIndex == TAB_PROOF_INDEX)
                btnCreateBox.Visibility = Visibility.Visible;
            else
                btnCreateBox.Visibility = Visibility.Hidden;
        }
        private void Chb_click(object sender, RoutedEventArgs e)
        {
            CheckBox chb = sender as CheckBox;
            if (chb.Name.Equals("MasterCheck"))
            {
                if (chb.IsChecked == true)
                    CheckAll(true);
                else
                    CheckAll(false);
            }
            else
            {
                MasterCheck.IsChecked = false;
                if (chb.IsChecked == true)
                {
                    ++checked_checkboxes;
                }
                else
                {
                    --checked_checkboxes;
                }
            }
            if (checked_checkboxes > 0)
                CheckMode(true);
            else
                CheckMode(false);
        }
        private void Cmb_SelectedValueChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            Grid parent = cmb.Parent as Grid;
            var location = spGridTable.Children.IndexOf(parent);
            string rule = cmb.SelectedItem as string;
            switch (rule)
            {
                //0 seg
                case "Data":
                case "Assumption":
                case "LEM":
                case "None":
                case "=i":
                case "Proven i":
                    HandleGridVisability(parent, 0);
                    //handleBox(cmb, location);
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
                case "∀i":
                case var ai when new Regex(@"∀.*i").IsMatch(ai):
                case var ae when new Regex(@"∀.*e").IsMatch(ae):
                case var ei when new Regex(@"∃.*i").IsMatch(ei):
                    HandleGridVisability(parent, 1);
                    //handleBox(cmb, location);
                    break;
                //2 seg
                case "MP":
                case "MT":
                case "¬e":
                case "∧i":
                case "∃e":
                case "=e":
                case "Proven e":
                case var ee when new Regex(@"∃.*e").IsMatch(ee):
                    HandleGridVisability(parent, 2);
                    break;
                //3 seg
                case "∨e":
                    HandleGridVisability(parent, 3);
                    break;

                case "Close Box":
                    HandleGridVisability(parent, -1);
                    //handleBox(cmb, location);
                    break;

                default:
                    HandleGridVisability(parent, 0);
                    break;
            }
        }
        #endregion EVENTS

        #region BUTTON_CLICKS
        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            Grid line = CreateRow(-1);
            elementWithFocus = line.Children[STATEMENT_INDEX] as TextBox;
            Keyboard.Focus(elementWithFocus);
            MasterCheck.Visibility = Visibility.Visible;
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
        private void BtnArrow_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "→");
            }
        }
        private void BtnNot_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "¬");
            }
        }

        private void BtnForAll_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "∀");
            }
        }

        private void BtnExists_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "∃");
            }
        }

        private void BtnCapPsi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Ψ");
            }
        }

        private void BtnCapPhi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Φ");
            }
        }

        private void BtnCapChi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Χ");
            }
        }
        private void BtnCapBeta_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Β");
            }
        }

        private void BtnCapGama_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Γ");
            }
        }

        private void BtnCapDelta_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Δ");
            }
        }

        private void BtnCapEpsilon_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Ε");
            }
        }

        private void BtnCapTeta_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Θ");
            }
        }

        private void BtnCapPai_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Π");
            }
        }

        private void BtnCapOmega_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Ω");
            }
        }

        private void BtnBeta_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "β");
            }
        }

        private void BtnGama_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "γ");
            }
        }

        private void BtnDelta_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "δ");
            }
        }

        private void BtnEpsilon_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "ε");
            }
        }

        private void BtnTeta_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "θ");
            }
        }

        private void BtnPai_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "π");
            }
        }
        private void BtnPhi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "φ");
            }
        }

        private void BtnChi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "χ");

            }
        }

        private void BtnPsi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "ψ");
            }
        }
        private void BtnOmega_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "ω");
            }
        }
        private void BtnX_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "X0");
            }
        }

        private void BtnY_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "Y0");
            }
        }
        private int isBracketsLogic(Grid uperRow, Grid lowerRow)
        {
            int braketsCnt = 0;
            int openIndex = spGridTable.Children.IndexOf(uperRow),
                closeIndex = spGridTable.Children.IndexOf(lowerRow);
            for (int i = openIndex; i <= closeIndex; i++)
            {
                if (spGridTable.Children[i] is Grid grid)
                {

                    if (grid.Children[1] is TextBlock tblock)
                    {
                        if (tblock.Text.Contains("└"))
                        {
                            braketsCnt--;
                        }
                        if (tblock.Text.Contains("┌"))
                        {
                            braketsCnt++;
                        }
                        if (braketsCnt < 0)
                            return -ERRCOUNTBRAKETS;
                    }
                }
            }
            if (braketsCnt != 0)
                return -ERRCOUNTBRAKETS;
            return SUCCESS;
        }
        private int IsBoxValid(Grid upperRow, Grid lowerRow)
        {

            //chekc picked indexes, to see if ligal (mathematic brackets logic)
            int ret = isBracketsLogic(upperRow, lowerRow);
            if (ret != 0)
                return ret;

            //check if the current checked rows aren't already Box
            if (!(upperRow.Children[LABEL_INDEX] is Label) && !(lowerRow.Children[LABEL_INDEX] is Label))
            {
                return -ERRMISSLINE;
            }
            int upperRowIndex = spGridTable.Children.IndexOf(upperRow);
            int lowerRowIndex = spGridTable.Children.IndexOf(lowerRow);
            //check outerbox padding
            ret = SUCCESS;
            List<Grid> outerBox = getOuterBox(spGridTable, upperRow, lowerRow);
            if (outerBox.Count != 0)
            {
                List<Grid> ignoreLines = new List<Grid>();
                for (int i = upperRowIndex; i <= lowerRowIndex; i++)
                {
                    ignoreLines.Add(spGridTable.Children[i] as Grid);
                }
                int openerIndex = spGridTable.Children.IndexOf(outerBox[OPEN_BOX_LIST_INDEX]) + 1,
                   closerIndex = spGridTable.Children.IndexOf(outerBox[CLOSE_BOX_LIST_INDEX]);

                ret = checkBoxPadding(spGridTable, openerIndex, closerIndex, ignoreLines);
                if (ret == -ERRBOXPADDING)
                {
                    brushBackgroundRed(outerBox[OPEN_BOX_LIST_INDEX].Children[TEXT_BLOCK_INDEX]);
                    brushBackgroundRed(outerBox[CLOSE_BOX_LIST_INDEX].Children[TEXT_BLOCK_INDEX]);
                }

            }
            return ret;
        }

        private List<Grid> getOuterBox(StackPanel spRows, Grid upperRow, Grid lowerRow)
        {
            List<Grid> ret = new List<Grid>();
            int upperRowIndex = spRows.Children.IndexOf(upperRow),
                lowerRowIndex = spRows.Children.IndexOf(lowerRow);
            int parCnt = 0;
            bool hasOuter = false;
            if (upperRowIndex != 0) //if upperRowindex = 0 no way there is outer box
            {
                for (int i = upperRowIndex - 1; i >= 0; i--)
                {
                    Grid currentRow = spRows.Children[i] as Grid;
                    if (currentRow.Children[TEXT_BLOCK_INDEX] is TextBlock tBlock)
                    {
                        if (tBlock.Text.Contains("┌"))
                        {
                            if (parCnt == 0)
                            {
                                ret.Add(currentRow);
                                hasOuter = true;
                                break;
                            }
                            else
                                parCnt++;

                        }
                        if (tBlock.Text.Contains("└"))
                            parCnt--;
                    }
                }
                if (hasOuter)
                {
                    ret.Add(GetCloserGrid(spRows, ret[OPEN_BOX_LIST_INDEX]));
                }
            }
            return ret;
        }
        private void brushBackgroundRed(UIElement elem)
        {
            if (elem is TextBox)
                ((TextBox)elem).Background = Brushes.Red;
            else
                ((TextBlock)elem).Background = Brushes.Red;
        }
        private void BtnCreateBox_Click(object sender, RoutedEventArgs e)
        {
            if (checked_checkboxes != 2)
            {
                DisplayErrorMsg("Error: Need to check exactly 2 rows if you want to create box", "Error");
                CheckMode(false);
                return;
            }

            var checkedForBox = GetCheckedForBox();
            int ret = IsBoxValid(checkedForBox[OPEN_BOX_LIST_INDEX], checkedForBox[CLOSE_BOX_LIST_INDEX]);
            if (ret == -ERRCOUNTBRAKETS)
            {
                DisplayErrorMsg($"Error: Can't create box, checked rows are illegal parenthesis Validity", "Error");
                CheckMode(false);
                return;
            }
            else if (ret == -ERRMISSLINE)
            {
                DisplayErrorMsg($"Error: Can't create box, must be at least one line padding between 2 boxes", "Error");
                CheckMode(false);
                return;
            }
            else if (ret == -ERRBOXPADDING)
            {
                DisplayErrorMsg($"Error: Can't create box, outer box must have at least one line padding", "Error");
                removeBackgroundColor();
                CheckMode(false);
                return;
            }
            int openIndex = spGridTable.Children.IndexOf(checkedForBox[OPEN_BOX_LIST_INDEX]);
            int closeIndex = spGridTable.Children.IndexOf(checkedForBox[CLOSE_BOX_LIST_INDEX]);
            ChangeBoxVariables(openIndex);
            HandleBox(BoxState.Open, openIndex);
            HandleBox(BoxState.Close, closeIndex + 2);
            CheckInnerBoxesSize(openIndex, closeIndex + 3);
            CheckMode(false);
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> pickedByUserRows = GetChecked();
            MessageBoxResult res = MessageBox.Show($"Warning: You are about to CLEAR {pickedByUserRows.Count} lines\nPlease confirm",
                "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Cancel)
                return;
            foreach (Grid row in pickedByUserRows)
            {
                foreach (var child in row.Children)
                {
                    //if (child is TextBlock)
                    //{
                    //    spGridTable.Children.Remove(row);
                    //}
                    if (child is TextBox textbox)
                    {
                        textbox.Text = string.Empty;
                    }
                    if (child is ComboBox combobox)
                    {
                        combobox.SelectedIndex = -1;
                    }
                    if (child is CheckBox checkbox)
                    {
                        checkbox.IsChecked = false;
                    }
                }
            }
            //re-initialize variables
            checked_checkboxes = 0;
            MasterCheck.IsChecked = false;
            CheckMode(false);
        }
        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> pickedByUserRows = GetChecked();
            MessageBoxResult res = MessageBox.Show($"Warning: You are about to REMOVE {pickedByUserRows.Count} lines\nPlease confirm",
                "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Cancel) return;
            int ret = checkAllBoxesPadding(pickedByUserRows);
            if (ret == -ERRBOXPADDING)
            {
                DisplayErrorMsg("Error: after remove one of the boxes will be without lines inside\nPlease recheck rows", "ERROR");
                removeBackgroundColor();
                return;
            }
            ret = CheckBoxesForRemove(pickedByUserRows);
            if (ret > 0)
            {
                hyphen_chunks = MAX_HYPHEN_CHUNKS + 1;
                spaces_chunks = MIN_HYPHEN_CHUNKS - 1;
                CheckInnerBoxesSize(0, spGridTable.Children.Count - 1);
            }
            if (ret == -ERRBOXMISSCLOSE)
            {
                DisplayErrorMsg("Error: checked box missng it's closer\nPlease recheck rows", "ERROR");
                removeBackgroundColor();
                return;
            }
            if (ret == -ERRBOXMISSOPEN)
            {
                DisplayErrorMsg("Error: checked box missng it's opener\nPlease recheck rows", "ERROR");
                removeBackgroundColor();
                return;
            }
            foreach (Grid row in pickedByUserRows)
            {
                spGridTable.Children.Remove(row);
            }
            HandleLabelsAfterCheckMode();
            checked_checkboxes = 0;
            MasterCheck.IsChecked = false;
            HandleMasterCheck();
            CheckMode(false);
        }


        private int CheckBoxesForRemove(List<Grid> pickedByUserRows)
        {
            List<Grid> verified = new List<Grid>();

            foreach (Grid row in pickedByUserRows)
            {
                //if closer, must be varified already
                if ((row.Children[TEXT_BLOCK_INDEX] is TextBlock tbClose) && tbClose.Text.Contains("└"))
                {
                    if (!verified.Contains(row))
                    {
                        brushBackgroundRed(row.Children[TEXT_BLOCK_INDEX]);
                        return -ERRBOXMISSOPEN;
                    }

                    continue;
                }
                //if opener
                if ((row.Children[TEXT_BLOCK_INDEX] is TextBlock tbOpen) && tbOpen.Text.Contains("┌"))
                {
                    int startSearchIndex = spGridTable.Children.IndexOf(row);
                    Grid closerGrid = GetCloserGrid(spGridTable, row);
                    if (closerGrid == null)
                    {
                        DisplayErrorMsg("something went wrong", "ERROR");
                    }
                    if (((CheckBox)closerGrid.Children[CHECKBOX_INDEX]).IsChecked == false)
                    {
                        brushBackgroundRed(row.Children[TEXT_BLOCK_INDEX]);
                        return -ERRBOXMISSCLOSE;
                    }
                    else
                    {
                        verified.Add(closerGrid);
                    }

                }
            }
            return verified.Count;
        }

        private int checkAllBoxesPadding(List<Grid> checkedRows)
        {
            foreach (Grid row in spGridTable.Children)
            {
                if ((row.Children[TEXT_BLOCK_INDEX] is TextBlock tbOpen) && tbOpen.Text.Contains("┌") &&
                    !checkedRows.Contains(row))
                {
                    Grid boxCloser = GetCloserGrid(spGridTable, row);
                    int boxCloserIndex = spGridTable.Children.IndexOf(boxCloser),
                        boxOpenerIndex = spGridTable.Children.IndexOf(row);
                    if (checkBoxPadding(spGridTable, boxOpenerIndex + 1, boxCloserIndex, checkedRows) == -ERRBOXPADDING)
                    {
                        brushBackgroundRed(tbOpen as UIElement);
                        brushBackgroundRed(boxCloser.Children[TEXT_BLOCK_INDEX]);
                        return -ERRBOXPADDING;
                    }

                }
            }
            return SUCCESS;
        }

        private int checkBoxPadding(StackPanel sp, int boxOpenerIndex, int boxCloserIndex, List<Grid> checkedRows)
        {
            int ret = GetLineInsideBox(spGridTable, boxOpenerIndex, boxCloserIndex, checkedRows);
            if (ret == 0)
            {
                return -ERRBOXPADDING;
            }
            return ret;
        }
        private int GetLineInsideBox(StackPanel sp, int from, int to, List<Grid> pickedByUserRows)
        {
            int bracketsCnt = 0;
            int linesInBox = 0;
            for (int i = from; i < to; i++)
            {
                Grid currentRow = sp.Children[i] as Grid;
                if (pickedByUserRows != null && pickedByUserRows.Contains(currentRow))
                    continue;
                if (currentRow.Children[TEXT_BLOCK_INDEX] is TextBlock tbCurrnet)
                {
                    if (tbCurrnet.Text.Contains("┌"))
                    {
                        bracketsCnt++;
                    }
                    if (tbCurrnet.Text.Contains("└"))
                    {
                        bracketsCnt--;
                    }
                }
                else
                {
                    if (bracketsCnt == 0)
                    {
                        linesInBox++;
                    }
                }
            }
            return linesInBox;
        }

        private Grid GetCloserGrid(StackPanel sp, Grid row)
        {
            TextBlock tbOpener = row.Children[TEXT_BLOCK_INDEX] as TextBlock;

            for (int i = sp.Children.IndexOf(row); i < sp.Children.Count; i++)
            {
                if (((Grid)sp.Children[i]).Children[TEXT_BLOCK_INDEX] is TextBlock tbCurrent)
                {
                    if (tbCurrent.Text.Contains("└") && (tbCurrent.Text.Length == tbOpener.Text.Length))
                    {
                        return tbCurrent.Parent as Grid;
                    }
                }
            }
            return null;
        }

        private void removeBackgroundColor()
        {
            foreach (Grid row in spGridTable.Children)
            {
                foreach (var child in row.Children)
                {
                    if (child is TextBlock tBlock)
                    {
                        tBlock.Background = null;
                    }
                    if (child is TextBox tBox)
                    {
                        tBox.Background = null;
                    }
                }
            }
        }

        private void BtnAddBefore_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> pickedByUserRows = GetChecked();
            MessageBoxResult res = MessageBox.Show($"Warning: You are about to Add {pickedByUserRows.Count} lines\nPlease confirm",
                "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Cancel) return;
            foreach (Grid row in pickedByUserRows)
            {
                ((CheckBox)row.Children[CHECKBOX_INDEX]).IsChecked = false;
                int addToIndex = spGridTable.Children.IndexOf(row);
                CreateRow(addToIndex);
            }
            HandleLabelsAfterCheckMode();
            //TODO: handlBoxesAfterRemove()!!
            CheckMode(false);
            MasterCheck.IsChecked = false;
            checked_checkboxes = 0;
        }
        #endregion BUTTON_CLICKS

        #region UTILITY
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        private void HandleMasterCheck()
        {
            if (spGridTable.Children.Count == 0)
                MasterCheck.Visibility = Visibility.Hidden;
            else
                MasterCheck.Visibility = Visibility.Visible;
        }
        private void CheckInnerBoxesSize(int openIndex, int closeIndex)
        {
            hyphen_chunks--;
            spaces_chunks++;
            for (int i = openIndex + 1; i < closeIndex; i++)
            {
                if (spGridTable.Children[i] is Grid grid)
                {
                    if (grid.Children[1] is TextBlock tblock)
                    {
                        if (tblock.Text.Contains("┌"))
                        {
                            tblock.Text = CreateBoxLine(BoxState.Open);
                        }
                        if (tblock.Text.Contains("└"))
                        {
                            tblock.Text = CreateBoxLine(BoxState.Close);
                        }
                    }
                }
            }
        }
        private void ChangeBoxVariables(int openIndex)
        {
            int outerBoxcnt = 0;

            for (int i = 0; i < openIndex; i++)
            {
                if (spGridTable.Children[i] is Grid grid)
                {
                    if (grid.Children[1] is TextBlock tblock)
                    {
                        if (tblock.Text.Contains("┌"))
                        {
                            ++outerBoxcnt;
                        }
                        if (tblock.Text.Contains("└"))
                        {
                            --outerBoxcnt;
                        }

                    }
                }
            }
            hyphen_chunks = MAX_HYPHEN_CHUNKS - outerBoxcnt;
            spaces_chunks = MIN_HYPHEN_CHUNKS + outerBoxcnt;
        }
        private List<Grid> GetCheckedForBox()
        {
            List<Grid> gridList = new List<Grid>();
            foreach (Grid grid in spGridTable.Children)
            {
                CheckBox chb = grid.Children[0] as CheckBox;
                if (chb.IsChecked == true)
                {
                    chb.IsChecked = false;
                    gridList.Add(grid);
                }
            }
            checked_checkboxes = 0;
            MasterCheck.IsChecked = false;
            return gridList;
        }
        public List<TextBox> GetAllTextBoxes()
        {
            UIElementCollection grids = spGridTable.Children;
            List<TextBox> ret = new List<TextBox>();
            foreach (Grid row in grids)
            {
                foreach (var child in row.Children)
                {
                    if (child is TextBox textbox)
                    {
                        ret.Add(textbox);
                    }
                    if (child is ComboBox combobox)
                    {
                        TextBox t = new TextBox
                        {
                            Text = combobox.Text
                        };
                        ret.Add(t);
                    }
                }
            }
            return ret;
        }
        /*
        public List<Tuple<int, string>> GetBoxPairs()
        {
            UIElementCollection grids = spGridTable.Children;
            List<Tuple<int, string>> ret = new List<Tuple<int, string>>();
            int index = 0;
            foreach (Grid row in grids)
            {
                if (row.Children[TEXT_BLOCK_INDEX] is TextBlock block)
                {
                    ret.Add(new Tuple<int, string>(index, block.Text));
                }
                index++;
            }
            return ret;
        }
        */
        private bool IsValidStatement(string expression, string rule, string first_segment,
           string second_segment, string third_segment)
        {
            int row = statement_list.Count;
            if (!IsValidExpression(expression, row))
            {
                return false;
            }
            if (string.IsNullOrEmpty(rule))
            {
                Expression_Error(row, "Rule is empty");
                return false;
            }
            if (first_segment != null)
            {
                if (first_segment == string.Empty)
                {
                    Expression_Error(row, "First segment is empty");
                    return false;
                }
                else if (!IsValidSegment(first_segment))
                {
                    Expression_Error(row, "First segment is not a positive integer number");
                    return false;
                }
            }
            if (second_segment != null)
            {
                if (second_segment == string.Empty)
                {
                    Expression_Error(row, "Second segment is empty");
                    return false;
                }
                if (!IsValidSegment(second_segment))
                {
                    Expression_Error(row, "Second segment is not a positive integer number");
                    return false;
                }
            }
            if (third_segment != null)
            {
                if (third_segment == string.Empty)
                {
                    Expression_Error(row, "Third segment is empty");
                    return false;
                }
                if (!IsValidSegment(third_segment))
                {
                    Expression_Error(row, "Third segment is not a positive integer number");
                    return false;
                }
            }

            return true;
        }

        //OREN
        private void HandleTableInput()
        {
            string msg = "All input is valid";
            statement_list.Clear();
            if (string.IsNullOrEmpty(tbValue.Text))
            {
                DisplayErrorMsg("Miss proof header", "Error");
                return;
            }
            if (!isValidLogicalEquivalent(tbValue.Text))
            {
                return;  
            }
            if (spGridTable.Children.Count == 0)
            {
                DisplayErrorMsg("Nothing to check", "Error");
                return;
            }

            statement_list.Add(new Statement(tbValue.Text, "first", "0"));
            string expression, rule, first_segment, second_segment, third_segment;
            int index;
            bool isGoalAchived = false;

            string header = "Conclusions";
            //One less column because of the line number column


            foreach (Grid row in spGridTable.Children)
            {
                index = spGridTable.Children.IndexOf(row);
                if (row.Children[LABEL_INDEX] is Label)
                {
                    expression = ((TextBox)row.Children[STATEMENT_INDEX]).Text.Replace(" ", string.Empty);
                    rule = ((ComboBox)row.Children[COMBOBOX_INDEX]).Text.Replace(" ", string.Empty);
                    first_segment = ((TextBox)row.Children[SEGMENT1_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT1_INDEX]).Text.Replace(" ", string.Empty) : null;
                    second_segment = ((TextBox)row.Children[SEGMENT2_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT2_INDEX]).Text.Replace(" ", string.Empty) : null;
                    third_segment = ((TextBox)row.Children[SEGMENT3_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT3_INDEX]).Text.Replace(" ", string.Empty) : null;
                    if (rule.Equals("Proveni"))
                    {
                        if (!isValidLogicalEquivalent(expression))
                            return;
                    }
                    else
                    {
                        if (!IsValidStatement(expression, rule, first_segment, second_segment, third_segment))
                            return;
                    }
                    statement_list.Add(new Statement(expression, rule, first_segment, second_segment, third_segment));
                    Evaluation e = new Evaluation(statement_list, rule);
                    if (!e.Is_Valid)
                        return;
                }
            }
            isGoalAchived = checkGoalAchived(spGridTable.Children[spGridTable.Children.Count - 1] as Grid);
            if (isGoalAchived)
            {
                foreach (Grid row in spGridTable.Children)
                {
                    index = spGridTable.Children.IndexOf(row);
                    if (row.Children[LABEL_INDEX] is Label)
                    {
                        expression = ((TextBox)row.Children[STATEMENT_INDEX]).Text.Replace(" ", string.Empty);
                        rule = ((ComboBox)row.Children[COMBOBOX_INDEX]).Text.Replace(" ", string.Empty);
                        first_segment = ((TextBox)row.Children[SEGMENT1_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT1_INDEX]).Text.Replace(" ", string.Empty) : null;
                        second_segment = ((TextBox)row.Children[SEGMENT2_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT2_INDEX]).Text.Replace(" ", string.Empty) : null;
                        third_segment = ((TextBox)row.Children[SEGMENT3_INDEX]).IsEnabled ? ((TextBox)row.Children[SEGMENT3_INDEX]).Text.Replace(" ", string.Empty) : null;

                        if (!IsValidBox(row, rule, first_segment, second_segment, third_segment))
                            return;
                    }
                }
                msg += ", proof success!";
            }
            else
                msg += ", but still didn't achived goal";

            DisplayInfoMsg(msg, header);
        }

        private bool isValidLogicalEquivalent(string s)
        {
            //VALIDATE HAS '⊢'
            if (!s.Contains('⊢'))
            {
                DisplayErrorMsg("Valid logical Equivalent must contain '⊢'", "Error");
                return false;
            }
            //VALIDATE WHAT SHOULD BE PROOF
            string goal = getGoal(s);
            if (!IsValidExpression(goal, -1))
                return false;
            //VALIDATE DATA
            string data = getData(s);
            string[] dataSplit = data.Split(',');
            foreach (string d in dataSplit)
            {
                if (!IsValidExpression(d, -1))
                {
                    return false;
                }
            }
            return true;
        }

        private bool checkGoalAchived(Grid lastRow)
        {
            if (lastRow.Children[TEXT_BLOCK_INDEX] is TextBlock)
                return false;

            string lastRowInput = replaceAll(((TextBox)lastRow.Children[STATEMENT_INDEX]).Text);
            string needToProof = getGoal(tbValue.Text);
            return lastRowInput.Equals(needToProof);
        }
        private string getData(string s)
        {
            string[] splited = s.Split('⊢');
            return replaceAll(splited[0]);
        }
        private string getGoal(string s)
        {
            string[] splited = s.Split('⊢');
            return replaceAll(splited[1]);
        }

        private string replaceAll(string s)
        {
            return s.Trim().Replace('^', '∧').Replace('V', '∨').Replace('~', '¬').Replace(" ", "");
        }
        private bool IsValidBox(Grid row, string rule, string first_segment, string second_segment, string third_segment)
        {

            switch (rule)
            {
                case "Assumption":
                    if (!haveAboveOpener(spGridTable.Children.IndexOf(row)))
                    {
                        DisplayErrorMsg($"Error: assumption at row {((Label)row.Children[LABEL_INDEX]).Content.ToString()}," +
                            $"\nmissing box opener above", "Error");
                        return false;
                    }
                    return true;
                case "Copy":
                    if (!copyFromLegalBox(row, first_segment))
                    {
                        DisplayErrorMsg($"Error: Copy  at row {((Label)row.Children[LABEL_INDEX]).Content.ToString()}," +
                            $"\ncan use only for variable from current or upper box", "Error");
                        return false;
                    }
                    return true;
                case "∨e":
                    List<int> firstbox = Evaluation.Get_Lines_From_Segment(second_segment),
                        secondBox = Evaluation.Get_Lines_From_Segment(third_segment);
                    if (!hasWrapBox(firstbox[0], firstbox[firstbox.Count - 1]) ||
                        !hasWrapBox(secondBox[0], secondBox[secondBox.Count - 1]))
                    {
                        DisplayErrorMsg($"Error: Or elimination at line {((Label)row.Children[LABEL_INDEX]).Content.ToString()}," +
                            $"\nlines mentioned in second and third segments must be wrapped with boxes", "Error");
                        return false;
                    }
                    return true;
                case "¬i":
                case "→i":
                case var a when new Regex(@"∀.*i").IsMatch(a):
                case var e when new Regex(@"∃.*e").IsMatch(e):
                    string segment = "first";
                    List<int> box;
                    if (new Regex(@"∃.*e").IsMatch(rule))
                    {
                        box = Evaluation.Get_Lines_From_Segment(second_segment);
                        segment = "second";
                    }
                    else
                        box = Evaluation.Get_Lines_From_Segment(first_segment);
                    if (!hasWrapBox(box[0], box[box.Count - 1]))
                    {
                        string messageRule = "rule";
                        if (rule.Equals("¬i"))
                            messageRule = "Not introduction";
                        else if (rule.Equals("→i"))
                            messageRule = "Arrow introducstion";
                        else if (new Regex(@"∀.*i").IsMatch(rule))
                            messageRule = "All introdution";
                        else if (new Regex(@"∃.*e").IsMatch(rule))
                            messageRule = "Exist elimination";
                        DisplayErrorMsg($"Error: {messageRule} at line {((Label)row.Children[LABEL_INDEX]).Content.ToString()}," +
                            $"\nlines mentioned in {segment} segment must be wrapped with box", "Error");
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private bool hasWrapBox(int open, int close)
        {
            int realOpenIndex = getspGridIndex(open),
                realCloseIndex = getspGridIndex(close);
            if (realOpenIndex <= 0 || realCloseIndex < 0 || realCloseIndex == spGridTable.Children.Count - 1)
                return false;
            if (!isCorrectBox(realOpenIndex - 1, BoxState.Open) || !isCorrectBox(realCloseIndex + 1, BoxState.Close))
                return false;
            return true;
        }
        bool isCorrectBox(int index, BoxState state)
        {
            Grid row = spGridTable.Children[index] as Grid;

            if (row.Children[LABEL_INDEX] is Label)
                return false;
            else
            {
                string search = "┌";
                if (state == BoxState.Close)
                    search = "└";
                if (!((TextBlock)row.Children[TEXT_BLOCK_INDEX]).Text.Contains(search))
                    return false;
            }
            return true;
        }
        private int getspGridIndex(int labelNumber)
        {
            for (int i = labelNumber; i < spGridTable.Children.Count; i++)
            {
                Grid row = spGridTable.Children[i] as Grid;
                if (row.Children[LABEL_INDEX] is Label lb)
                {
                    if (Int32.Parse(lb.Content.ToString()) == labelNumber)
                        return i;
                }
            }
            return -ERRNOTFOUND;
        }

        private bool copyFromLegalBox(Grid row, string first_segment)
        {
            int start = spGridTable.Children.IndexOf(row);
            bool isValid = true;
            for (int i = start - 1; i >= 0; i--)
            {
                Grid currentRow = spGridTable.Children[i] as Grid;
                if (currentRow.Children[LABEL_INDEX] is Label lb)
                {
                    if (lb.Content.ToString() == first_segment)
                        break;
                }
                else
                {
                    TextBlock tb = currentRow.Children[TEXT_BLOCK_INDEX] as TextBlock;
                    if (tb.Text.Contains("┌"))
                        isValid = true;
                    else
                        isValid = false;
                }

            }
            return isValid;
        }

        private bool haveAboveOpener(int index)
        {
            if (index == 0)
                return false;
            if (((Grid)spGridTable.Children[index - 1]).Children[TEXT_BLOCK_INDEX] is TextBlock tBlock)
            {
                if (tBlock.Text.Contains("┌"))
                    return true;
            }
            return false;
        }

        private void DisplayErrorMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        private void DisplayWarningMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        private void DisplayInfoMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
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
        public List<string> GetAllTableInput()
        {
            UIElementCollection grids = spGridTable.Children;
            List<string> ret = new List<string>();
            foreach (var row in grids)
            {
                if (row is TextBlock block)
                {
                    ret.Add(block.Text);
                }
                else //grid
                {
                    Grid g = row as Grid;
                    foreach (var child in g.Children)
                    {
                        if (child is TextBox textbox)
                        {
                            ret.Add(textbox.Text);
                        }
                        if (child is ComboBox combobox)
                        {
                            ret.Add(combobox.Text);
                        }
                    }
                }
            }
            return ret;
        }
        private bool FileInUse(string file)
        {
            try
            {
                using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                DisplayErrorMsg($"Error: Fail to open {file}\nPlease close all file instance of file and try again", "Open failed");
                return true;
            }
            return false;
        }
        #endregion UTILITY

        #region KEYBOARD_FUNC
        private void AppendKeyboardChar(TextBox tb, string sign)
        {
            int cursor_location = tb.SelectionStart;
            tb.Text = tb.Text.Insert(tb.SelectionStart, sign);
            elementWithFocus.Focus();
            elementWithFocus.Select(cursor_location + sign.Length, 0);
        }
        private void GridKeyboard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox temp)
                elementWithFocus = temp;
        }
        #endregion KEYBOARD_FUNC

        #region INPUT_CHECKS
        private bool IsOperator(char c)
        {
            return c == '^' || c == 'v' || c == '|' || c == '¬' || c == '~' ||
                c == '∧' || c == '→' || c == '∨' || c == '↔' || c == '⊢' ||
                c == '⊥' || c == '=';
        }
        public void Expression_Error(int row, string error, int index = -1)
        {
            string error_message = "Error on row: " + row;
            if (row == -1)
            {
                error_message = "Error on logical expession given:";
            }
            if (index != -1)
            {
                error_message += " index: " + index;
            }
            error_message += "\nError is: " + error;

            DisplayErrorMsg(error_message, "Expression Check");
        }
        public bool IsValidExpression(string input, int row)
        {
            int parentheses_count = 0;
            bool after_operator = false;
            bool after_predicate = false;
            int i = 0;

            //Check if input is empty
            if (input.Length == 0)
            {
                Expression_Error(row, "No input", i);
                return false;
            }

            for (; i < input.Length; i++)
            {
                char c = input[i];

                //Ignore spaces
                if (Char.IsWhiteSpace(c))
                    continue;

                if (Char.IsNumber(c))
                {
                    if (i > 0)
                    {
                        if (input[i] == '0' && (input[i - 1] == 'X' || input[i - 1] == 'Y'))
                            continue;
                    }
                    Expression_Error(row, "Entering digits is not allowed, problematic char is: " + c, i);
                    return false;
                }

                //Open parentheses
                else if (c == '(')
                {
                    if (i != 0 && input[i - 1] == ')')
                    {
                        Expression_Error(row, "Missing an operator, problematic char is: " + c, i);
                        return false;
                    }
                    after_operator = true;
                    after_predicate = false;
                    parentheses_count++;
                }
                //Close parentheses
                else if (c == ')')
                {
                    if (after_operator)
                    {
                        Expression_Error(row, "Two operators in a row, problematic char is: " + c, i);
                        return false;
                    }
                    if (after_predicate)
                    {
                        Expression_Error(row, "Cant put ')' after predicate sign ", i);
                        return false;
                    }
                    parentheses_count--;
                    if (parentheses_count < 0)
                    {
                        Expression_Error(row, "Too many closing parentheses, problematic char is: " + c, i);
                        return false;
                    }
                }
                else if (c == '∀' || c == '∃')
                {
                    if (after_predicate)
                    {
                        Expression_Error(row, "Two predicate symbols in a row, problematic char is: " + c, i);
                        return false;
                    }
                    after_predicate = true;
                    after_operator = false;
                }
                else if (IsOperator(c))
                {
                    if (after_operator)
                    {
                        if (c != '~' && c != '¬')
                        {
                            Expression_Error(row, "Two operators in a row, problematic char is: " + c, i);
                            return false;
                        }
                    }
                    after_operator = true;
                    after_predicate = false;
                }
                else if (Char.IsLetter(c))
                {
                    if (!after_operator && !after_predicate && i != 0)
                    {
                        Expression_Error(row, "Two variables in a row, problematic char is: " + c, i);
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
                    after_predicate = after_operator = false;
                }
                else if (c == ',')
                {
                    Expression_Error(row, "An invalid character input, problematic char is: " + c, i);
                    return false;
                }
                else
                {
                    Expression_Error(row, "An invalid character input, problematic char is: " + c, i);
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
            string[] splitted = seg.Split(new Char[] { ',', '-' });
            if (seg.Contains('-') && splitted.Length > 2)

            foreach (var s in splitted)
            {
                if (!Int32.TryParse(s, out _))
                    return false;
            }
            return true;
        }
        #endregion INPUT_CHECKS

        #region TESTING
        public void PrintInputList(List<string> l)
        {
            foreach (string t in l)
            {
                Console.WriteLine(t);
            }
        }

        #endregion TESTING       


    }
}
