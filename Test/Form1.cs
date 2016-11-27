using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Wav;
using Frequency;

namespace Test
{
    using Freq = Frequency.Frequency;

    public partial class Form1 : Form
    {
        private float[] _signal;
        private float _min, _max, _center;

        private void writeWav(Stream stream, float[] samples, uint samplerate)
        {
            uint numsamples = (uint)samples.Length;
            ushort numchannels = 1;
            ushort samplelength = 2;

            BinaryWriter wr = new BinaryWriter(stream);

            wr.Write(Encoding.ASCII.GetBytes("RIFF"));
            wr.Write(36 + numsamples * numchannels * samplelength);
            wr.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            wr.Write((uint)16);
            wr.Write((ushort)1);
            wr.Write(numchannels);
            wr.Write(samplerate);
            wr.Write((uint)(samplerate * samplelength * numchannels));
            wr.Write((ushort)(samplelength * numchannels));
            wr.Write((ushort)(8 * samplelength));
            wr.Write(Encoding.ASCII.GetBytes("data"));
            wr.Write(numsamples * samplelength);

            for (int i = 0; i < numsamples; i++)
            {
                wr.Write((ushort)(samples[i] * short.MaxValue));
            }
        }

        public Form1()
        {
            InitializeComponent();

            float[] orig;
            uint rate;
            using (var stream = new FileStream(@"E:\Sounds\VoiceOoEb.wav", FileMode.Open, FileAccess.Read))
            {
                var file = WavFile.FromStream(stream);
                rate = file.SampleRate;
                orig = new float[file.Length];
                file.Read(orig, orig.Length);
            }

            //orig = Enumerable.Range(0, orig.Length).Select(i => (float)Math.Sin(349.23f * i * 2 * Math.PI / rate)).ToArray();
            _signal = Freq.FFT(orig);
            //var mags = Freq.Smoothe(Freq.FFT(orig), 100);
            //var phase = Freq.FFTPhase(orig);
            //_signal = mags;
            //Freq.InOut(orig);
            //var cepstrum = Freq.FFT(_signal);
            //_center = rate / (Freq.Center(cepstrum) * 2);
            //_center = rate / Freq.Center(cepstrum);
            //_center = 126f;
            var corr = Freq.Autocorrelate(_signal);
            int fundamental = corr.ToList().IndexOf(corr.Max());
            _center = fundamental;
            float pitch = (_center / _signal.Length) * (rate / 2);

            float fundamentalPower = Enumerable.Range(-fundamental / 8, fundamental / 4).Sum(ii => _signal[fundamental + ii]);
            var harmonics = Enumerable.Range(2, 20).Select(i =>
            {
                return Enumerable.Range(-fundamental / 8, fundamental / 4).Sum(ii => _signal[i * fundamental + ii]) / fundamentalPower;
            }).ToArray();

            var gen = Freq.Generate(orig.Length, pitch, rate, harmonics);
            //var gen = Freq.IFT(mags, phase);
            //float max = Math.Max(Math.Abs(gen.Max()), Math.Abs(gen.Min()));
            //gen = gen.Select(v => v / max).ToArray();
            using (var stream = new FileStream(@"E:\Sounds\GenOoEb.wav", FileMode.Create, FileAccess.Write))
            {
                writeWav(stream, gen, rate);
            }
            //_signal = Freq.FFT(gen);

            _min = _signal.Min();
            _max = _signal.Max();

            int octave = (int)Math.Floor((Math.Round(Math.Log(pitch / 440, Math.Pow(2, 1 / 12f))) + 9) / 12) + 4;
            int note = ((int)Math.Round(Math.Log(pitch / 440, Math.Pow(2, 1 / 12f))) + 120) % 12;
            var noteNames = new[] { "A", "A#/Bb", "B", "C", "C#/Db", "D", "D#/Eb", "E", "F", "F#/Gb", "G", "G#/Ab" };

            this.Text = string.Format("Samples: {0} , Range: {1} - {2} , Note: {3}{4}", _signal.Length, _min, _max, noteNames[note], octave);
            //this.Text = string.Format("Samples: {0} , Range: {1} - {2}", _signal.Length, _min, _max);

            _signal = _signal.Take(4000).ToArray();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, this.DisplayRectangle);

            float c = (_center / _signal.Length) * this.DisplayRectangle.Width;
            e.Graphics.DrawLine(new Pen(Color.Red, 2), c, 0, c, this.DisplayRectangle.Height);

            e.Graphics.DrawLines(Pens.Green, Enumerable.Range(0, this.DisplayRectangle.Width).Select(x => new Point(x, getY(x))).ToArray());
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private int getY(int x)
        {
            float i = x * (float)(_signal.Length - 1) / this.DisplayRectangle.Width;
            float v = _signal.Linear(i);
            return (int)(((_max - v) / (_max - _min)) * this.DisplayRectangle.Height);
        }
    }

    public static class Extensions
    {
        public static float Linear(this float[] signal, float index)
        {
            if (index < 1)
            {
                return signal[0] + index * (signal[1] - signal[0]);
            }

            int i = (int)Math.Ceiling(index);
            float part = i - index;
            return signal[i] + part * (signal[i - 1] - signal[i]);
        }

        public static IEnumerable<Tuple<T, T>> Segments<T>(this IEnumerable<T> items)
        {
            var e = items.GetEnumerator();
            var v1 = e.Current;
            while (e.MoveNext())
            {
                var v2 = e.Current;
                yield return Tuple.Create(v1, v2);
                v1 = v2;
            }
        }
    }
}
