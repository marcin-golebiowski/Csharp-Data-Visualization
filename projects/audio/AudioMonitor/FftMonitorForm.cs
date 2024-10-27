using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Numerics;

namespace AudioMonitor;

public partial class FftMonitorForm : Form
{
    readonly double[] LeftAudioValues;
    readonly double[] RightAudioValues;

    readonly WasapiCapture AudioDevice;
    readonly double[] LeftFftValues;
    readonly double[] RightFftValues;

    public FftMonitorForm(WasapiCapture audioDevice)
    {
        InitializeComponent();
        AudioDevice = audioDevice;
        WaveFormat fmt = audioDevice.WaveFormat;



        LeftAudioValues = new double[fmt.SampleRate];
        double[] paddedAudio = FftSharp.Pad.ZeroPad(LeftAudioValues);
        var fft = FftSharp.FFT.Forward(paddedAudio);
        var mag = FftSharp.FFT.Magnitude(fft);
        LeftFftValues = new double[fft.Length];
        double fftPeriod = FftSharp.FFT.FrequencyResolution(fmt.SampleRate, mag.Length);
        formsPlot1.Plot.Add.Signal(LeftFftValues, 1.0 / (fftPeriod));
        formsPlot1.Plot.YLabel("Spectral Power");
        formsPlot1.Plot.XLabel("Frequency (kHz)");
        formsPlot1.Plot.Title($"{fmt.Encoding} ({fmt.BitsPerSample}-bit) {fmt.SampleRate} KHz");
        formsPlot1.Plot.Axes.SetLimits(0, 6000, 0, 0.005);
        formsPlot1.Refresh();


        RightAudioValues = new double[fmt.SampleRate];
        paddedAudio = FftSharp.Pad.ZeroPad(RightAudioValues);
        fft = FftSharp.FFT.Forward(paddedAudio);
        mag = FftSharp.FFT.Magnitude(fft);
        RightFftValues = new double[fft.Length];
        fftPeriod = FftSharp.FFT.FrequencyResolution(fmt.SampleRate, mag.Length);
        formsPlot2.Plot.Add.Signal(RightFftValues, 1.0 / (fftPeriod));
        formsPlot2.Plot.YLabel("Spectral Power");
        formsPlot2.Plot.XLabel("Frequency (kHz)");
        formsPlot2.Plot.Title($"{fmt.Encoding} ({fmt.BitsPerSample}-bit) {fmt.SampleRate} KHz");
        formsPlot2.Plot.Axes.SetLimits(0, 6000, 0, 0.005);
        formsPlot2.Refresh();


        AudioDevice.StartRecording();
        AudioDevice.DataAvailable += WaveIn_DataAvailable;
        FormClosed += FftMonitorForm_FormClosed;
    }

    private void FftMonitorForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Closing audio device: {AudioDevice}");
        AudioDevice.StopRecording();
        AudioDevice.Dispose();
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        int bytesPerSamplePerChannel = AudioDevice.WaveFormat.BitsPerSample / 8;
        int bytesPerSample = bytesPerSamplePerChannel * AudioDevice.WaveFormat.Channels;
        int bufferSampleCount = e.Buffer.Length / bytesPerSample;

        if (bufferSampleCount >= LeftAudioValues.Length)
        {
            bufferSampleCount = LeftAudioValues.Length;
        }

        if (bytesPerSamplePerChannel == 2 && AudioDevice.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            for (int i = 0; i < bufferSampleCount; i++)
            {
                LeftAudioValues[i] = BitConverter.ToInt16(e.Buffer, i * bytesPerSample);
                RightAudioValues[i] = BitConverter.ToInt16(e.Buffer, i * bytesPerSample + 2);
            }
        }
        else if (bytesPerSamplePerChannel == 4 && AudioDevice.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            for (int i = 0; i < bufferSampleCount; i++)
            {
                LeftAudioValues[i] = BitConverter.ToInt32(e.Buffer, i * bytesPerSample);
                RightAudioValues[i] = BitConverter.ToInt32(e.Buffer, i * bytesPerSample + 4);
            }
        }
        else if (bytesPerSamplePerChannel == 4 && AudioDevice.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            for (int i = 0; i < bufferSampleCount; i++)
            {
                LeftAudioValues[i] = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
                RightAudioValues[i] = BitConverter.ToSingle(e.Buffer, i * bytesPerSample + bytesPerSamplePerChannel);
            }
        }
        else
        {
            throw new NotSupportedException(AudioDevice.WaveFormat.ToString());
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        double[] paddedAudio = FftSharp.Pad.ZeroPad(LeftAudioValues);
        var fft = FftSharp.FFT.Forward(paddedAudio);
        var mag = FftSharp.FFT.Magnitude(fft);
        Array.Copy(mag, LeftFftValues, mag.Length);

        // request a redraw using a non-blocking render queue
        formsPlot1.Refresh();

        paddedAudio = FftSharp.Pad.ZeroPad(RightAudioValues);
        fft = FftSharp.FFT.Forward(paddedAudio);
        mag = FftSharp.FFT.Magnitude(fft);
        Array.Copy(mag, RightFftValues, mag.Length);

        formsPlot2.Refresh();
    }
}
