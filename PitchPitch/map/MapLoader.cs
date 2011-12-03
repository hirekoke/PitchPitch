using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.IO;
using System.Xml;

namespace PitchPitch.map
{
    class MapLoader
    {
        public List<MapInfo> LoadMapInfos()
        {
            List<MapInfo> ret = new List<MapInfo>();

            if (!Directory.Exists(Properties.Resources.MapDir)) return ret;

            DirectoryInfo mapDir = new DirectoryInfo(Properties.Resources.MapDir);
            
            DirectoryInfo[] dirInfos = mapDir.GetDirectories("*", SearchOption.TopDirectoryOnly);
            foreach (DirectoryInfo dir in dirInfos)
            {
                MapInfo info = LoadMapInfo(dir.FullName);
                if(info != null) ret.Add(info);
            }

            return ret;
        }

        public MapInfo LoadMapInfo(string dirPath)
        {
            MapInfo mi = new MapInfo();

            string mapXmlFile = Path.Combine(dirPath, "map.xml");
            if (!File.Exists(mapXmlFile)) return null;

            XmlDocument mapXml = new XmlDocument();
            mapXml.Load(mapXmlFile);

            XmlElement rootElem = mapXml["Map"];
            if (rootElem == null) return null;

            mi.DirPath = dirPath;
            mi.Id = Path.GetFileName(dirPath);

            mi.Name = rootElem["Name"] == null ? "" : rootElem["Name"].InnerText.Trim();

            #region Size
            {
                XmlElement sizeElem = rootElem["Size"];
                if (sizeElem != null)
                {
                    int width = 0; int height = 0;
                    foreach (XmlNode node in sizeElem.ChildNodes)
                    {
                        switch (node.Name)
                        {
                            case "Width":
                                int.TryParse(node.InnerText.Trim(), out width);
                                break;
                            case "Height":
                                int.TryParse(node.InnerText.Trim(), out height);
                                break;
                        }
                    }
                    mi.Size = new Size(width, height);
                }
            }
            #endregion

            #region Source
            XmlElement srcElem = rootElem["Source"];
            if (srcElem == null)
            {
                mi.MapSourceType = MapSourceType.None;
            }
            else
            {
                #region SourceType
                try
                {
                    string mt = srcElem["Type"] == null ? "" : srcElem["Type"].InnerText.Trim();
                    mi.MapSourceType = (MapSourceType)Enum.Parse(typeof(MapSourceType), mt, true);
                }
                catch (ArgumentException)
                {
                    mi.MapSourceType = MapSourceType.None;
                }
                #endregion

                mi.FileName = srcElem["Name"] == null ? "" : srcElem["Name"].InnerText.Trim();
                mi.Mapping = srcElem["Mapping"] == null ? "" : srcElem["Mapping"].InnerText.Trim();
            }
            #endregion

            #region Chip
            XmlElement chipElem = rootElem["Chip"];
            if (chipElem == null)
            {
                mi.ChipType = MapChipType.Builtin;
                mi.ChipFileName = "";
                mi.BuiltinChipName = MapChipBuiltinType.Binary;
            }
            else
            {
                #region ChipType
                try
                {
                    string ct = chipElem["Type"] == null ? "" : chipElem["Type"].InnerText.Trim();
                    mi.ChipType = (MapChipType)Enum.Parse(typeof(MapChipType), ct, true);
                }
                catch (ArgumentException)
                {
                    mi.ChipType = MapChipType.None;
                }
                #endregion

                #region Name
                if (mi.ChipType == MapChipType.Builtin)
                {
                    string cnt = chipElem["Name"] == null ? "" : chipElem["Name"].InnerText.Trim();
                    try
                    {
                        mi.BuiltinChipName = (MapChipBuiltinType)Enum.Parse(typeof(MapChipBuiltinType), cnt, true);
                    }
                    catch (ArgumentException)
                    {
                        mi.BuiltinChipName = MapChipBuiltinType.None;
                    }
                }
                else
                {
                    mi.ChipFileName = chipElem["Name"] == null ? "" : chipElem["Name"].InnerText.Trim();
                    mi.BuiltinChipName = MapChipBuiltinType.None;
                }
                #endregion

                #region Size
                XmlElement sizeElem = chipElem["Size"];
                if (sizeElem != null)
                {
                    int width = 0; int height = 0;
                    foreach (XmlNode node in sizeElem.ChildNodes)
                    {
                        switch (node.Name)
                        {
                            case "Width":
                                int.TryParse(node.InnerText.Trim(), out width);
                                break;
                            case "Height":
                                int.TryParse(node.InnerText.Trim(), out height);
                                break;
                        }
                    }
                    mi.ChipSize = new Size(width, height);
                }
                else
                {
                    mi.ChipSize = Size.Empty;
                }
                #endregion
            }
            #endregion

            #region Player
            XmlElement playerElem = rootElem["Player"];
            if (playerElem != null)
            {
                int vx = 1;
                string vxt = playerElem["Vx"] == null ? "1" : playerElem["Vx"].InnerText.Trim();
                if (int.TryParse(vxt, out vx)) mi.PlayerVx = vx;
            }
            #endregion

            #region Colors
            XmlElement colorElem = rootElem["Color"];
            if (colorElem != null)
            {
                mi.BackColor = colorElem["Background"] == null ? Color.White : ImageManager.GetColor(colorElem["Background"].InnerText.Trim());
                mi.StrongColor = colorElem["Strong"] == null ? Color.Red : ImageManager.GetColor(colorElem["Strong"].InnerText.Trim());
                mi.ForeColor = colorElem["Foreground"] == null ? Color.Black : ImageManager.GetColor(colorElem["Foreground"].InnerText.Trim());
            }
            #endregion

            if (isValidMapInfo(mi)) return mi;
            return null;
        }

