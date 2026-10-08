using System;
using System.IO;
using OggVorbisEncoder;

namespace RSDSwissKnife
{
    public static class OggConverter
    {
        public static void ConvertWavToOgg(string inputWavPath, string outputOggPath, float quality = 0.4f)
        {
            if (!File.Exists(inputWavPath))
                throw new FileNotFoundException("Input WAV file not found.", inputWavPath);

            byte[] wavBytes = File.ReadAllBytes(inputWavPath);
            using MemoryStream ms = new MemoryStream(wavBytes);
            using BinaryReader reader = new BinaryReader(ms);

           
            if (ms.Length < 12)
                throw new InvalidDataException("WAV file is too small to contain a valid header.");

            string riff = new string(reader.ReadChars(4));
            int chunkSize = reader.ReadInt32();
            string wave = new string(reader.ReadChars(4));

            if (riff != "RIFF" || wave != "WAVE")
                throw new InvalidDataException("Invalid or unsupported WAV file format.");

            short channels = 0;
            int sampleRate = 0;
            short bitsPerSample = 0;
            byte[] pcmData = null;

            while (ms.Position < ms.Length)
            {
                if (ms.Position + 8 > ms.Length) break;
                string chunkId = new string(reader.ReadChars(4));
                int size = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    reader.ReadInt16(); 
                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadInt32(); 
                    reader.ReadInt16(); 
                    bitsPerSample = reader.ReadInt16();

                    if (size > 16) reader.BaseStream.Position += (size - 16);
                }
                else if (chunkId == "data")
                {
                    pcmData = reader.ReadBytes(size);
                    break;
                }
                else
                {
                    reader.BaseStream.Position += size;
                }
            }

          
            if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0)
                throw new InvalidDataException($"Invalid WAV format parameters (Channels: {channels}, SampleRate: {sampleRate}, BitsPerSample: {bitsPerSample}).");

            if (pcmData == null || pcmData.Length == 0)
                throw new InvalidDataException("WAV file contains no audio PCM data (0 bytes).");

           
            int bytesPerSample = bitsPerSample / 8;
            if (bytesPerSample <= 0)
                throw new InvalidDataException($"Invalid BitsPerSample value: {bitsPerSample}. Must be 8 or 16.");

            int frameSize = bytesPerSample * channels;
            int totalFrames = pcmData.Length / frameSize;

            if (totalFrames <= 0)
                throw new InvalidDataException("PCM data size is too small for a single audio frame.");

            // Setup
            var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, quality);
            var serial = new Random().Next();
            var processingState = ProcessingState.Create(info);

            using FileStream outputStream = new FileStream(outputOggPath, FileMode.Create, FileAccess.Write, FileShare.None);

            // Write Headers
            var infoPacket = HeaderPacketBuilder.BuildInfoPacket(info);
            var commentsPacket = HeaderPacketBuilder.BuildCommentsPacket(new Comments());
            var booksPacket = HeaderPacketBuilder.BuildBooksPacket(info);

            var oggStream = new OggStream(serial);
            oggStream.PacketIn(infoPacket);
            oggStream.PacketIn(commentsPacket);
            oggStream.PacketIn(booksPacket);

            FlushPages(oggStream, outputStream, true);

            int chunkSizeSamples = 2048;
            int framesProcessed = 0;

            // ugly nesting begins
            while (framesProcessed < totalFrames)
            {
                int count = Math.Min(chunkSizeSamples, totalFrames - framesProcessed);
                float[][] sampleBuffers = new float[channels][];

                for (int c = 0; c < channels; c++)
                {
                    sampleBuffers[c] = new float[count];
                }

                for (int i = 0; i < count; i++)
                {
                    int sampleOffset = (framesProcessed + i) * frameSize;

                    for (int c = 0; c < channels; c++)
                    {
                        int samplePosition = sampleOffset + (c * bytesPerSample);

                        if (samplePosition + bytesPerSample > pcmData.Length)
                            break;

                        if (bitsPerSample == 16)
                        {
                            short sample16 = BitConverter.ToInt16(pcmData, samplePosition);
                            sampleBuffers[c][i] = sample16 / 32768f;
                        }
                        else if (bitsPerSample == 8)
                        {
                            byte sample8 = pcmData[samplePosition];
                            sampleBuffers[c][i] = (sample8 - 128) / 128f;
                        }
                    }
                }

                processingState.WriteData(sampleBuffers, count);
                framesProcessed += count;

                while (processingState.PacketOut(out OggPacket packet))
                {
                    oggStream.PacketIn(packet);
                    FlushPages(oggStream, outputStream, false);
                }
            }

            // End of stream
            processingState.WriteEndOfStream();

            while (processingState.PacketOut(out OggPacket packet))
            {
                oggStream.PacketIn(packet);
                FlushPages(oggStream, outputStream, false);
            }

            // flush the turd
            FlushPages(oggStream, outputStream, true);
        }

        private static void FlushPages(OggStream oggStream, Stream outputStream, bool force)
        {
            while (oggStream.PageOut(out OggPage page, force))
            {
                outputStream.Write(page.Header, 0, page.Header.Length);
                outputStream.Write(page.Body, 0, page.Body.Length);
            }
        }
    }
}