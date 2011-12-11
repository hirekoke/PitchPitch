using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PitchPitch.audio
{
    class AudioOutput : IDisposable
    {
        private string _filePath;
        private BinaryWriter _writer;

        private int _chunkSizePosition;
        private int _dataSizePosition;
        private int _sampleNum;
        private CoreAudioApi.WAVEFORMATEXTENSIBLE _format = null;

        private object lockObj = new object();

        public string FilePath
        {
            get { return _filePath; }
            set { 
                _filePath = value;

                WriteEnd();

                lock (lockObj)
                {
                    _writer = new BinaryWriter(new FileStream(_filePath, FileMode.Create, FileAccess.Write));
                }
                writeHeader();
            }
        }
        public CoreAudioApi.WAVEFORMATEXTENSIBLE Format
        {
            get { return _format; }
            set { _format = value; }
        }

        public AudioOutput()
        {
        }

        public void Dispose()
        {
            WriteEnd();
        }

        private void writeHeader()
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                _writer.Write(new char[] { 'R', 'I', 'F', 'F' });

                _chunkSizePosition = (int)_writer.BaseStream.Position;
                _writer.Seek(4, SeekOrigin.Current);

                _writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // format tag
                _writer.Write(new char[] { 'f', 'm', 't', ' ' });
                // format bytes
                _writer.Write(16);
                // format tag
                _writer.Write((ushort)CoreAudioApi.WaveFormatTag.WAVE_FORMAT_PCM);
                // channels
                _writer.Write((ushort)_format.nChannels);
                // sampling rate
                _writer.Write((uint)_format.nSamplesPerSec);
                // byte/sec
                _writer.Write((uint)(_format.nSamplesPerSec/*samp*/ *
                    _format.nChannels/*channel*/ * 16/*bit*/ / 8.0));
                // block size (byte/sample * channel)
                _writer.Write((ushort)(16/*bit*/ / 8.0 * _format.nChannels/*channel*/));
                // bit/sample
                _writer.Write((ushort)16/*bit*/);

                // data tag
                _writer.Write(new char[] { 'd', 'a', 't', 'a' });
                // data length (byte)
                _dataSizePosition = (int)_writer.BaseStream.Position;
                _writer.Seek(4, SeekOrigin.Current);

                _writer.Flush();
            }
        }

        public void WriteEnd()
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;

                int dataSize = (int)(_sampleNum * _format.nChannels/*channel*/ * 16/*bit*/ / 8.0);
                _writer.Seek(_chunkSizePosition, SeekOrigin.Begin);
                _writer.Write((uint)(dataSize + 36));
                _writer.Seek(_dataSizePosition, SeekOrigin.Begin);
                _writer.Write((uint)(dataSize));

                _writer.Flush();
                _writer.Close();
                _writer = null;
            }
        }

        public void WriteData(double[][] data)
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                int len = 0;
                if (data.Length > 0)
                {
                    len = data[0].Length;
                    _sampleNum += len;
                }

                for (int j = 0; j < len; j++)
                {
                    for (int i = 0; i < _format.nChannels; i++)
                    {
                        short s = (short)(data[i][j] * short.MaxValue);
                        _writer.Write(s);
                    }
                }
                _writer.Flush();
            }
        }
        public void WriteData(double[] data)
        {
            lock (lockObj)
            {
                if (_writer == null || !_writer.BaseStream.CanWrite) return;
                _sampleNum += (int)(data.Length / (double)_format.nChannels);

                for (int j = 0; j < data.Length; j++)
                {
                    short s = (short)(data[j] * short.MaxValue);
                    _writer.Write(s);
                }
                _writer.Flush();
            }
        }
    }
}
