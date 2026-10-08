using System;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Forms;
using NVorbis;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace RSDSwissKnife
{
    public class PlaybackProgressEventArgs : EventArgs
    {
        public TimeSpan CurrentTime { get; }
        public TimeSpan TotalTime { get; }

        public PlaybackProgressEventArgs(TimeSpan currentTime, TimeSpan totalTime)
        {
            CurrentTime = currentTime;
            TotalTime = totalTime;
        }
    }

    public class RSDAudioPlayer : IDisposable
    {
        private SoundPlayer _audioPlayer;
        private WinFormsTimer _playbackTimer;
        private DateTime _playbackStartTime;
        private TimeSpan _currentAudioDuration;
        private string _tempPlaybackPath;

        public bool IsPlaying => _audioPlayer != null;
        public bool IsLooping { get; set; }

        public event EventHandler<PlaybackProgressEventArgs> ProgressUpdated;
        public event EventHandler PlaybackStopped;

        public RSDAudioPlayer()
        {
            _playbackTimer = new WinFormsTimer { Interval = 100 };
            _playbackTimer.Tick += PlaybackTimer_Tick;
        }

        public void Play(string filePath)
        {
            Stop();

            if (!File.Exists(filePath)) return;

            string ext = Path.GetExtension(filePath).ToLower();
            string wavToPlay = filePath;

           
            if (ext == ".rsd")
            {
                _tempPlaybackPath = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid()}.wav");
                string headerType = GetRsdHeaderType(filePath);

                switch (headerType)
                {
                    case "RADP":
                        RSDDecoder.ConvertRadpToWav(filePath, _tempPlaybackPath);
                        break;
                    case "RSD4PCM":
                        RSDDecoder.ConvertPcmToWav(filePath, _tempPlaybackPath);
                        break;
                    case "RSD4PCMB":
                        RSDDecoder.ConvertPcmbToWav(filePath, _tempPlaybackPath);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported RSD format: '{headerType}'");
                }

                wavToPlay = _tempPlaybackPath;
            }
            
            else if (ext == ".ogg")
            {
                _tempPlaybackPath = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid()}.wav");
                DecodeOggToTempWav(filePath, _tempPlaybackPath);
                wavToPlay = _tempPlaybackPath;
            }
            else if (ext != ".wav")
            {
                throw new InvalidOperationException("Playback supports .RSD, .WAV, and .OGG files.");
            }

            _currentAudioDuration = GetWavDuration(wavToPlay);
            _audioPlayer = new SoundPlayer(wavToPlay);

            if (IsLooping)
            {
                _audioPlayer.PlayLooping();
            }
            else
            {
                _audioPlayer.Play();
            }

            _playbackStartTime = DateTime.Now;
            _playbackTimer.Start();
        }

        public void Stop()
        {
            if (_audioPlayer != null)
            {
                _audioPlayer.Stop();
                _audioPlayer.Dispose();
                _audioPlayer = null;
            }

            _playbackTimer.Stop();
            CleanupTempFile();

            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - _playbackStartTime;

            if (!IsLooping && elapsed >= _currentAudioDuration)
            {
                Stop();
                ProgressUpdated?.Invoke(this, new PlaybackProgressEventArgs(_currentAudioDuration, _currentAudioDuration));
                return;
            }

            if (IsLooping && _currentAudioDuration.TotalMilliseconds > 0)
            {
                double currentMs = elapsed.TotalMilliseconds % _currentAudioDuration.TotalMilliseconds;
                elapsed = TimeSpan.FromMilliseconds(currentMs);
            }

            ProgressUpdated?.Invoke(this, new PlaybackProgressEventArgs(elapsed, _currentAudioDuration));
        }

        private void DecodeOggToTempWav(string oggPath, string tempWavPath)
        {
            using VorbisReader vorbis = new VorbisReader(oggPath);
            using FileStream fs = new FileStream(tempWavPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using BinaryWriter bw = new BinaryWriter(fs);

            int channels = vorbis.Channels;
            int sampleRate = vorbis.SampleRate;

            // I dont know why i keep rewriting this all the time.
            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(0); // Reserved for total RIFF size, listen to Sick of It All for MAXIMUM RIFF SIZE please RIP Lou.
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // Subchunk size (PCM)
            bw.Write((short)1); // Audio format (1 = PCM)
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * sizeof(short)); // Byte rate
            bw.Write((short)(channels * sizeof(short)));    // Block align
            bw.Write((short)16);                            // Bits per sample

            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(0); 

            long dataStartPos = fs.Position;
            int totalBytesWritten = 0;

            // Read and stream samples in 4096-float chunks
            float[] readBuffer = new float[4096];
            int readCount;

            while ((readCount = vorbis.ReadSamples(readBuffer, 0, readBuffer.Length)) > 0)
            {
                for (int i = 0; i < readCount; i++)
                {
                    float sample = Math.Max(-1.0f, Math.Min(1.0f, readBuffer[i]));
                    short sample16 = (short)(sample * 32767f);
                    bw.Write(sample16);
                    totalBytesWritten += 2;
                }
            }

            // Back-patch header with exact sizes
            fs.Position = 4;
            bw.Write(36 + totalBytesWritten); // RIFF size

            fs.Position = dataStartPos - 4;
            bw.Write(totalBytesWritten);     // Data chunk size
        }

        private string GetRsdHeaderType(string filePath)
        {
            try
            {
                using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Length < 8) return "UNKNOWN";

                byte[] headerBytes = new byte[8];
                fs.Read(headerBytes, 0, 8);
                string rawHeader = Encoding.ASCII.GetString(headerBytes).Replace("\0", "").Trim();

                if (rawHeader.StartsWith("RSD4RADP")) return "RADP";
                if (rawHeader.StartsWith("RADPCMB")) return "PCMB";
                if (rawHeader.StartsWith("RADPCM")) return "PCM";

                return rawHeader;
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        private TimeSpan GetWavDuration(string wavFilePath)
        {
            using (FileStream fs = new FileStream(wavFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                if (fs.Length < 44) return TimeSpan.Zero;

                fs.Position = 12; // Skip RIFF header

                short channels = 0;
                int sampleRate = 0;
                short bitsPerSample = 0;
                int dataSize = 0;

                while (fs.Position < fs.Length)
                {
                    if (fs.Position + 8 > fs.Length) break;
                    string chunkId = new string(br.ReadChars(4));
                    int chunkSize = br.ReadInt32();

                    if (chunkId == "fmt ")
                    {
                        br.ReadInt16(); // Compression code
                        channels = br.ReadInt16();
                        sampleRate = br.ReadInt32();
                        br.ReadInt32(); // Avg bytes/sec
                        br.ReadInt16(); // Block align
                        bitsPerSample = br.ReadInt16();
                        if (chunkSize > 16) fs.Position += (chunkSize - 16);
                    }
                    else if (chunkId == "data")
                    {
                        dataSize = chunkSize;
                        break;
                    }
                    else
                    {
                        fs.Position += chunkSize; // Skip non-data chunks
                    }
                }

                int bytesPerSample = (bitsPerSample / 8) * channels;
                double totalSeconds = (bytesPerSample > 0 && sampleRate > 0) ? (double)dataSize / (sampleRate * bytesPerSample) : 0;

                return TimeSpan.FromSeconds(totalSeconds);
            }
        }

        private void CleanupTempFile()
        {
            if (!string.IsNullOrEmpty(_tempPlaybackPath) && File.Exists(_tempPlaybackPath))
            {
                try { File.Delete(_tempPlaybackPath); } catch { }
                _tempPlaybackPath = null;
            }
        }

        public void Dispose()
        {
            Stop();
            _playbackTimer?.Dispose();
        }
    }
}