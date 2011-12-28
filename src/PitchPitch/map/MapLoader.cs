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

            XmlNode rootNode = mapXml.FirstChild;
            if (rootNode == null || rootNode.Name.ToLower() != "map") return null;

            mi.DirectoryPath = dirPath;
            mi.Id = Path.GetFileName(dirPath);
            mi.PitchType = PitchType.Undefined;

            foreach (XmlNode node in rootNode)
            {
                switch (node.Name.ToLower())
                {
                    #region name
                    case "name":
                        {
                            mi.MapName = node.InnerText.Trim();
                        }
                        break;
                    #endregion
                    #region author
                    case "author":
                        {
                            mi.AuthorName = node.InnerText.Trim();
                        }
                        break;
                    #endregion
                    #region player
                    case "player":
                        {
                            foreach (XmlNode plNode in node)
                            {
                                switch (plNode.Name.ToLower())
                                {
                                    case "vx":
                                        int vx = 1;
                                        string vxt = plNode.InnerText.Trim();
                                        if (int.TryParse(vxt, out vx)) mi.PlayerVx = vx;
                                        break;
                                }
                            }
                        }
                        break;
                    #endregion
                    #region color
                    case "color":
                        {
                            foreach (XmlNode coNode in node)
                            {
                                switch (coNode.Name.ToLower())
                                {
                                    case "foreground":
                                        mi.ForegroundColor = ImageUtil.GetColor(coNode.InnerText.Trim());
                                        break;
                                    case "background":
                                        mi.BackgroundColor = ImageUtil.GetColor(coNode.InnerText.Trim());
                                        break;
                                    case "strong":
                                        mi.StrongColor = ImageUtil.GetColor(coNode.InnerText.Trim());
                                        break;
                                }
                            }
                        }
                        break;
                    #endregion
                    #region pitch
                    case "pitch":
                        {
                            #region PitchType
                            try
                            {
                                string pt = node.InnerText.Trim();
                                mi.PitchType = (PitchType)Enum.Parse(typeof(PitchType), pt, true);
                            }
                            catch (Exception) { mi.PitchType = PitchType.Undefined; }
                            #endregion

                            #region max/min
                            if (mi.PitchType == PitchType.Fixed)
                            {
                                double max = Config.Instance.MaxFreq;
                                double min = Config.Instance.MinFreq;
                                foreach (XmlAttribute attr in node.Attributes)
                                {
                                    switch (attr.Name.ToLower())
                                    {
                                        case "max":
                                            if (double.TryParse(attr.InnerText.Trim(), out max)) mi.MaxPitch = max;
                                            break;
                                        case "min":
                                            if (double.TryParse(attr.InnerText.Trim(), out min)) mi.MinPitch = min;
                                            break;
                                    }
                                }
                            }
                            #endregion
                        }
                        break;
                    #endregion
                    #region chipdata
                    case "chipdata":
                        {
                            ChipDataInfo cdi = new ChipDataInfo();
                            #region default
                            cdi.ChipType = MapChipType.Builtin;
                            cdi.FileName = "";
                            cdi.BuiltinType = MapChipBuiltinType.Binary;
                            #endregion

                            #region size
                            int width = 16; int height = 16;
                            foreach (XmlAttribute attr in node.Attributes)
                            {
                                switch (attr.Name.ToLower())
                                {
                                    case "width":
                                        if (!int.TryParse(attr.InnerText.Trim(), out width)) width = 16;
                                        break;
                                    case "height":
                                        if (!int.TryParse(attr.InnerText.Trim(), out height)) height = 16;
                                        break;
                                }
                            }
                            cdi.Size = new Size(width, height);
                            #endregion

                            string name = null;
                            foreach (XmlNode cdiNode in node)
                            {
                                switch (cdiNode.Name.ToLower())
                                {
                                    #region type
                                    case "type":
                                        {
                                            try
                                            {
                                                string ct = cdiNode.InnerText.Trim();
                                                cdi.ChipType = (MapChipType)Enum.Parse(typeof(MapChipType), ct, true);
                                            }
                                            catch (ArgumentException)
                                            {
                                                cdi.ChipType = MapChipType.None;
                                            }
                                        }
                                        break;
                                    #endregion
                                    #region name
                                    case "name":
                                        {
                                            name = cdiNode.InnerText.Trim();
                                        }
                                        break;
                                    #endregion
                                    #region chip
                                    case "chip":
                                        {
                                            if (cdi.ChipInfos == null) cdi.ChipInfos = new List<ChipInfo>();

                                            ChipInfo ci = new ChipInfo();
                                            ci.Color = null;
                                            ci.Hardness = cdi.ChipInfos.Count == 0 ? 0 : 1;

                                            foreach (XmlNode ciNode in cdiNode)
                                            {
                                                switch (ciNode.Name.ToLower())
                                                {
                                                    case "color":
                                                        {
                                                            string ciColorStr = ciNode.InnerText.Trim();
                                                            ci.Color = ImageUtil.GetColor(ciColorStr);
                                                        }
                                                        break;
                                                    case "hardness":
                                                        {
                                                            string ciHardStr = ciNode.InnerText.Trim();
                                                            if (!int.TryParse(ciHardStr, out ci.Hardness)) ci.Hardness = cdi.ChipInfos.Count == 0 ? 0 : 1;
                                                        }
                                                        break;
                                                }
                                            }
                                            cdi.ChipInfos.Add(ci);
                                        }
                                        break;
                                    #endregion
                                }
                            }

                            #region name解釈
                            if (cdi.ChipType == MapChipType.Builtin)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(name)) cdi.BuiltinType = MapChipBuiltinType.Binary;
                                    else cdi.BuiltinType = (MapChipBuiltinType)Enum.Parse(typeof(MapChipBuiltinType), name, true);
                                }
                                catch (ArgumentException)
                                {
                                    cdi.BuiltinType = MapChipBuiltinType.Binary;
                                }
                            }
                            else
                            {
                                cdi.FileName = name;
                                cdi.BuiltinType = MapChipBuiltinType.None;
                            }
                            #endregion

                            mi.ChipDataInfo = cdi;
                        }
                        break;
                    #endregion
                    #region source
                    case "source":
                        foreach (XmlNode soNode in node)
                        {
                            switch (soNode.Name.ToLower())
                            {
                                #region type
                                case "type":
                                    try
                                    {
                                        string mt = soNode.InnerText.Trim();
                                        mi.MapSourceType = (MapSourceType)Enum.Parse(typeof(MapSourceType), mt, true);
                                    }
                                    catch (ArgumentException)
                                    {
                                        mi.MapSourceType = MapSourceType.None;
                                    }
                                    break;
                                #endregion
                                #region name
                                case "name":
                                    mi.MapSourceFileName = soNode.InnerText.Trim();
                                    break;
                                #endregion
                                #region mapping
                                case "mapping":
                                    mi.Mapping = soNode.InnerText.Trim();
                                    break;
                                #endregion
                            }
                        }
                        break;
                    #endregion
                    #region bgm
                    case "bgm":
                        {
                            BgmInfo bi = new BgmInfo();
                            bi.Name = node.InnerText.Trim();
                            foreach (XmlAttribute attr in node.Attributes)
                            {
                                switch (attr.Name.ToLower())
                                {
                                    #region volume
                                    case "volume":
                                        {
                                            int vol = 50;
                                            if (int.TryParse(attr.InnerText.Trim(), out vol))
                                            {
                                                vol = vol < 0 ? 0 : (vol > 100 ? 100 : vol);
                                            }
                                            bi.Volume = vol;
                                        }
                                        break;
                                    #endregion
                                }
                            }
                            mi.BgmInfo = bi;
                        }
                        break;
                    #endregion
                    default:
                        break;
                }
            }

            #region Pitchの記述が無い場合
            if (mi.PitchType == PitchType.Undefined)
            {
                if (mi.MapSourceType == MapSourceType.Music) mi.PitchType = PitchType.Fixed;
                else mi.PitchType = PitchType.Variable;
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
            if (info.PitchType == PitchType.Undefined) return false;

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
                                    string fpath = Path.Combine(dirPath, Properties.Resources.Filename_MusicLogImage);
                                    srcBmp.Save(fpath);
                                    fpath = Path.Combine(dirPath, Properties.Resources.Filename_MusicLogText);
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

                if (info.BgmInfo != null && !string.IsNullOrEmpty(info.BgmInfo.Name))
                {
                    string bgmPath = Path.Combine(info.DirectoryPath, info.BgmInfo.Name);
                    map.Bgm = new SdlDotNet.Audio.Music(bgmPath);
                    map.BgmVolume = info.BgmInfo.Volume;
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
