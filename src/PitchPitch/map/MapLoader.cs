using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Xml;

using PitchPitch.audio;

namespace PitchPitch.map
{
    class MapLoadException : Exception
    {
        public MapLoadException(string message) : base(message) { }
        public MapLoadException(string message, Exception innerEx) : base(message, innerEx) { }
    }

    class MapLoadedEventArgs : EventArgs
    {
        public readonly Map Map;
        public MapLoadedEventArgs(Map map)
        {
            Map = map;
        }
    }
    delegate void MapLoadedEventHandler(object s, MapLoadedEventArgs e);
    class MapLoadCanceledEventArgs : EventArgs {
        public readonly Exception Exception;
        public MapLoadCanceledEventArgs(Exception ex)
        {
            Exception = ex;
        }
    }
    delegate void MapLoadCanceledEventHandler(object s, MapLoadCanceledEventArgs e);

    class MapLoader
    {
        public event MapLoadedEventHandler OnMapLoaded;
        public event MapLoadCanceledEventHandler OnMapLoadCanceled;

        public List<MapInfo> LoadMapInfos()
        {
            List<MapInfo> ret = new List<MapInfo>();

            if (!Directory.Exists(Properties.Resources.Dirname_Map)) return ret;

            DirectoryInfo mapDir = new DirectoryInfo(Properties.Resources.Dirname_Map);
            
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

            mi.DirectoryPath = dirPath;
            mi.Id = Path.GetFileName(dirPath);

            mi.MapName = rootElem["Name"] == null ? "" : rootElem["Name"].InnerText.Trim();
            mi.AuthorName = rootElem["Author"] == null ? "Unknown" : rootElem["Author"].InnerText.Trim();

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

                mi.Bgm = srcElem["Bgm"] == null ? null : srcElem["Bgm"].InnerText.Trim();
                mi.MapSourceFileName = srcElem["Name"] == null ? "" : srcElem["Name"].InnerText.Trim();
                mi.Mapping = srcElem["Mapping"] == null ? "" : srcElem["Mapping"].InnerText.Trim();
            }
            #endregion

            #region ChipDataInfo
            XmlElement chipDataElem = rootElem["ChipData"];
            ChipDataInfo cdi = new ChipDataInfo();
            if (chipDataElem == null)
            {
                cdi.ChipType = MapChipType.Builtin;
                cdi.FileName = "";
                cdi.BuiltinType = MapChipBuiltinType.Binary;
            }
            else
            {
                #region ChipType
                try
                {
                    string ct = chipDataElem["Type"] == null ? "" : chipDataElem["Type"].InnerText.Trim();
                    cdi.ChipType = (MapChipType)Enum.Parse(typeof(MapChipType), ct, true);
                }
                catch (ArgumentException)
                {
                    cdi.ChipType = MapChipType.None;
                }
                #endregion

                #region Name
                if (cdi.ChipType == MapChipType.Builtin)
                {
                    string cnt = chipDataElem["Name"] == null ? "" : chipDataElem["Name"].InnerText.Trim();
                    try
                    {
                        if (string.IsNullOrEmpty(cnt)) cdi.BuiltinType = MapChipBuiltinType.Binary;
                        else cdi.BuiltinType = (MapChipBuiltinType)Enum.Parse(typeof(MapChipBuiltinType), cnt, true);
                    }
                    catch (ArgumentException)
                    {
                        cdi.BuiltinType = MapChipBuiltinType.Binary;
                    }
                }
                else
                {
                    cdi.FileName = chipDataElem["Name"] == null ? "" : chipDataElem["Name"].InnerText.Trim();
                    cdi.BuiltinType = MapChipBuiltinType.None;
                }
                #endregion

                #region Size
                XmlElement sizeElem = chipDataElem["Size"];
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
                    cdi.Size = new Size(width, height);
                }
                else
                {
                    cdi.Size = new Size(16, 16);
                }
                #endregion

