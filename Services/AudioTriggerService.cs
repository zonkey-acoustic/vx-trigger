using NAudio.Wave;

namespace ShotTrigger.Services;

public class AudioTriggerService : IDisposable
{
    private WaveOutEvent? _waveOut;
    private bool _disposed;

    public bool IsEnabled { get; set; }
    public int SelectedDeviceIndex { get; set; } = -1;
    public string? SelectedDeviceName { get; set; }

    // Tone envelope parameters
    public double ToneFrequencyHz { get; set; } = 5800;
    public double ToneNoiseDecay { get; set; } = 60;
    public double ToneToneDecay { get; set; } = 200;
    public double ToneMix { get; set; } = 0.1;
    public double ToneDurationMs { get; set; } = 500;

    public List<AudioDeviceInfo> GetAudioOutputDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var capabilities = WaveOut.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo
            {
                Index = i,
                Name = capabilities.ProductName
            });
        }

        return devices;
    }

    public void PlayTriggerTone()
    {
        if (!IsEnabled || SelectedDeviceIndex < 0)
            return;

        PlayToneOnDevice(SelectedDeviceIndex);
    }

    public void TestTone(int deviceIndex)
    {
        PlayToneOnDevice(deviceIndex);
    }

    public void TestTone(int deviceIndex, double freq, double noiseDecay,
        double toneDecay, double mix, double durationMs)
    {
        PlayToneOnDevice(deviceIndex, freq, noiseDecay, toneDecay, mix, durationMs);
    }

    private void PlayToneOnDevice(int deviceIndex, double? freq = null, double? noiseDecay = null,
        double? toneDecay = null, double? mix = null, double? durationMs = null)
    {
        try
        {
            StopPlayback();

            _waveOut = new WaveOutEvent
            {
                DeviceNumber = deviceIndex
            };

            var impactSound = new GolfImpactSampleProvider(
                freq ?? ToneFrequencyHz,
                noiseDecay ?? ToneNoiseDecay,
                toneDecay ?? ToneToneDecay,
                mix ?? ToneMix,
                durationMs ?? ToneDurationMs);
            _waveOut.Init(impactSound);
            _waveOut.PlaybackStopped += (s, e) =>
            {
                var wo = s as WaveOutEvent;
                wo?.Dispose();
                if (_waveOut == wo)
                    _waveOut = null;
            };

            _waveOut.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing audio trigger: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a realistic golf club impact sound.
    /// </summary>
    private class GolfImpactSampleProvider : ISampleProvider
    {
        private readonly WaveFormat _waveFormat;
        private readonly Random _random;
        private int _sampleIndex;
        private readonly int _totalSamples;
        private readonly int _sampleRate;

        private readonly double _frequencyHz;
        private readonly double _noiseDecay;
        private readonly double _toneDecay;
        private readonly double _toneMix;

        public WaveFormat WaveFormat => _waveFormat;

        public GolfImpactSampleProvider(
            double frequencyHz = 3200,
            double noiseDecay = 60,
            double toneDecay = 8,
            double toneMix = 0.7,
            double durationMs = 500)
        {
            _sampleRate = 44100;
            _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_sampleRate, 1);
            _random = new Random();

            _frequencyHz = frequencyHz;
            _noiseDecay = noiseDecay;
            _toneDecay = toneDecay;
            _toneMix = toneMix;

            _totalSamples = (int)(_sampleRate * durationMs / 1000.0);
            _sampleIndex = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesWritten = 0;

            for (int i = 0; i < count; i++)
            {
                if (_sampleIndex >= _totalSamples)
                {
                    buffer[offset + i] = 0;
                    samplesWritten++;
                    continue;
                }

                double timeSec = _sampleIndex / (double)_sampleRate;
                double sample = GenerateSound(timeSec);
                sample = Math.Max(-1.0, Math.Min(1.0, sample));

                buffer[offset + i] = (float)sample;
                _sampleIndex++;
                samplesWritten++;
            }

            if (_sampleIndex >= _totalSamples && samplesWritten == count)
            {
                bool allZeros = true;
                for (int i = 0; i < count && allZeros; i++)
                {
                    if (buffer[offset + i] != 0) allZeros = false;
                }
                if (allZeros) return 0;
            }

            return samplesWritten;
        }

        private double GenerateSound(double timeSec)
        {
            double tone = Math.Sin(2.0 * Math.PI * _frequencyHz * timeSec);
            double toneEnvelope = Math.Exp(-_toneDecay * timeSec);
            double toneComponent = tone * toneEnvelope;

            double noise = _random.NextDouble() * 2.0 - 1.0;
            double noiseEnvelope = Math.Exp(-_noiseDecay * timeSec);
            double noiseComponent = noise * noiseEnvelope;

            return (toneComponent * _toneMix) + (noiseComponent * (1.0 - _toneMix));
        }
    }

    private void StopPlayback()
    {
        if (_waveOut != null)
        {
            try
            {
                _waveOut.Stop();
                _waveOut.Dispose();
            }
            catch { }
            _waveOut = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopPlayback();
    }
}

public class AudioDeviceInfo
{
    public int Index { get; set; }
    public required string Name { get; set; }
}
