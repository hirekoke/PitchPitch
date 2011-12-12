using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

using SdlDotNet.Core;
using SdlDotNet.Graphics;
using SdlDotNet.Graphics.Primitives;
using SdlDotNet.Graphics.Sprites;
using SdlDotNet.Particles;
using SdlDotNet.Particles.Emitters;
using SdlDotNet.Particles.Manipulators;

namespace PitchPitch.gameobj
{
    class Player : GameObj
    {
        protected SurfaceCollection _playerSurfaces;
        protected SurfaceCollection _coloredPlayerSurfaces;
        protected int _renderSurfaceIdx = 0;

        protected long _prevFrameTick = 0;
        protected const int _frameTick = 100; /* msec */

        protected bool _isPaused = false;
        public bool IsPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; }
        }

        protected double _defaultVx = 1;
        protected double _vx = 1;
        public double Vx
        {
            get { return _vx; }
            set { _vx = value; }
        }

        #region 爆発用
        protected ParticleSystem _ptclSystem;
        protected ParticleSurfaceEmitter _ptclEmitter;

        /// <summary>爆発中かどうか</summary>
        protected bool _isExplosion = false;
        /// <summary>爆発が始まった時刻</summary>
        protected long _explosionStartTick = 0;
        /// <summary>爆発にかかる時間</summary>
        protected const int _explosionLength = 500; /* msec */
        /// <summary>表示する要素の実態</summary>
        protected SurfaceCollection _explosionSurfaces;
        protected SurfaceCollection _explosionColoredSurfaces;
        #endregion

        #region 無敵時間制御用
        protected bool _isInvincible = false;
        protected const int _invincibleLength = 1000; /* msec*/
        protected const int _invincibleFrameTick = 100; /* msec */
        protected long _invincibleStartTick = 0;
        protected long _prevInvincibleFrameTick = 0;
        protected bool _isInvincibleFrameVisible = false;
        #endregion

        #region 半径
        private double _minRad = 6;
        public double MinRadius
        {
            get { return _minRad; }
            set { _minRad = value; }
        }
        private double _maxRad = 40;
        public double MaxRadius
        {
            get { return _maxRad; }
            set { _maxRad = value; }
        }

        private double _radInc = 1;
        private double _radDec = 2;
        /// <summary>大きさ増分</summary>
        public double RadInc
        {
            get { return _radInc; }
            set { _radInc = value; }
        }
        /// <summary>大きさ減分</summary>
        public double RadDec
        {
            get { return _radDec; }
            set { _radDec = value; }
        }

        private double _rad = 10;
        public double Rad
        {
            get { return _rad; }
            set
            {
                _rad = value;
                if (_rad > _maxRad) _rad = _maxRad;
                if (_rad < _minRad) _rad = _minRad;
                _width = _rad * 2;
                _height = _rad * 2;

                updateCollisionPoints();
            }
        }

        private new double Width { get { return _width; } }
        private new double Height { get { return _height; } }
        #endregion

        #region 色
        protected Color _foreColor = Color.Black;
        public Color ForeColor
        {
            get { return _foreColor; }
            set
            {
                _foreColor = value;
                if (_playerSurfaces != null)
                {
                    if (_coloredPlayerSurfaces != null) 
                        foreach (Surface s in _coloredPlayerSurfaces) s.Dispose();
                    _coloredPlayerSurfaces = ImageUtil.CreateColored(_playerSurfaces, _foreColor);
                }
                updateExpColor();
            }
        }

        protected Color _explosionColor = Color.White;
        public Color ExplosionColor
        {
            get { return _explosionColor; }
            set
            {
                _explosionColor = value;
                updateExpColor();
            }
        }

        private void updateExpColor()
        {
            if (_explosionSurfaces != null)
            {
                if (_explosionColoredSurfaces != null)
                {
                    foreach (Surface s in _explosionColoredSurfaces)
                    {
                        s.Dispose();
                    }
                }
                _explosionColoredSurfaces = new SurfaceCollection();
                foreach (Surface s in _explosionSurfaces)
                {
                    _explosionColoredSurfaces.Add(ImageUtil.CreateColored(s,
                        _foreColor, _explosionColor));
                }
            }
            initExplosion();
        }
        #endregion

        public Player() : base()
        {
        }

        public override void Init(PitchPitch Parent)
        {
            Dispose();

            base.Init(Parent);
            _rad = _minRad * 2;
            _vx = _defaultVx;
            _hp = _maxHp;

            _collisionPoints = new Point[8];
            updateCollisionPoints();
            loadImages();
            initExplosion();
        }

        protected virtual void initExplosion()
        {
            _ptclSystem = new ParticleSystem();
            _ptclEmitter = new ParticleSurfaceEmitter(_explosionColoredSurfaces == null ? _explosionSurfaces : _explosionColoredSurfaces);
            _ptclEmitter.Frequency = 300;
            _ptclEmitter.LifeFullMin = 20;
            _ptclEmitter.LifeFullMax = 40;
            _ptclEmitter.LifeMin = 10;
            _ptclEmitter.LifeMax = 30;
            _ptclEmitter.DirectionMin = -(float)Math.PI;
            _ptclEmitter.DirectionMax = (float)Math.PI;
            _ptclEmitter.SpeedMin = 5;
            _ptclEmitter.SpeedMax = 6;

            _ptclSystem.Add(_ptclEmitter);

            _ptclSystem.Manipulators.Add(new ParticleGravity(0.2f)); // Gravity
            _ptclSystem.Manipulators.Add(new ParticleFriction(0.3f)); // Slow down _ptclSystem
            //_ptclSystem.Manipulators.Add(new ParticleVortex(1f, 200f)); // A particle vortex fixed on the mouse
            _ptclSystem.Manipulators.Add(new ParticleBoundary(new Size(Constants.StageViewWidth, Constants.StageViewHeight))); // fix _ptclSystem on screen.
        }

        protected virtual void updateCollisionPoints()
        {
            int c00 = (int)(_rad * 0.8);
            int c45 = (int)(_rad * Math.Cos(Math.PI / 4.0) * 0.8);
            _collisionPoints[0] = new Point(0, -c00);
            _collisionPoints[1] = new Point(c00, 0);
            _collisionPoints[2] = new Point(0, c00);
            _collisionPoints[3] = new Point(-c00, 0);
            _collisionPoints[4] = new Point(-c45, -c45);
            _collisionPoints[5] = new Point(-c45, +c45);
            _collisionPoints[6] = new Point(+c45, -c45);
            _collisionPoints[7] = new Point(+c45, +c45);
        }

        protected virtual void loadImages()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

            int[] tmp = { 0, 1, 2 };
            _playerSurfaces = ResourceManager.LoadSurfaces(Array.ConvertAll<int, string>(tmp, (i) => { return string.Format("player-{0}.png", i); }));
            _playerSurfaces.Add(_playerSurfaces[1]);

            _explosionSurfaces = ResourceManager.LoadSurfaces("explosion.png", new Size(24, 24));
        }

        public bool Hit(map.Chip chip, PointD pointInView, int chipWidth, int chipHeight)
        {
            if (_isPaused) return false;
            if (_isInvincible) return false; // 無敵中

            foreach (Point cp in _collisionPoints)
            {
                double cpx = pointInView.X + cp.X; double cpy = pointInView.Y + cp.Y;
                bool collision =
                    (chip.ViewX <= cpx && chip.ViewY <= cpy &&
                    cpx <= chip.ViewX + chipWidth && cpy <= chip.ViewY + chipHeight);
                if (collision)
                {
                    Hp -= chip.Hardness;

                    // 爆発開始
                    _isExplosion = true;
                    _explosionStartTick = Environment.TickCount;
                    _ptclEmitter.Emitting = false;
                    _ptclEmitter.X = (float)cpx; _ptclEmitter.Y = (float)cpy;
                    _ptclEmitter.Emitting = true;
                    
                    // 無敵時間開始
                    _isInvincible = true;
                    _invincibleStartTick = Environment.TickCount;

                    return true;
                }
            }
            return false;
        }

        protected virtual void renderExplosion(Surface s, Point p)
        {
            long tick = Environment.TickCount;
            if (_isExplosion)
            {
                _ptclEmitter.X = p.X; _ptclEmitter.Y = p.Y;

                if (!_isPaused) _ptclSystem.Update();

                _ptclSystem.Render(s);

                if (tick - _explosionStartTick > _explosionLength && !_isPaused)
                    _isExplosion = false;
            }
        }

        private double _psWidthInv = -1;
        private double _psHeightInv = -1;
        protected virtual void renderPlayer(Surface s, Point p)
        {
            Surface ps = (_coloredPlayerSurfaces == null ? _playerSurfaces : _coloredPlayerSurfaces)[_renderSurfaceIdx];
            if (_psWidthInv < 0) _psWidthInv = 1 / (double)ps.Width;
            if (_psHeightInv < 0) _psHeightInv = 1 / (double)ps.Height;

            long tick = Environment.TickCount;
            if (tick - _invincibleStartTick > _invincibleLength)
            {
                _isInvincible = false;
                _isInvincibleFrameVisible = true;
            }

            if (_isInvincible && !_isPaused) // 無敵中
            {
                if (tick - _prevInvincibleFrameTick > _invincibleFrameTick)
                {
                    _prevInvincibleFrameTick = tick;
                    _isInvincibleFrameVisible = !_isInvincibleFrameVisible;
                }
            }
            if (_isInvincibleFrameVisible)
            {
                using (Surface ts = ps.CreateScaledSurface(_rad * 3 * _psWidthInv, _rad * 3 * _psHeightInv, true))
                {
                    s.Blit(ts, new Point((int)(p.X - _rad * 2), (int)(p.Y - _rad * 2)));
                }
            }
        }

        public override void Render(Surface s, Point p)
        {
            long tick = Environment.TickCount;
            renderPlayer(s, p);
            renderExplosion(s, p);

            // 当たり判定点
            //foreach (Point cp in _collisionPoints)
            //{
            //    s.Fill(new Rectangle(p.X + cp.X - 1, p.Y + cp.Y - 1, 3, 3), Color.Red);
            //}

            if (tick - _prevFrameTick > _frameTick && !_isPaused)
            {
                _renderSurfaceIdx = (_renderSurfaceIdx + 1) % _playerSurfaces.Count;
                _prevFrameTick = tick;
            }
        }

        public new void Dispose()
        {
            if (_playerSurfaces != null)
            {
                foreach (Surface s in _playerSurfaces) s.Dispose();
            }
            if (_explosionSurfaces != null)
            {
                foreach (Surface s in _explosionSurfaces) s.Dispose();
            }
            if (_coloredPlayerSurfaces != null)
            {
                foreach (Surface s in _coloredPlayerSurfaces) s.Dispose();
            }
            if (_explosionColoredSurfaces != null)
            {
                foreach (Surface s in _explosionColoredSurfaces) s.Dispose();
            }
        }
    }
}
