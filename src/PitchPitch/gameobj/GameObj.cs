using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Sprites;

namespace PitchPitch.gameobj
{
    class GameObj : IDisposable
    {
        protected Sprite _graphic;
        public Sprite Graphic { get { return _graphic; } }

        protected PitchPitch _parent;

        #region 位置・大きさ
        protected double _x;
        protected double _y;
        protected double _width, _height;
        public double X { get { return _x; } set { _x = value; } }
        public double Y { get { return _y; } set { _y = value; } }
        public double Width { get { return _width; } set { _width = value; } }
        public double Height { get { return _height; } set { _height = value; } }

        public double Left { get { return _x - _width / 2.0; } }
        public double Top { get { return _y - _height / 2.0; } }
        public double Right { get { return _x + _width / 2.0; } }
        public double Bottom { get { return _y + _height / 2.0; } }
        #endregion

        #region 当たり判定位置
        protected Point[] _collisionPoints = new Point[4];
        public Point[] CollisionPoints { get { return _collisionPoints; } }
        #endregion

        protected int _hp = 100;
        public int Hp
        {
            get { return _hp; }
            set { _hp = value; }
        }
        protected int _maxHp = 10;
        public int MaxHp
        {
            get { return _maxHp; }
            set { _maxHp = value; }
        }

        public GameObj()
        {
        }

        public virtual void Init(PitchPitch Parent)
        {
            _parent = Parent;

            _hp = _maxHp;
            _collisionPoints[0] = new Point(-(int)(_width / 2.0), -(int)(_height / 2.0));
            _collisionPoints[1] = new Point(-_collisionPoints[0].X, _collisionPoints[0].Y);
            _collisionPoints[2] = new Point(-_collisionPoints[0].X, -_collisionPoints[0].Y);
            _collisionPoints[3] = new Point(_collisionPoints[0].X, -_collisionPoints[0].Y);
        }

        public virtual void LoadImage(string filePath)
        {
            _graphic = new Sprite(filePath);
            _width = _graphic.Width;
            _height = _graphic.Height;
            _graphic.Transparent = true;
            _graphic.TransparentColor = Color.Lime;
        }

        public virtual void Render(Surface s, Point p)
        {
            s.Blit(_graphic, new Point(p.X - (int)(_width / 2.0), p.Y - (int)(_height / 2.0)));

            foreach (Point offset in _collisionPoints)
            {
                s.Fill(new Rectangle(p.X - offset.X, p.Y - offset.Y, 1, 1), Color.Red);
            }
        }

        public virtual void RenderInformation(Surface s, Rectangle rect)
        {
            // 結果値
            using (Surface sc = new Surface(rect.Size))
            {
                sc.Lock();
                sc.Alpha = 100;
                sc.AlphaBlending = true;
                sc.Fill(Color.DarkGreen);
                sc.Unlock();
                s.Blit(sc, rect.Location);
            }

            using (Surface sc = new Surface(rect.Size))
            {
                sc.Transparent = true;
                sc.Draw(new SdlDotNet.Graphics.Primitives.Box(Point.Empty, new Size(rect.Width - 1, rect.Height - 1)),
                    Color.White, true, false);
                sc.Blit(ResourceManager.SmallTTFont.Render(
                    string.Format("({0},{1}), {2}x{3}", X, Y, Width, Height),
                    Color.White, true), new Point(10, 10));
                sc.Blit(ResourceManager.SmallTTFont.Render(
                    string.Format("HP: {0}", _hp),
                    Color.White, true), new Point(10, 13 + ResourceManager.SmallTTFont.Height));
                s.Blit(sc, rect.Location);
            }
        }

        public void Dispose()
        {
            if (_graphic != null) _graphic.Dispose();
        }
    }
}
