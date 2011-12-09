using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Xml;

namespace PitchPitch
{
    class Config
    {
        private double _maxFreq = 250;
        public double MaxFreq
        {
            get { return _maxFreq; }
            set { _maxFreq = value; }
        }
        private double _minFreq = 150;
        public double MinFreq
        {
            get { return _minFreq; }
            set { _minFreq = value; }
        }

        private string _deviceId = null;
        public string DeviceId
        {
            get { return _deviceId; }
            set { _deviceId = value; }
        }

        public static string FilePath
        {
            get
            {
                string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                dirPath = Path.Combine(dirPath, Properties.Resources.Dirname_Config);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                return Path.Combine(dirPath, "config.xml");
            }
        }

        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    string filePath = FilePath;
                    if (File.Exists(filePath))
                        _instance = read(FilePath);
                    else
                        _instance = new Config();
                    if (_instance == null) _instance = new Config();
                }
                return _instance;
            }
        }

        private Config()
        {
        }

        public void Save()
        {
            write(FilePath);
        }

        private void write(string filePath)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.CloseOutput = true;
                settings.CheckCharacters = true;
                settings.ConformanceLevel = ConformanceLevel.Document;
                settings.Encoding = Encoding.UTF8;
                settings.Indent = true;
                settings.IndentChars = "\t";
                settings.NewLineChars = "\r\n";

                using (XmlWriter writer = XmlWriter.Create(filePath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Config");

                    writer.WriteStartElement("FrequencyRange");
                    writer.WriteAttributeString("min", _minFreq.ToString());
                    writer.WriteAttributeString("max", _maxFreq.ToString());
                    writer.WriteEndElement();

                    writer.WriteElementString("DeviceID", _deviceId);

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogType.Error, "設定ファイルの書き込みに失敗: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private static Config read(string filePath)
        {
            try
            {
                Config c = new Config();

                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);

                XmlNodeList lst = doc.GetElementsByTagName("Config");
                if (lst.Count == 0) return c;
                XmlNode root = lst[0];

                foreach (XmlNode node in root.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "FrequencyRange":
                            {
                                double min = 86; double max = 1000;
                                foreach (XmlAttribute attr in node.Attributes)
                                {
                                    switch (attr.Name)
                                    {
                                        case "min":
                                            if (double.TryParse(attr.InnerText.Trim(), out min)) c.MinFreq = min;
                                            break;
                                        case "max":
                                            if (double.TryParse(attr.InnerText.Trim(), out max)) c.MaxFreq = max;
                                            break;
                                    }
                                }
                            }
                            break;
                        case "DeviceID":
                            c.DeviceId = node.InnerText.Trim();
                            break;
                    }
                }

                return c;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log(LogType.Error, "設定ファイルの読み込みに失敗: " + ex.Message + "\r\n" + ex.StackTrace);
                return null;
            }
        }
    }
}
