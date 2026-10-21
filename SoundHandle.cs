using NAudio.Wave;
using System.IO;

namespace rans0m;

/// <summary>
/// Handles a WAV clip and its playback callback. PlaybackStopped may arrive later
/// from another thread; it must never touch a reader already disposed by Finish().
/// </summary>
public sealed class SoundHandle
{
    private readonly object _gate = new();
    private readonly WaveOut _waveOut;
    private readonly WaveFileReader _reader;
    private bool _disposed;
    private bool _looping;

    private SoundHandle(WaveOut waveOut, WaveFileReader reader)
    {
        _waveOut = waveOut;
        _reader = reader;
        _waveOut.PlaybackStopped += OnPlaybackStopped;
    }

    public static SoundHandle Create(Stream wavStream)
    {
        WaveFileReader? reader = null;
        WaveOut? output = null;
        try
        {
            reader = new WaveFileReader(wavStream);
            output = new WaveOut();
            output.Init(reader);
            return new SoundHandle(output, reader);
        }
        catch
        {
            try { output?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try { wavStream.Dispose(); } catch { }
            throw;
        }
    }

    public void Play()
    {
        lock (_gate)
        {
            if (_disposed) return;
            try { _waveOut.Play(); } catch (Exception ex) { LabDiagnostics.Write("Audio Play: " + ex); }
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (_disposed) return;
            try { _waveOut.Pause(); } catch (Exception ex) { LabDiagnostics.Write("Audio Pause: " + ex); }
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            if (_disposed) return;
            try { _reader.Position = 0; } catch (Exception ex) { LabDiagnostics.Write("Audio Reset: " + ex); }
        }
    }

    public void PlayLooping()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _looping = true;
            try { _waveOut.Play(); }
            catch (Exception ex) { _looping = false; LabDiagnostics.Write("Audio Loop: " + ex); }
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // This handler can be queued BEFORE Stop() but delivered AFTER Dispose().
        // _gate and _disposed prevent access to an already-disposed WaveFileReader.
        try
        {
            lock (_gate)
            {
                if (_disposed) return;
                if (e.Exception != null)
                {
                    _looping = false;
                    LabDiagnostics.Write("Audio playback failed: " + e.Exception);
                    return;
                }
                _reader.Position = 0;
                if (_looping) _waveOut.Play();
            }
        }
        catch (Exception ex)
        {
            // Never let a background NAudio callback terminate the WPF process.
            LabDiagnostics.Write("Audio callback: " + ex);
        }
    }

    public void Stop()
    {
        // Disarm and unsubscribe BEFORE stopping/disposal: Stop() may emit
        // PlaybackStopped asynchronously. In-flight handlers recheck _disposed.
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _looping = false;
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
        }
        try { _waveOut.Stop(); } catch (Exception ex) { LabDiagnostics.Write("Audio Stop: " + ex); }
        try { _waveOut.Dispose(); } catch (Exception ex) { LabDiagnostics.Write("Audio Dispose: " + ex); }
        try { _reader.Dispose(); } catch (Exception ex) { LabDiagnostics.Write("Audio Reader Dispose: " + ex); }
    }
}
