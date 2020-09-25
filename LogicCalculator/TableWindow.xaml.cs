using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LogicCalculator
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class TableWindow : Window
    {
        public class SensorInput
        {
            public SensorInput(bool a, bool b, bool c)
            {
                A = a;
                B = b;
                C = c;
            }

            public bool A { get; private set; }
            public bool B { get; private set; }
            public bool C { get; private set; }

            public bool Output
            {
                // output logic goes here
                get { return A || B || C; }
            }
        }

        public TableWindow()
        {
            DataGrid dg = new DataGrid();
            InitializeComponent();
            var inputs = new List<SensorInput>()
            {
                new SensorInput(true, true, true),
                new SensorInput(true, true, false),
                new SensorInput(true, false, true),
                new SensorInput(true, false, false),
                new SensorInput(false, true, true),
                new SensorInput(false, true, false),
                new SensorInput(false, false, true),
                new SensorInput(false, false, false)
            };

            dg.ItemsSource = inputs;
        }
    }
}