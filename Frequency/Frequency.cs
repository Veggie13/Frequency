using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;

namespace Frequency
{
    public static class Frequency
    {
        public static float[] FFT(float[] signal)
        {
            var complexSignal = signal.Select(v => new Complex(v, 0)).ToArray();
            Fourier.Forward(complexSignal, FourierOptions.NoScaling);
            return complexSignal.Take(complexSignal.Length / 2).Select(v => (float)v.Magnitude).ToArray();
        }

        public static float[] FFTPhase(float[] signal)
        {
            var complexSignal = signal.Select(v => new Complex(v, 0)).ToArray();
            Fourier.Forward(complexSignal, FourierOptions.AsymmetricScaling);
            return complexSignal.Take(complexSignal.Length / 2).Select(v => (float)v.Phase).ToArray();
        }

        public static float[] IFT(float[] signal)
        {
            var complexSignal = signal.Concat(signal.Reverse()).Select(v => new Complex(v, 0)).ToArray();
            Fourier.Inverse(complexSignal, FourierOptions.NoScaling);
            return complexSignal.Select(v => (float)v.Magnitude).ToArray();
        }

        public static float[] IFT(float[] magnitudes, float[] phase)
        {
            var complexSignal = Enumerable.Range(0, magnitudes.Length).Select(i => Complex.FromPolarCoordinates(magnitudes[i], phase[i]))
                .Concat(new[] { Complex.Zero })
                .Concat(Enumerable.Range(1, magnitudes.Length - 1).Reverse().Select(i => Complex.FromPolarCoordinates(magnitudes[i], -phase[i])))
                .ToArray();
            Fourier.Inverse(complexSignal, FourierOptions.AsymmetricScaling);
            return complexSignal.Select(v => (float)v.Real).ToArray();
        }

        public static float Center(float[] signal)
        {
            return signal.Select((v, i) => i * v).Sum() / signal.Sum();
        }

        public static float[] Autocorrelate(float[] signal)
        {
            return Enumerable.Range(0, 1000).Select(i =>
            {
                return Enumerable.Range(0, signal.Length - i).Select(ii => signal[i] * signal[i + ii]).Sum();
            }).ToArray();
        }

        public static float[] Generate(int numSamples, float fundamentalFrequency, float sampleRate, params float[] harmonics)
        {
            float samplesPerCycle = sampleRate / fundamentalFrequency;
            var amplitudes = new[] { 1f }.Concat(harmonics);
            return Enumerable.Range(0, numSamples).Select(s =>
            {
                return amplitudes.Select((a, i) => a * waveform(samplesPerCycle / (i + 1))(s)).Sum();
            }).ToArray();
        }

        private static Func<float, float> waveform(float samplesPerCycle)
        {
            return v => (float)Math.Sin(v * 2 * Math.PI / samplesPerCycle);
        }

        public static float[] Smoothe(float[] signal, int window)
        {
            return Enumerable.Range(0, window).Select(i => signal[i])
                .Concat(Enumerable.Range(window, signal.Length - 2 * window).Select(i => Enumerable.Range(i - window, window * 2 + 1).Average(ii => signal[ii])))
                .Concat(Enumerable.Range(signal.Length - window, window).Select(i => signal[i]))
                .ToArray();
        }

        public static float[] InOut(float[] signal)
        {
            var complexSignal = signal.Select(v => new Complex(v, 0)).ToArray();
            Fourier.Forward(complexSignal, FourierOptions.AsymmetricScaling);
            var mags = complexSignal.Select(v => (float)v.Magnitude).Take(complexSignal.Length / 2).ToArray();
            var phase = complexSignal.Select(v => (float)v.Phase).Take(complexSignal.Length / 2).ToArray();
            var outs = Enumerable.Range(0, mags.Length).Select(i => Complex.FromPolarCoordinates(mags[i], phase[i]))
                .Concat(new[] { Complex.Zero })
                .Concat(Enumerable.Range(1, mags.Length - 1).Reverse().Select(i => Complex.FromPolarCoordinates(mags[i], -phase[i])))
                .ToArray();
            Fourier.Inverse(outs, FourierOptions.AsymmetricScaling);
            var result = outs.Select(v => (float)v.Real).ToArray();
            return result;
        }
    }
}
