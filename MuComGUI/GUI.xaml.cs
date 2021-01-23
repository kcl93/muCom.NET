using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;
using System.IO.Ports;
using OxyPlot;
using OxyPlot.Wpf;
using MuCom;

namespace MuComGUI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class GUI : Window
    {
        #region Properties

        Timer Timer = null;

        Dispatcher dispatcher = null;

        DispatcherTimer GraphTimer = null;

        MuComHandler muCom = null;

        int BaudRate
        {
            get
            {
                if(int.TryParse(this.TBBaudrate.Text, out int value) == true)
                {
                    return value;
                }
                this.BaudRate = 250000;
                return 250000;
            }
            set => this.TBBaudrate.Text = Math.Max(value, 1).ToString();
        }

        int UpdateRate
        {
            get
            {
                if (int.TryParse(this.TBUpdateRate.Text, out int value) == true)
                {
                    return value;
                }
                this.UpdateRate = 1000;
                return 1000;
            }
            set
            {
                this.Timer.Change(0, Math.Max(value, 1));
                this.TBUpdateRate.Text = Math.Max(value, 1).ToString();
            }
        }

        readonly Dictionary<int, List<DataPoint>> DataPoints = new Dictionary<int, List<DataPoint>>();

        #endregion

        #region Constructor

        public GUI()
        {
            InitializeComponent();

            this.dispatcher = Dispatcher.CurrentDispatcher;

            this.GraphTimer = new DispatcherTimer(DispatcherPriority.Background);
            this.GraphTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            this.GraphTimer.Tick += new EventHandler(this.UpdateGraph);

            this.Timer = new Timer(new TimerCallback(this.UpdateData));

            this.SerialPorts.ItemsSource = SerialPort.GetPortNames();
        }

        #endregion

        #region Methods

        public void UpdateData(object sender)
        {
            if (this.muCom is null) return;

            foreach(var data in this.DataPoints)
            {
                //var value = this.muCom.Read();
                //var timestamp = this.
                //if (data.Value.Count >= 800)
                //{

                //}
                //data.Value[data.Value.Count - 1] = new DataPoint;
            }
        }

        public void UpdateGraph(object sender, EventArgs e)
        {
            if(this.GraphActive.IsChecked == true)
            {
                this.Graph.Series.Clear();
                foreach (var data in this.DataPoints)
                {
                    var series = new LineSeries();
                    series.Name = "Addr " + data.Key.ToString();
                    series.ItemsSource = data.Value;
                    this.Graph.Series.Add(series);
                }
                this.Graph.InvalidatePlot();
            }
        }

        #endregion

        #region GUI callbacks

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NumericTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var allowedKeys = new List<Key>() { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9 };
            if(allowedKeys.Contains(e.Key) == false)
            {
                e.Handled = true;
            }
        }

        #endregion
    }
}
