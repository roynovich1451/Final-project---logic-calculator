﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Compilation;
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
        const int COL_TEXTBLOCK_WIDTH = 420;
        const int CHILD_MARGIN = 4;
        const int THICKNESS = 2;
        const int SPACES = 6;
        const int HYPHEN = 8;
        const int MAX_HYPHEN_CHUNKS = 10;
        const int MIN_HYPHEN_CHUNKS = 1;
        const int CHECKBOX_INDEX = 0;
        const int LABL_INDEX = 1;
        const int STATEMENT_INDEX = 2;
        const int COMBOBOX_INDEX = 3;
        const int SEGMENT1_INDEX = 4;
        const int SEGMENT2_INDEX = 5;
        const int SEGMENT3_INDEX = 6;
        const int TEXT_BLOCK_INDEX = 1;
        enum BoxState
        {
            Open,
            Close
        }
        #endregion

        #region VARIABLES
        private int checked_checkboxes = 0;
        readonly List<Statement> statement_list = new List<Statement>();
        private int table_row_num = 0;
        private static readonly int TABLE_COL_NUM = 6;
        TextBox elementWithFocus;
        private readonly List<string> rules = new List<string> { "Data", "Assumption", "LEM", "PBC", "MP", "MT", "Copy"
                                                                 ,"∧i", "∧e1", "∧e2", "∨i1", "∨i2", "∨e", "¬¬e",
                                                                 "¬¬i", "→i", "⊥e", "¬i", "¬e", "→i"};
        private int hyphen_chunks = MAX_HYPHEN_CHUNKS;
        private int spaces_chunks = MIN_HYPHEN_CHUNKS;
        private int box_closers = 0;
        private int box_openers = 0;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
        }

        #region MENUBAR_CLICKS
        private void MenuItemNew_Click(object sender, RoutedEventArgs e)
        {
            spGridTable.Children.Clear();
            table_row_num = 0;
            hyphen_chunks = MAX_HYPHEN_CHUNKS;
            spaces_chunks = MIN_HYPHEN_CHUNKS;
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
                    createRow(-1);
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
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.ShowDialog();
            string saveFileOutput = saveFileDialog.FileName;
            if (string.IsNullOrEmpty(saveFileOutput)) return;
            string file_name = Path.GetFileNameWithoutExtension(saveFileOutput);
            string path = Path.GetDirectoryName(saveFileOutput);
            
            //string file_name = "wow";//TODO: get this from somewhere
            //string path = "C:\\Oren\\Final_project_logic_calculator";//TODO: get this from somewhere


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
                document.InsertParagraph("Logic Tool Results\n").FontSize(16d).Bold(true).UnderlineStyle(UnderlineStyle.singleLine);

                //Add the proof table
                switch (((TabItem)mainTab.SelectedItem).Header)
                {
                    case "Logical proof":
                        //Add the main expression
                        document.InsertParagraph("Logical Expression: " + tbValue.Text + '\n').FontSize(14d);

                        Table proof_table = document.AddTable(table_row_num + 1, TABLE_COL_NUM);
                        proof_table.Alignment = Alignment.center;

                        proof_table.Rows[0].Cells[0].Paragraphs.First().Append("Line").UnderlineStyle(UnderlineStyle.singleLine);
                        proof_table.Rows[0].Cells[1].Paragraphs.First().Append("Expression").UnderlineStyle(UnderlineStyle.singleLine);
                        proof_table.Rows[0].Cells[2].Paragraphs.First().Append("Rule").UnderlineStyle(UnderlineStyle.singleLine);
                        proof_table.Rows[0].Cells[3].Paragraphs.First().Append("First Segment").UnderlineStyle(UnderlineStyle.singleLine);
                        proof_table.Rows[0].Cells[4].Paragraphs.First().Append("Second Segment").UnderlineStyle(UnderlineStyle.singleLine);
                        proof_table.Rows[0].Cells[5].Paragraphs.First().Append("Third Segment").UnderlineStyle(UnderlineStyle.singleLine);

                        List<string> input_list = GetAllTableInput();
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
                        break;

                    case "Text editor":
                        document.InsertParagraph(tbEditor.Text).FontSize(12d);
                        break;
                }
                try
                {
                    document.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                MessageBox.Show("Created Document: " + path + "\\" + file_name + ".docx", "Documented Created"
                    , MessageBoxButton.OK, MessageBoxImage.Information);
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
        /*
        private void clearGridLine(int index)
        {
            if (spGridTable.Children[index] is TextBlock)
            {
                removeTextBlock(index);
                return;
            }
            Grid line = spGridTable.Children[index] as Grid;
            foreach (var child in line.Children)
            {
                if (child is TextBox)
                {
                    ((TextBox)child).Text = string.Empty;
                }
                if (child is ComboBox)
                {
                    ((ComboBox)child).SelectedIndex = -1;
                }
                if (child is CheckBox)
                {
                    ((CheckBox)child).IsChecked = false;
                }
            }
        }
        */
        private bool boxChecker(BoxState state)
        {
            switch (state)
            {
                case BoxState.Close:
                    if (box_closers >= box_openers)
                    {
                        displayMsg("Error: Can't be more box closers then box openers", "Error");
                        return false;
                    }
                    if (!hasBoxOpen())
                    {
                        displayMsg("Error: no opener found", "Error");
                        return false;
                    }
                    return true;

                case BoxState.Open:
                    if (hyphen_chunks <= MIN_HYPHEN_CHUNKS)
                    {
                        displayMsg("Error: You reached to maximum available boxes", "Error");
                        return false;
                    }
                    return true;

                default:
                    return false;
            }
        }
        private void handleGridVisability(Grid g, int needed)
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
                        if (Grid.GetColumn(child) == LABL_INDEX)
                        {
                            ((Label)child).Visibility = Visibility.Visible;
                        }
                        if (child is TextBox)
                        {
                            if (Grid.GetColumn(child) == LABL_INDEX)
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
                        if (Grid.GetColumn(child) == LABL_INDEX)
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
                        if (Grid.GetColumn(child) == LABL_INDEX)
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
                        if (Grid.GetColumn(child) == LABL_INDEX)
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
        private void handleLabelsAfterCheckMode()
        {
            int index = 0;
            foreach (Grid row in spGridTable.Children)
            {
                if (row.Children[LABL_INDEX] is Label)
                {
                    Label label = row.Children[LABL_INDEX] as Label;
                    label.Content = ++index;
                }
            }
            //re-initial variables
            if (spGridTable.Children.Count == 0)
                table_row_num = 0;
            else
                table_row_num = index - 1;
        }
        private List<Grid> getChecked()
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
        private Grid createBox(BoxState state)
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
            chb.Click += new RoutedEventHandler(chb_click);
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
            tbline.Text = createBoxLine(state);

            newLine.Children.Add(chb);
            newLine.Children.Add(tbline);
            return newLine;
        }
        private void checkMode(bool state)
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
        private bool removeTextBlock(int location)
        {
            /* TODO: NEED FIX TO NEW DESIGN 
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
            */
            return true;
        }
        private void handleBox(BoxState state)
        {
            if (!boxChecker(state)) return;
            switch (state)
            {
                case BoxState.Close:
                    spGridTable.Children.Add(createBox(BoxState.Close));
                    ++box_closers;
                    break;
                case BoxState.Open:
                    spGridTable.Children.Add(createBox(BoxState.Open));
                    ++box_openers;
                    break;
            }
        }
        private void createRow(int index)
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
            chb.Click += new RoutedEventHandler(chb_click);
            Grid.SetColumn(chb, CHECKBOX_INDEX);

            //label
            Label lb = new Label
            {
                Content = $"{table_row_num}",
                Margin = new Thickness(THICKNESS),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(lb, LABL_INDEX);

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
                Width = COL_SEGMENT_WIDTH - CHILD_MARGIN
            };
            cmbRules.SelectionChanged += new SelectionChangedEventHandler(cmb_SelectedValueChanged);
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
        }
        #endregion

        #region EVENTS
        private void chb_click(object sender, RoutedEventArgs e)
        {
            CheckBox chb = sender as CheckBox;
            if (chb.IsChecked == true)
            {
                ++checked_checkboxes;
            }
            else
            {
                --checked_checkboxes;
            }
            if (checked_checkboxes > 0)
            {
                checkMode(true);
            }
            else
            {
                checkMode(false);
            }
        }
        private void cmb_SelectedValueChanged(object sender, SelectionChangedEventArgs e)
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
                    handleGridVisability(parent, 0);
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
                case "¬e":
                case "¬i":
                    handleGridVisability(parent, 1);
                    //handleBox(cmb, location);
                    break;
                //2 seg
                case "MP":
                case "MT":
                case "∧i":
                    handleGridVisability(parent, 2);
                    break;
                //3 seg
                case "∨e":
                    handleGridVisability(parent, 3);
                    break;
                case "Close Box":
                    handleGridVisability(parent, -1);
                    //handleBox(cmb, location);
                    break;
                default:
                    handleGridVisability(parent, 0);
                    break;
            }
        }
        #endregion

        #region BUTTON_CLICKS
        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            createRow(-1);
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
        private void BtnNot_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "¬");
            }
        }
        private void btnPhi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "φ");
            }
        }
        private void btnChi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "χ");
            }
        }
        private void btnPsi_Click(object sender, RoutedEventArgs e)
        {
            if (elementWithFocus != null)
            {
                AppendKeyboardChar(elementWithFocus, "ψ");
            }
        }
        private void btnOpenBox_Click(object sender, RoutedEventArgs e)
        {
            handleBox(BoxState.Open);
        }
        private void btnCloseBox_Click(object sender, RoutedEventArgs e)
        {
            handleBox(BoxState.Close);
        }
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> checkedGrid = getChecked();
            MessageBoxResult res = MessageBox.Show($"Warning: You are about to CLEAR {checkedGrid.Count} lines\nPlease confirm",
                "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Cancel)
                return;
            foreach (Grid row in checkedGrid)
            {
                foreach (var child in row.Children)
                {
                    //if (child is TextBlock)
                    //{
                    //    spGridTable.Children.Remove(row);
                    //}
                    if (child is TextBox)
                    {
                        ((TextBox)child).Text = string.Empty;
                    }
                    if (child is ComboBox)
                    {
                        ((ComboBox)child).SelectedIndex = -1;
                    }
                    if (child is CheckBox)
                    {
                        ((CheckBox)child).IsChecked = false;
                    }
                }
            }
            //re-initial variables
            checked_checkboxes = 0;
            checkMode(false);
        }
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> checkedGrid = getChecked();
            MessageBoxResult res = MessageBox.Show($"Warning: You are about to REMOVE {checkedGrid.Count} lines\nPlease confirm",
                "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Cancel) return;
            foreach (Grid row in checkedGrid)
            {
                spGridTable.Children.Remove(row);
            }
            handleLabelsAfterCheckMode();
            //TODO: handlBoxesAfterRemove()!!
            checkMode(false);
        }
        private void btnAddBefore_Click(object sender, RoutedEventArgs e)
        {
            List<Grid> checkedGrid = getChecked();
            MessageBoxResult res = MessageBox.Show($"Warning: You are about to Add {checkedGrid.Count} lines\nPlease confirm",
                "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Cancel) return;
            foreach (Grid row in checkedGrid)
            {
                ((CheckBox)row.Children[CHECKBOX_INDEX]).IsChecked = false;
                int addToIndex = spGridTable.Children.IndexOf(row);
                createRow(addToIndex);
            }
            handleLabelsAfterCheckMode();
            //TODO: handlBoxesAfterRemove()!!
            checkMode(false);
            checked_checkboxes = 0;
        }
        #endregion

        #region UTILITY 
        public List<TextBox> GetAllTextBoxes()
        {
            UIElementCollection grids = spGridTable.Children;
            List<TextBox> ret = new List<TextBox>();
            foreach (var row in grids)
            {
                Grid g = row as Grid;
                foreach (var child in g.Children)
                {
                    if (child is TextBox)
                    {
                        ret.Add(((TextBox)child));
                    }
                    if (child is ComboBox)
                    {
                        TextBox t = new TextBox();
                        t.Text = ((ComboBox)child).Text;
                        ret.Add(t);
                    }
                }
            }
            return ret;
        }
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
        void HandleTableInput()
        {
            statement_list.Add(new Statement(tbValue.Text, "first", "0"));
            //List<string> text_boxes_list = GetAllTableInput();
            List<TextBox> text_boxes_list = GetAllTextBoxes();
            //One less column because of the line number column
            int col_to_check = TABLE_COL_NUM - 1;

            for (int i = 0; i < text_boxes_list.Count - col_to_check + 1; i += col_to_check)
            {   //Remove spaces
                string expression = text_boxes_list[i].Text.Replace(" ", string.Empty);
                string rule = text_boxes_list[i + 1].Text.Replace(" ", string.Empty);
                string first_segment = text_boxes_list[i + 2].IsEnabled ? text_boxes_list[i + 2].Text.Replace(" ", string.Empty) : null;
                string second_segment = text_boxes_list[i + 3].IsEnabled ? text_boxes_list[i + 3].Text.Replace(" ", string.Empty) : null;
                string third_segment = text_boxes_list[i + 4].IsEnabled ? text_boxes_list[i + 4].Text.Replace(" ", string.Empty) : null;

                if (!IsValidStatement(expression, rule, first_segment, second_segment, third_segment))
                    return;
                Statement s = new Statement(expression, rule, first_segment, second_segment, third_segment);
                statement_list.Add(s);
                Evaluation e = new Evaluation(statement_list, rule);
                if (!e.is_valid)
                    return;
            }
            MessageBox.Show("All input is valid");
        }
        private void displayMsg(string msg, string title)
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        private bool hasBoxOpen()
        {
            var searchIndex = spGridTable.Children.Count;
            int openers = 0;
            int closers = 0;
            for (int i = searchIndex - 1; i >= 0; --i)
            {
                if (spGridTable.Children[i] is TextBlock)
                {
                    TextBlock tb = spGridTable.Children[i] as TextBlock;
                    if (tb.Text.Contains("┌") && 
                        tb.Text.Length == (hyphen_chunks + 1)*HYPHEN + SPACES*(spaces_chunks - 1)) break;
                    if (tb.Text.Contains("┌")) ++openers;
                    if (tb.Text.Contains("└")) ++closers;
                }
            }
            return openers == closers ? true : false;
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
                if (row is TextBlock)
                {
                    ret.Add(((TextBlock)row).Text);
                } 
                else //grid
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
                
            }
            return ret;
        }
        #endregion

        #region KEYBOARD_FUNC
        private void AppendKeyboardChar(TextBox tb, string sign)
        {
            tb.Text += sign;
        }
        private void gridKeyboard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox temp)
                elementWithFocus = temp;
        }
        #endregion

        #region INPUT_CHECKS
        private bool IsValidStatement(string expression, string rule, string first_segment,
           string second_segment, string third_segment, int row)
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
            return c == '^' || c == 'v' || c == '|' || c == '¬' || c == '~' ||
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
        public void PrintInputList(List<string> l)
        {
            foreach (string t in l)
            {
                Console.WriteLine(t);
            }
        }
        #endregion testing


    }
}