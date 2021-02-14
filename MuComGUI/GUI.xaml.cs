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
using System.Xml;

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
                this.Timer?.Change(0, Math.Max(value, 1));
                this.TBUpdateRate.Text = Math.Max(value, 1).ToString();
            }
        }

        public static DateTime graphStartTime = DateTime.Now;

        public static int GraphValueCount = 1000;

        readonly private PlotModel graphModel = new PlotModel();

        #endregion

        #region Constructor

        public GUI()
        {
            InitializeComponent();

            //Get list of available COM ports
            this.SerialPorts.ItemsSource = SerialPort.GetPortNames();

            //Get/create dispatcher for doing stuff in the GUI
            this.dispatcher = Dispatcher.CurrentDispatcher;

            //Create timers for updating variables and the variable graph
            this.GraphTimer = new DispatcherTimer(DispatcherPriority.Background);
            this.GraphTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            this.GraphTimer.Tick += new EventHandler(this.UpdateGraph);

            //Load config file
            this.ReadXml();

            //Link variables to screen
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

                foreach(var variable in this.TargetVariables)
                {
                    if(variable.Plot == true)
                    {
                        try
                        {
                            variable.Read(this.muCom);
                        }
                        catch
                        {

                        }
                    }
                }

                foreach (var variable in this.OwnVariables)
                {
                    if (variable.Plot == true)
                    {
                        try
                        {
                            if(double.TryParse(variable.Value, out double value) == false)
                            {
                                value = double.NaN;
                            }
                            variable.AddDataPoint(value);
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }

        public void UpdateGraph(object sender, EventArgs e)
        {
            if(this.GraphActive.IsChecked == true)
            {
                this.graphModel.Series.Clear();
                foreach (var variable in this.TargetVariables)
                {
                    if (variable.Plot == true)
                    {
                        var series = new LineSeries();
                        series.Title = "Target " + variable.ID.ToString();
                        series.ItemsSource = variable.DataPoints;
                        this.graphModel.Series.Add(series);
                    }
                }
                foreach (var variable in this.OwnVariables)
                {
                    if (variable.Plot == true)
                    {
                        var series = new LineSeries();
                        series.Title = "Target " + variable.ID.ToString();
                        series.ItemsSource = variable.DataPoints;
                        this.graphModel.Series.Add(series);
                    }
                }
                this.Graph.InvalidatePlot(true);
            }
        }

        private void ReadXml()
        {
            try
            {
                var reader = XmlReader.Create("MuComGUI.xml");

                if (reader.ReadToFollowing("MuComGUIConfig") == true)
                {
                    //Serial port
                    if (reader.ReadToFollowing("SerialPort") == true)
                    {
                        var port = reader.ReadElementContentAsString();
                        if ((this.SerialPorts.ItemsSource as string[])?.Contains(port) == true)
                        {
                            this.SerialPorts.SelectedItem = port;
                        }
                    }

                    //Graph update rate
                    if (reader.ReadToFollowing("UpdateRate") == true)
                    {
                        if (int.TryParse(reader.ReadElementContentAsString(), out int rate) == true)
                        {
                            this.UpdateRate = rate;
                        }
                    }

                    //Graph update rate
                    if (reader.ReadToFollowing("GraphValueCount") == true)
                    {
                        if (int.TryParse(reader.ReadElementContentAsString(), out int count) == true)
                        {
                            GUI.GraphValueCount = count;
                        }
                    }

                    //Target variables
                    if (reader.ReadToFollowing("TargetVariables") == true)
                    {
                        if ((reader.IsStartElement() == true) && (reader.IsEmptyElement == false))
                        {
                            reader.Read();
                            while (reader.Read() == true)
                            {
                                //reader.Read();
                                if (reader.Name == "TargetVariables")
                                {
                                    break;
                                }
                                if (reader.Name == "Variable")
                                {
                                    var variable = new VariableInfo();
                                    if (byte.TryParse(reader.GetAttribute("ID"), out byte ID) == true)
                                    {
                                        variable.ID = ID;
                                    }
                                    variable.Value = reader.GetAttribute("Value");
                                    variable.VariableTypeName = reader.GetAttribute("Type");
                                    if (bool.TryParse(reader.GetAttribute("Plot"), out bool plot) == true)
                                    {
                                        variable.Plot = plot;
                                    }
                                    this.TargetVariables.Add(variable);
                                }
                            }
                        }
                    }

                    //Own variables
                    if (reader.ReadToFollowing("OwnVariables") == true)
                    {
                        if ((reader.IsStartElement() == true) && (reader.IsEmptyElement == false))
                        {
                            reader.Read();
                            while (reader.Read() == true)
                            {
                                //reader.Read();
                                if (reader.Name == "OwnVariables")
                                {
                                    break;
                                }
                                if (reader.Name == "Variable")
                                {
                                    var variable = new VariableInfo();
                                    if (byte.TryParse(reader.GetAttribute("ID"), out byte ID) == true)
                                    {
                                        variable.ID = ID;
                                    }
                                    variable.Value = reader.GetAttribute("Value");
                                    variable.VariableTypeName = reader.GetAttribute("Type");
                                    if (bool.TryParse(reader.GetAttribute("Plot"), out bool plot) == true)
                                    {
                                        variable.Plot = plot;
                                    }
                                    this.OwnVariables.Add(variable);
                                }
                            }
                        }
                    }
                }

            }
            catch
            {

            }
        }

        private void WriteXml()
        {
            //Create writer
            var writer = XmlWriter.Create("MuComGUI.xml", new XmlWriterSettings() { Indent = true });
            if (writer != null)
            {
                //Write data
                writer.WriteStartElement("MuComGUIConfig");

                //Serial port
                writer.WriteElementString("SerialPort", this.SerialPorts.SelectedItem?.ToString());

                //Graph update rate
                writer.WriteElementString("UpdateRate", this.UpdateRate.ToString());

                //Graph value count per variable
                writer.WriteElementString("GraphValueCount", GUI.GraphValueCount.ToString());

                //Target variables
                writer.WriteStartElement("TargetVariables");
                foreach (var variable in this.TargetVariables)
                {
                    writer.WriteStartElement("Variable");
                    writer.WriteAttributeString("ID", variable.ID.ToString());
                    writer.WriteAttributeString("Value", variable.Value);
                    writer.WriteAttributeString("Type", variable.VariableTypeName);
                    writer.WriteAttributeString("Plot", variable.Plot.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                //Own variables
                writer.WriteStartElement("OwnVariables");
                foreach (var variable in this.OwnVariables)
                {
                    writer.WriteStartElement("Variable");
                    writer.WriteAttributeString("ID", variable.ID.ToString());
                    writer.WriteAttributeString("Value", variable.Value);
                    writer.WriteAttributeString("Type", variable.VariableTypeName);
                    writer.WriteAttributeString("Plot", variable.Plot.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteEndElement();

                //Write data to file
                writer.Flush();
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
                this.muCom?.Close();
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
            var allowedKeys = new List<Key>() { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9 };
            if(allowedKeys.Contains(e.Key) == false)
            {
                e.Handled = true;
            }
        }

        private void GraphActive_Checked(object sender, RoutedEventArgs e)
        {
            GUI.graphStartTime = DateTime.Now;
            this.GraphTimer.Start();
            this.Timer = new Timer(this.UpdateData, null, 0, this.UpdateRate);
        }

        private void GraphActive_Unchecked(object sender, RoutedEventArgs e)
        {
            this.Timer.Dispose();
            this.Timer = null;
            this.GraphTimer.Stop();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.CloseButton_Click(null, null);
            this.WriteXml();
        }

        #endregion
    }
}
