using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using AForge.Math;

namespace PitchPitch.audio
{
    public class PitchResult
    {
        /// <summary>ミリ秒</summary>
        public double[] Time;
        /// <summary>入力波</summary>
        public double[] Signal;

        /// <summary>周波数</summary>
        public double[] Frequency;
        /// <summary>周波数の振幅</summary>
        public double[] Amplitude;
        /// <summary>周波数のパワー</summary>
        public double[] Power;

        /// <summary>自己相関関数</summary>
        public double[] Correlation;
        /// <summary>Normalized Square Differnce Function</summary>
        public double[] NSDF;

        /// <summary>入力波の長さ(サンプル数)</summary>
        public int Length;
        /// <summary>Sample rate(サンプル数/秒)</summary>
        public double SampleFrequency;

        /// <summary>検出した周波数</summary>
        public double Pitch = 0;
        /// <summary>検出した周波数の確からしさ</summary>
        public double Clarity = 0;

        public PitchResult(int size, double sampleFreq)
        {
            Length = size;
            SampleFrequency = sampleFreq;
            Signal = new double[size];
            Frequency = new double[size];
            Amplitude = new double[size];
            Power = new double[size];
            Time = new double[size];
            Correlation = new double[size];
            NSDF = new double[size];
        }

        public PitchResult Copy()
        {
            PitchResult result = new PitchResult(Length, SampleFrequency);
            result.Pitch = Pitch;
            result.Clarity = Clarity;

            Array.Copy(Signal, result.Signal, Signal.Length);
            Array.Copy(Frequency, result.Frequency, Frequency.Length);
            Array.Copy(Amplitude, result.Amplitude, Amplitude.Length);
            Array.Copy(Power, result.Power, Power.Length);
            Array.Copy(Time, result.Time, Time.Length);
            Array.Copy(Correlation, result.Correlation, Correlation.Length);
            Array.Copy(NSDF, result.NSDF, NSDF.Length);
            return result;
        }
    }

    class PitchAnalyzer
    {
        private double _sampleFrequency = 44100;
        public double SampleFrequency
        {
            get { return _sampleFrequency; }
            set { _sampleFrequency = value; }
        }

        public PitchAnalyzer() { }