        private bool isValidMapInfo(MapInfo info)
        {
            if (info.MapSourceType == MapSourceType.None) return false;
            if (info.ChipType == MapChipType.None) return false;

            if (info.ChipType == MapChipType.Image)
            {
                if (info.ChipSize == Size.Empty) return false;
                if (string.IsNullOrEmpty(info.ChipFileName)) return false;
                if (!File.Exists(Path.Combine(info.DirPath, info.ChipFileName))) return false;
            }
            else if (info.ChipType == MapChipType.Builtin)
            {
                if (info.BuiltinChipName == MapChipBuiltinType.None) return false;
            }

            if (string.IsNullOrEmpty(info.Name)) return false;
            if (string.IsNullOrEmpty(info.FileName)) return false;

            if (!File.Exists(Path.Combine(info.DirPath, info.FileName))) return false;

            return true;
        }

        public Map LoadMap(MapInfo info)
        {
            Map map = null;
            MapChipData chipData = null;

            switch (info.ChipType)
            {
                case MapChipType.Builtin:
                    {
                        switch (info.BuiltinChipName)
                        {
                            case MapChipBuiltinType.Binary:
                                map = new BinaryMap();
                                break;
                        }
                    }
                    break;
                case MapChipType.Image:
                    {
                        chipData = ImageMapChipData.LoadChipData(info);
                        map = new BasicMap();
                        map.ChipData = chipData;
                    }
                    break;
            }

            switch (info.MapSourceType)
            {
                case MapSourceType.Image:
                    {
                        string srcPath = Path.Combine(info.DirPath, info.FileName);
                        string mappingPath = Path.Combine(info.DirPath, info.Mapping);
                        using (Bitmap srcBmp = (Bitmap)Bitmap.FromFile(srcPath))
                        {
                            if (!string.IsNullOrEmpty(info.Mapping) && File.Exists(mappingPath))
                            {
                                using (Bitmap mappingBmp = (Bitmap)Bitmap.FromFile(mappingPath))
                                {
                                    map.LoadMapImage(srcBmp, mappingBmp);
                                }
                            }
                            else
                            {
                                map.LoadMapImage(srcBmp, null);
                            }
                        }
                    }
                    break;
            }

            map.MapInfo = info;
            return map;
        }
    }
}