                #region ChipInfo
                {
                    cdi.ChipInfos = new List<ChipInfo>();
                    XmlNodeList chipsLst = chipDataElem.GetElementsByTagName("Chip");
                    foreach (XmlNode chipNode in chipsLst)
                    {
                        ChipInfo ci = new ChipInfo();
                        if (chipNode["Color"] == null)
                        {
                            ci.Color = null;
                        }
                        else
                        {
                            string ciColorStr = chipNode["Color"].InnerText.Trim();
                            ci.Color = ImageUtil.GetColor(ciColorStr);
                        }
                        string ciHardStr = chipNode["Hardness"] == null ? "0" : chipNode["Hardness"].InnerText.Trim();
                        if (!int.TryParse(ciHardStr, out ci.Hardness)) ci.Hardness = 0;
                        cdi.ChipInfos.Add(ci);
                    }
                }
                #endregion
            }
            mi.ChipDataInfo = cdi;
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
                mi.BackgroundColor = colorElem["Background"] == null ? Color.White : ImageUtil.GetColor(colorElem["Background"].InnerText.Trim());
                mi.StrongColor = colorElem["Strong"] == null ? Color.Red : ImageUtil.GetColor(colorElem["Strong"].InnerText.Trim());
                mi.ForegroundColor = colorElem["Foreground"] == null ? Color.Black : ImageUtil.GetColor(colorElem["Foreground"].InnerText.Trim());
            }
            #endregion

            #region Pitch
            XmlElement pitchElem = rootElem["Pitch"];
            if (pitchElem != null)
            {
                object pitchObj = Enum.Parse(typeof(PitchType), pitchElem.InnerText.Trim(), true);
                try
                {
                    mi.PitchType = (PitchType)pitchObj;
                }
                catch (Exception) { mi.PitchType = PitchType.Variable; }
            }
            #endregion
            if (isValidMapInfo(mi)) return mi;
            return null;
        }

        private bool isValidMapInfo(MapInfo info)
        {
            if (info.MapSourceType == MapSourceType.None) return false;
            if (info.ChipDataInfo == null) return false;
            if (info.ChipDataInfo.ChipType == MapChipType.None) return false;

            ChipDataInfo ci = info.ChipDataInfo;
            if (ci.ChipType == MapChipType.Image)
            {
                if (ci.Size == Size.Empty) return false;
                if (string.IsNullOrEmpty(ci.FileName)) return false;
                if (!File.Exists(Path.Combine(info.DirectoryPath, ci.FileName))) return false;
            }
            else if (ci.ChipType == MapChipType.Builtin)
            {
                if (ci.BuiltinType == MapChipBuiltinType.None) return false;
            }

            if (string.IsNullOrEmpty(info.MapName)) return false;
            if (string.IsNullOrEmpty(info.MapSourceFileName)) return false;

            if (!File.Exists(Path.Combine(info.DirectoryPath, info.MapSourceFileName))) return false;

            return true;
        }

        private void loadMapTh(MapInfo info)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += (s, e) =>
            {
                try
                {
                    e.Result = loadMap(e.Argument as MapInfo);
                }
                catch (MapLoadException)
                {
                    e.Cancel = true;
                }
            };
            worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Cancelled)
                {
                    MapLoadCanceledEventHandler del = OnMapLoadCanceled;
                    if (del != null)
                    {
                        MapLoadCanceledEventArgs arg = new MapLoadCanceledEventArgs(null);
                        del(s, arg);
                    }
                }
                else
                {
                    MapLoadedEventHandler del = OnMapLoaded;
                    if (del != null)
                    {
                        MapLoadedEventArgs arg = new MapLoadedEventArgs(e.Result as Map);
                        del(s, arg);
                    }
                }
            };
            worker.RunWorkerAsync(info);
        }

        private Map loadBuiltinMap(MapInfo info)
        {
            Map map = null;
            if (info is RandomMap.RandomMapInfo)
            {
                map = new RandomMap(info.Level);
            }
            else if (info is EmptyMap.EmptyMapInfo)
            {
                map = new EmptyMap();
            }
            else if (info is EmptyFixedMap.EmptyFixedMapInfo)
            {
                map = new EmptyFixedMap();
            }
            else if (info is RandomEndlessMap.RandomEndlessMapInfo)
            {
                map = null;
            }
            map.MapInfo = info;
            return map;
        }

        private Map loadUserMap(MapInfo info)
        {
            Map map = null;
            MapChipData chipData = null;

            try
            {
                switch (info.ChipDataInfo.ChipType)
                {
                    case MapChipType.Builtin:
                        {
                            switch (info.ChipDataInfo.BuiltinType)
                            {
                                case MapChipBuiltinType.Binary:
                                    chipData = BinaryChipData.LoadChipData(info);
                                    map = new BinaryMap();
                                    map.ChipData = chipData;
                                    break;
                                case MapChipBuiltinType.Colors:
                                    chipData = ColorChipData.LoadChipData(info);
                                    map = new BasicMap();
                                    map.ChipData = chipData;
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
                    case MapSourceType.Text:
                        {
                            string srcPath = Path.Combine(info.DirectoryPath, info.MapSourceFileName);
                            string mapping = info.Mapping;
                            string[] lines = File.ReadAllLines(srcPath, Encoding.UTF8);
                            map.LoadMapText(lines, mapping);
                        }
                        break;
                    case MapSourceType.Image:
                        {
                            string srcPath = Path.Combine(info.DirectoryPath, info.MapSourceFileName);
                            string mappingPath = Path.Combine(info.DirectoryPath, info.Mapping);
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
                    case MapSourceType.Music:
                        {
                            string srcPath = Path.Combine(info.DirectoryPath, info.MapSourceFileName);

                            Music music = Music.LoadMusic(srcPath);
                            info.MaxPitch = music.MaxPitch;
                            info.MinPitch = music.MinPitch;
                            using (Bitmap srcBmp = music.GetMap(SdlDotNet.Core.Events.TargetFps, info.PlayerVx,
                                info.ChipDataInfo.Size.Width, info.ChipDataInfo.Size.Height))
                            {
                                if (srcBmp != null)
                                {
                                    string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                    dirPath = Path.Combine(dirPath, Properties.Resources.Dirname_Config);
                                    string fpath = Path.Combine(dirPath, "music.png");
                                    srcBmp.Save(fpath);
                                    fpath = Path.Combine(dirPath, "music.txt");
                                    using (StreamWriter writer = new StreamWriter(fpath, false, Encoding.UTF8))
                                    {
                                        writer.WriteLine("max pitch: {0}", info.MaxPitch);
                                        writer.WriteLine("min pitch: {0}", info.MinPitch);
                                    }

                                    map.LoadMapImage(srcBmp, null);
                                }
                            }
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(info.Bgm))
                {
                    string bgmPath = Path.Combine(info.DirectoryPath, info.Bgm);
                    map.Bgm = new SdlDotNet.Audio.Music(bgmPath);
                }

                map.MapInfo = info;
                return map;
            }
            catch (Exception ex)
            {
                throw new MapLoadException(string.Format("{0}: {1}", Properties.Resources.Str_MapLoadError, info.Id), ex);
            }
        }

        private Map loadMap(MapInfo info)
        {
            if (info is BuiltinMapInfo) return loadBuiltinMap(info);
            else return loadUserMap(info);
        }

        public void LoadMap(MapInfo info)
        {
            loadMapTh(info);
        }
    }
}
