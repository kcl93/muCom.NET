using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MuComGUI
{
    public static class ConfigHandler
    {

        public static void ReadXml(GUI target)
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
                        if ((target.SerialPorts.ItemsSource as string[])?.Contains(port) == true)
                        {
                            target.SerialPorts.SelectedItem = port;
                        }
                    }

                    //Baudrate
                    if (reader.ReadToFollowing("Baudrate") == true)
                    {
                        if (int.TryParse(reader.ReadElementContentAsString(), out int baudrate) == true)
                        {
                            target.BaudRate = baudrate;
                        }
                    }

                    //Graph update rate
                    if (reader.ReadToFollowing("UpdateRate") == true)
                    {
                        if (int.TryParse(reader.ReadElementContentAsString(), out int rate) == true)
                        {
                            target.UpdateRate = rate;
                        }
                    }

                    //Graph value count per variable
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
                                    target.TargetVariables.Add(variable);
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
                                    target.OwnVariables.Add(variable);
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

        public static void WriteXml(GUI target)
        {
            //Create writer
            var writer = XmlWriter.Create("MuComGUI.xml", new XmlWriterSettings() { Indent = true });
            if (writer != null)
            {
                //Write data
                writer.WriteStartElement("MuComGUIConfig");

                //Serial port
                writer.WriteElementString("SerialPort", target.SerialPorts.SelectedItem?.ToString());

                //Baudrate
                writer.WriteElementString("Baudrate", target.BaudRate.ToString());

                //Graph update rate
                writer.WriteElementString("UpdateRate", target.UpdateRate.ToString());

                //Graph value count per variable
                writer.WriteElementString("GraphValueCount", GUI.GraphValueCount.ToString());

                //Target variables
                writer.WriteStartElement("TargetVariables");
                foreach (var variable in target.TargetVariables)
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
                foreach (var variable in target.OwnVariables)
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
    }
}
