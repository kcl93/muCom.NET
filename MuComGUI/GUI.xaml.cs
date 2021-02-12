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
using OxyPlot.Axes;
using OxyPlot.Series;
using MuCom;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MuComGUI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class GUI : Window, INotifyPropertyChanged
    {
        #region Property changed

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region Properties

        readonly object readLock = new object();

        readonly object waitLock = new object();

        Timer Timer = null;

        Dispatcher dispatcher = null;

        DispatcherTimer GraphTimer = null;

        MuComHandler muCom = null;

        public List<VariableInfo> TargetVariables { get; } = new List<VariableInfo>();

        public List<VariableInfo> OwnVariables { get; } = new List<VariableInfo>();

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

        private DateTime graphStartTime;

        readonly private PlotModel graphModel = new PlotModel();

        readonly private Dictionary<VariableInfo, List<DataPoint>> DataPoints = new Dictionary<VariableInfo, List<DataPoint>>();

        #endregion

        #region Constructor

        public GUI()
        {
            InitializeComponent();

            this.DataContext = this;

            //Get/create dispatcher for doing stuff in the GUI
            this.dispatcher = Dispatcher.CurrentDispatcher;

            //Create timers for updating variables and the variable graph
            this.GraphTimer = new DispatcherTimer(DispatcherPriority.Background);
            this.GraphTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            this.GraphTimer.Tick += new EventHandler(this.UpdateGraph);

            this.Timer = new Timer(new TimerCallback(this.UpdateData));

            //Link variables to screen
            this.SerialPorts.ItemsSource = SerialPort.GetPortNames();
            this.TargetVariablesGrid.ItemsSource = this.TargetVariables;
            this.OwnVariablesGrid.ItemsSource = this.OwnVariables;

            //Create empty graph
            this.graphModel.Axes.Add(new TimeSpanAxis() { Position = AxisPosition.Bottom });
            this.graphModel.Axes.Add(new LinearAxis() { Position = AxisPosition.Left });
            this.Graph.Model = this.graphModel;
        }

        #endregion

        #region Methods

        public void UpdateData(object sender)
        {
            if (Monitor.TryEnter(this.waitLock, 0) == false) return;

            //Only one additional thread can wait here

            lock(this.readLock)
            {
                Monitor.Exit(this.waitLock);

                if (this.muCom is null) return;

                foreach (var data in this.DataPoints)
                {
                    var value = this.muCom.Read((byte)data.Key.ID, 1);
                    var timestamp = DateTime.Now - this.graphStartTime;
                    //if (data.Value.Count >= 800)
                    //{

                    //}
                    //data.Value[data.Value.Count - 1] = new DataPoint();
                }
            }
        }

        public void UpdateGraph(object sender, EventArgs e)
        {
            if(this.GraphActive.IsChecked == true)
            {
                this.graphModel.Series.Clear();
                foreach (var data in this.DataPoints)
                {
                    var series = new LineSeries();
                    series.Title = "Addr " + data.Key.ID.ToString();
                    series.ItemsSource = data.Value;
                    this.graphModel.Series.Add(series);
                }
                this.Graph.InvalidatePlot(true);
            }
        }

        #endregion

        #region GUI callbacks

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.muCom = new MuComHandler(this.SerialPorts.Text, this.BaudRate);
                this.muCom.Open();
                this.OpenButton.IsEnabled = false;
                this.CloseButton.IsEnabled = true;
            }
            catch
            {
                this.CloseButton_Click(sender, e);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.GraphActive.IsChecked = false;
                this.muCom.Close();
                this.OpenButton.IsEnabled = true;
                this.CloseButton.IsEnabled = false;
            }
            finally
            {
                this.muCom = null;
            }
        }

        private void NumericTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var allowedKeys = new List<Key>() { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9 };
            if(allowedKeys.Contains(e.Key) == false)
            {
                e.Handled = true;
            }
        }

        private void GraphActive_Checked(object sender, RoutedEventArgs e)
        {
            this.DataPoints.Clear();
            this.graphStartTime = new DateTime();

            foreach (var variable in this.TargetVariables)
            {
                if (variable.Plot == true)
                {
                    this.DataPoints.Add(variable, new List<DataPoint>() { new DataPoint(0.0, variable.ToDouble()) });
                }
            }

            foreach (var variable in this.OwnVariables)
            {
                if (variable.Plot == true)
                {
                    this.DataPoints.Add(variable, new List<DataPoint>() { new DataPoint(0.0, variable.ToDouble()) });
                }
            }
        }

        private void ReadVariableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (sender as Button);
                if (button is null) return;

                var variable = (sender as Button).DataContext as VariableInfo;
                if (variable is null) return;

                variable.Read(this.muCom);
            }
            catch
            {

            }
        }

        private void WriteVariableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (sender as Button);
                if (button is null) return;

                var variable = (sender as Button).DataContext as VariableInfo;
                if (variable is null) return;

                variable.Write(this.muCom);
            }
            catch
            {

            }
        }

        #endregion
    }
}
