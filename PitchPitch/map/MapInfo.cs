using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PitchPitch.map
{
    enum MapSourceType
    {
        None,
        Image,
    }

    enum MapChipType
    {
        None,
        Builtin,
        Image,
    }

    enum MapChipBuiltinType
    {
        None,
        Binary,
    }

    class MapInfo
    {
        private string _id = "";
        public string Id { get { return _id; } set { _id = value; } }
        private string _dirPath = "";
        public string DirPath { get { return _dirPath; } set { _dirPath = value; } }

        private string _name = "";
        public string Name { get { return _name; } set { _name = value; } }

        private string _fileName = "";
        public string FileName { get { return _fileName; } set { _fileName = value; } }
        private MapSourceType _mapSourceType = MapSourceType.None;
        public MapSourceType MapSourceType { get { return _mapSourceType; } set { _mapSourceType = value; } }
        private string _mapping = "";
        public string Mapping { get { return _mapping; } set { _mapping = value; } }

        private string _chipFileName = "";
        public string ChipFileName { get { return _chipFileName; } set { _chipFileName = value; } }
        private MapChipBuiltinType _builtinChipName = MapChipBuiltinType.Binary;
        public MapChipBuiltinType BuiltinChipName { get { return _builtinChipName; } set { _builtinChipName = value; } }
        private MapChipType _chipType = MapChipType.Builtin;
        public MapChipType ChipType { get { return _chipType; } set { _chipType = value; } }
        private Size _chipSize = new Size(16, 16);
        public Size ChipSize { get { return _chipSize; } set { _chipSize = value; } }

        private Size _size = new Size(200, 30);
        public Size Size { get { return _size; } set { _size = value; } }

        private int _playerVx = 1;
        public int PlayerVx { get { return _playerVx; } set { _playerVx = value; } }

        private Color _foreColor = Color.Black;
        public Color ForeColor { get { return _foreColor; } set { _foreColor = value; } }
        private Color _backColor = Color.White;
        public Color BackColor { get { return _backColor; } set { _backColor = value; } }
    }
}