        public PitchResult Analyze(double[] sig)
        {
            // 必要数
            int n = (int)Math.Floor(Math.Log(sig.Length, 2));
            int size = (int)Math.Pow(2, n > 13 ? 13 : n); // limit of AForge.NET

            // 返り値用意
            PitchResult result = new PitchResult(size, SampleFrequency);

            // 時間幅・周波数分解能算出
            double curFreq = 0;
            double diffFreq = _sampleFrequency / (double)size;

            // zero pad
            double[] windowed = new double[size * 2];
            for (int i = 0; i < windowed.Length; i++)
            {
                windowed[i] = i < size ? sig[sig.Length - size + i] : 0;

                if (i < result.Signal.Length)
                {
                    result.Signal[i] = windowed[i]; // ついでに入力を保存
                    result.Time[i] = i * 1000 / result.SampleFrequency; // ms
                }
            }

            // FFT -> frequency domein
            Complex[] input = Array.ConvertAll<double, Complex>(windowed, (d) => { return new Complex(d, 0); });
            FourierTransform.FFT(input, FourierTransform.Direction.Forward);

            // 自己相関関数
            //  (パワースペクトル -> 逆フーリエ変換
            Complex[] powerSpec = Array.ConvertAll<Complex, Complex>(input, (c) => { return new Complex(c.SquaredMagnitude * 4 * size * size, 0); });
            FourierTransform.FFT(powerSpec, FourierTransform.Direction.Backward);
            double[] correlation = Array.ConvertAll<Complex, double>(powerSpec, (c) => { return c.Re / (size * 2); });

            int idx = 0;
            int highestNsdfIdx = 0; bool positiveCross = false;
            List<KeyValuePair<double, double>> keyMaxes = new List<KeyValuePair<double, double>>();
            double squared = 0; double max = double.MinValue;

            foreach (Complex c in input)
            {
                result.Correlation[idx] = correlation[idx];
                if (idx == 0)
                {
                    // ndsf divider
                    squared = result.Correlation[idx] * 2.0;
                }
                else
                {
                    // フーリエ変換の結果を入力
                    curFreq += diffFreq;
                    result.Frequency[idx - 1] = curFreq;
                    result.Amplitude[idx - 1] = c.Magnitude * 2.0;
                    result.Power[idx - 1] = c.SquaredMagnitude * 4.0;

                    // ndsf divider
                    squared = squared
                        - Math.Pow(result.Signal[idx - 1], 2)
                        - Math.Pow(result.Signal[result.Length - idx], 2);
                }

                // Normalized Square Difference Function
                if (squared == 0)
                    result.NSDF[idx] = 0;
                else
                    result.NSDF[idx] = 2 * result.Correlation[idx] / squared;

                // pick key max
                if (idx > 0)
                {
                    if (result.NSDF[idx - 1] < 0 && result.NSDF[idx] >= 0)
                    {
                        positiveCross = true;
                        highestNsdfIdx = idx;
                    }
                    else if (result.NSDF[idx - 1] > 0 && result.NSDF[idx] <= 0 || idx == result.Length - 2)
                    {
                        positiveCross = false;
                        if (highestNsdfIdx > 0 && result.NSDF[highestNsdfIdx] > 0)
                        {
                            double peakIdx; double clarity;
                            parabolicInterpolation(
                                highestNsdfIdx - 1, result.NSDF[highestNsdfIdx - 1],
                                highestNsdfIdx, result.NSDF[highestNsdfIdx],
                                highestNsdfIdx + 1, result.NSDF[highestNsdfIdx + 1],
                                out peakIdx, out clarity);
                            if(keyMaxes.Count > 0 || peakIdx < result.Length / 2.0)
                                keyMaxes.Add(new KeyValuePair<double, double>(peakIdx, clarity));
                            if (max < clarity) max = clarity;
                        }
                    }

                    if (positiveCross && result.NSDF[highestNsdfIdx] < result.NSDF[idx])
                    {
                        highestNsdfIdx = idx;
                    }
                }

                idx++;
                if (idx >= result.Length) break;
            }

            // pick max
            KeyValuePair<double, double> keyMax = keyMaxes.Find((KeyValuePair<double, double> kv) => { return (kv.Value > max * 0.8); });
            if (keyMaxes.Count <= 1)
            {
                result.Pitch = 440;
                result.Clarity = 0;
            }
            else
            {
                result.Pitch = result.SampleFrequency / keyMax.Key;
                result.Clarity = keyMax.Value;
            }

            return result;
        }

        /// <summary>
        /// 3点から放物線補間を行う
        /// </summary>
        /// <param name="x0">点0のx</param>
        /// <param name="y0">点0のy</param>
        /// <param name="x1">点1のx</param>
        /// <param name="y1">点1のy</param>
        /// <param name="x2">点2のx</param>
        /// <param name="y2">点2のy</param>
        /// <param name="b">放物線の中心x座標</param>
        /// <param name="c">放物線の頂点y座標</param>
        private void parabolicInterpolation(double x0, double y0, double x1, double y1, double x2, double y2, out double b, out double c)
        {
            b = 0; c = 0;

            double d = (y0 - y1) * (x1 - x2) - (y1 - y2) * (x0 - x1);
            if (d != 0)
                b = 0.5 * ((y0 - y1) * (x1 * x1 - x2 * x2) - (y1 - y2) * (x0 * x0 - x1 * x1)) / d;

            double t0 = x0 - b; double t1 = x1 - b;
            if (t0 * t0 - t1 * t1 != 0)
                c = (t0 * t0 * y1 - t1 * t1 * y0) / (t0 * t0 - t1 * t1);
        }
    }
}
