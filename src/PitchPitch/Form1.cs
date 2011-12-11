using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PitchPitch.audio;

namespace PitchPitch
{
    public partial class Form1 : Form
    {
        private audio.AudioInput _input;

        public Form1()
        {
            InitializeComponent();

            this.Load += new EventHandler(Form1_Load);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

        }

        void Form1_Load(object sender, EventArgs arg)
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            _input = new AudioInput();

            _input.DeviceInfoUpdated += (s, e) =>
                {
                    this.Invoke(new updateDeviceListDelegate(updateDeviceList), e.DeviceInfo);
                };

            _input.DeviceSelected += (s, e) =>
                {
                    this.Invoke(new updateDeviceDelegate(updateDevice), e.Device);
                };
            _input.CaptureStarted += (s, e) =>
                {
                    Console.WriteLine("capture started");
                };
            _input.CaptureStopped += (s, e) =>
                {
                    Console.WriteLine("capture stopped");
                };
            _input.DataUpdated += (s, e) =>
                {
                    this.Invoke(new TestShowDelegate(TestShow), e.Pitch);
                };
        }
        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _input.Dispose();
        }

        private delegate void updateDeviceListDelegate(List<DeviceInfo> info);
        private void updateDeviceList(List<DeviceInfo> info)
        {
            listBox1.SuspendLayout();
            listBox1.Items.Clear();
            foreach (DeviceInfo di in info)
            {
                listBox1.Items.Add(di);
            }
            listBox1.ResumeLayout();
        }

        private delegate void updateDeviceDelegate(CoreAudioApi.MMDevice dev);
        private void updateDevice(CoreAudioApi.MMDevice dev)
        {
            List<KeyValuePair<string, string>> lst = new List<KeyValuePair<string, string>>();
            lst.Add(new KeyValuePair<string, string>("名前", dev.FriendlyName));
            lst.Add(new KeyValuePair<string, string>("Id", dev.Id));
            lst.Add(new KeyValuePair<string, string>("DataFlow", dev.DataFlow.ToString()));
            lst.Add(new KeyValuePair<string, string>("状態", dev.State.ToString()));

            CoreAudioApi.WAVEFORMATEXTENSIBLE format = dev.AudioClient.MixFormat;
            lst.Add(new KeyValuePair<string, string>("チャンネル", format.nChannels.ToString()));
            lst.Add(new KeyValuePair<string, string>("ビット数/サンプル", format.wBitsPerSample.ToString()));
            lst.Add(new KeyValuePair<string, string>("フォーマット", format.wFormatTag.ToString()));
            lst.Add(new KeyValuePair<string, string>("サンプル数/秒", format.nSamplesPerSec.ToString()));
            lst.Add(new KeyValuePair<string, string>("ブロックサイズ", format.nBlockAlign.ToString()));
            Guid subFormat = format.SubFormat;
            lst.Add(new KeyValuePair<string, string>("サブタイプ", CoreAudioApi.AudioMediaSubtypes.GetAudioSubtypeName(subFormat)));
            devListViewAdd(lst);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _input.UpdateDeviceInfo();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            object o = listBox1.SelectedItem;
            DeviceInfo info = o as DeviceInfo;
            if (info != null)
            {
                _input.SelectDevice(info.DeviceId);
            }
        }

        private void devListViewAdd(List<KeyValuePair<string, string>> lst)
        {
            listView1.SuspendLayout();
            listView1.Items.Clear();
            foreach (KeyValuePair<string, string> kv in lst)
            {
                ListViewItem item = new ListViewItem(new string[] { kv.Key, kv.Value });
                listView1.Items.Add(item);
            }
            listView1.ResumeLayout();
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _input.StartCapture();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            _input.StopCapture();
        }

        private delegate void TestShowDelegate(audio.PitchResult result);
        public void TestShow(audio.PitchResult result)
        {
            lock (_result)
            {
                _result = result;
            }
            panel1.Invalidate();
        }

        private audio.PitchResult _result = new audio.PitchResult(0, 48000);
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (_result == null) return;
            Graphics g = e.Graphics;
            g.Clear(SystemColors.Window);

            Pen powerPen = new Pen(Brushes.Black, 1f);
            Pen correlationPen = new Pen(Brushes.Gray, 1f);
            Pen nsdfPen = new Pen(Brushes.DarkGreen, 1f);

            lock (_result)
            {
                double dx = panel1.Width / (double)_result.Length;
                int width = panel1.Width; int height = panel1.Height - 20;

                double x = 0; double prevX = -40;
                for (int i = 0; i < _result.Length; i++)
                {
                    x += dx;
                    if (x - prevX >= 40)
                    {
                        g.DrawLine(powerPen, new Point((int)x, panel1.Height), new Point((int)x, height));
                        g.DrawString(_result.Frequency[i].ToString("f0"), SystemFonts.DefaultFont, Brushes.Red, new Point((int)x, height + 2));
                        prevX = x;
                    }
                }

                double maxPower = double.MinValue;
                double maxCorrelation = double.MinValue;
                double minCorrelation = double.MaxValue;

                for (int i = 0; i < _result.Length; i++)
                {
                    if (maxPower < _result.Power[i]) maxPower = _result.Power[i];
                }
                for (int i = 1; i < _result.Length; i++)
                {
                    if (maxCorrelation < _result.Correlation[i])
                        maxCorrelation = _result.Correlation[i];
                    if (minCorrelation > _result.Correlation[i])
                        minCorrelation = _result.Correlation[i];
                }

                x = 0;
                double powerY = 0;
                double correlationY = 0;

                for (int i = 0; i < _result.Length; i++)
                {
                    double powerYNew = height - height * (_result.Power[i] / (maxPower == 0 ? 1.0 : maxPower));
                    double correlationYNew = height - height * ((_result.Correlation[i] - minCorrelation) / ((maxCorrelation - minCorrelation) == 0 ? 1.0 : (maxCorrelation - minCorrelation)));

                    double xnew = x + dx;
                    if (i != 0)
                    {
                        g.DrawLine(powerPen, new Point((int)x, (int)powerY), new Point((int)xnew, (int)powerYNew));
                        g.DrawLine(correlationPen, new Point((int)x, (int)correlationY), new Point((int)xnew, (int)correlationYNew));
                        g.DrawLine(nsdfPen,
                            new Point((int)x, (int)(height - height * (_result.NSDF[i - 1] + 1) / 2.0)),
                            new Point((int)xnew, (int)(height - height * (_result.NSDF[i] + 1) / 2.0)));
                        g.DrawLine(new Pen(Brushes.Red, 1.0f),
                            new Point((int)xnew, (int)(height - height * (_result.Signal[i] + 1) / 2.0)),
                            new Point((int)xnew, (int)(height / 2.0)));
                    }
                    x = xnew;
                    powerY = powerYNew;
                    correlationY = correlationYNew;
                }

                g.DrawString("pitch: " + (_result.Pitch).ToString("f1"), SystemFonts.DefaultFont, Brushes.Blue, new Point(10, 10));
            }
        }
    }
}
