using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace RSDSwissKnife
{
    public static class RSDDecoder
    {
        // IMA ADPCM PAGE says this, so we doing this shit
        // it seems right?
        private static readonly int[] StepTable = new int[]
        {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
            19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
            50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
            876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
            2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
            5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
        };

        private static readonly int[] IndexTable = new int[]
        {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8
        }; 

        public static void ConvertRadpToWav(string inputRsdPath, string outputWavPath)
        {
            byte[] bytes = File.ReadAllBytes(inputRsdPath);
            if (bytes.Length < 2048)
                throw new InvalidDataException("File too small for RSD format.");

            int channels = BitConverter.ToInt32(bytes, 8);
            int sampleRate = BitConverter.ToInt32(bytes, 16);
            if (channels < 1) channels = 1;

            int dataOffset = 2048;
            int frameHeaderBytes = 4 * channels;
            int frameDataBytes = 16 * channels;
            int frameSize = frameHeaderBytes + frameDataBytes;

            int numFrames = (bytes.Length - dataOffset) / frameSize;
            List<short> pcmList = new List<short>(numFrames * 32 * channels);
            int pSource = dataOffset;

            int[] channelIndex = new int[channels];
            int[] channelPrev = new int[channels];

            for (int f = 0; f < numFrames; f++)
            {
                
                for (int c = 0; c < channels; c++)
                {
                    channelIndex[c] = BitConverter.ToInt16(bytes, pSource);
                    pSource += 2;
                    channelPrev[c] = BitConverter.ToInt16(bytes, pSource);
                    pSource += 2;

                    channelIndex[c] = Math.Clamp(channelIndex[c], 0, 88);
                }

                // Decode 32 samples per channel
                short[][] decodedChannelSamples = new short[channels][];

                for (int c = 0; c < channels; c++)
                {
                    short[] samples = new short[32];
                    int stepIndex = channelIndex[c];
                    int prevValue = channelPrev[c];

                    for (int b = 0; b < 16; b++)
                    {
                        byte byteVal = bytes[pSource++];
                        int n1 = byteVal & 0x0F;
                        int n2 = (byteVal >> 4) & 0x0F;

                        samples[b * 2] = DecodeRadicalNibble(n1, ref stepIndex, ref prevValue);
                        samples[b * 2 + 1] = DecodeRadicalNibble(n2, ref stepIndex, ref prevValue);
                    }

                    decodedChannelSamples[c] = samples;
                }

                // Interleave frame samples
                for (int s = 0; s < 32; s++)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        pcmList.Add(decodedChannelSamples[c][s]);
                    }
                }
            }

            // Write output WAV 
            WriteWavFile(outputWavPath, pcmList.ToArray(), channels, sampleRate);
        }

        private static short DecodeRadicalNibble(int delta, ref int stepIndex, ref int prevValue)
        {
            // I hate/love radical! At least this isnt tooooo different.
            int step = StepTable[stepIndex];
            int difference = step >> 3;

            if ((delta & 1) != 0) difference += step >> 2;
            if ((delta & 2) != 0) difference += step >> 1;
            if ((delta & 4) != 0) difference += step;
            if ((delta & 8) != 0) difference = -difference;

            prevValue += difference;
            prevValue = Math.Clamp(prevValue, -32768, 32767);

            stepIndex += IndexTable[delta];
            stepIndex = Math.Clamp(stepIndex, 0, 88);

            return (short)prevValue;
        }

        private static short[] InterleaveChannels(short[][] channelsData, int channelCount, int samplesPerChannel)
        {
            short[] interleaved = new short[channelCount * samplesPerChannel];
            for (int i = 0; i < samplesPerChannel; i++)
            {
                for (int c = 0; c < channelCount; c++)
                {
                    interleaved[i * channelCount + c] = channelsData[c][i];
                }
            }
            return interleaved;
        }

        private static void WriteWavFile(string outputPath, short[] pcmData, int channels, int sampleRate)
        {
            byte[] pcmBytes = new byte[pcmData.Length * sizeof(short)];
            Buffer.BlockCopy(pcmData, 0, pcmBytes, 0, pcmBytes.Length);

            using FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + pcmBytes.Length);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((short)(channels * 2));
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(pcmBytes.Length);
            bw.Write(pcmBytes);

            
            
        }

        public static void ConvertPcmToWav(string inputFile, string outputFile)
        {
            byte[] bytes = File.ReadAllBytes(inputFile);

            int channels = BitConverter.ToInt32(bytes, 8);
            int bits = BitConverter.ToInt32(bytes, 12);
            int sampleRate = BitConverter.ToInt32(bytes, 16);

            if (bits != 16)
            {
                // Your file is garbage you thick motherfucker
                throw new InvalidOperationException($"Only 16-bit PCM is supported (found {bits}).");
            }

            int dataOffset = 52;
            int pcmLength = bytes.Length - dataOffset;
            byte[] pcmData = new byte[pcmLength];
            Array.Copy(bytes, dataOffset, pcmData, 0, pcmLength);

            byte[] wavData = BuildWavHeaderAndData(pcmData, channels, sampleRate);
            File.WriteAllBytes(outputFile, wavData);
        }

        public static void ConvertPcmbToWav(string inputFile, string outputFile)
        {
            byte[] bytes = File.ReadAllBytes(inputFile);

            int channels = BitConverter.ToInt32(bytes, 8);
            int sampleRate = BitConverter.ToInt32(bytes, 16);

            int dataOffset = 52;
            int pcmLength = bytes.Length - dataOffset;
            byte[] pcmData = new byte[pcmLength];
            Array.Copy(bytes, dataOffset, pcmData, 0, pcmLength);

            // Swap large indian samples to little indian
            for (int i = 0; i < pcmData.Length - 1; i += 2)
            {
                byte tmp = pcmData[i];
                pcmData[i] = pcmData[i + 1];
                pcmData[i + 1] = tmp;
            }

            byte[] wavData = BuildWavHeaderAndData(pcmData, channels, sampleRate);
            File.WriteAllBytes(outputFile, wavData);
        }

        private static byte[] BuildWavHeaderAndData(byte[] pcmData, int channels, int sampleRate)
        {
            List<byte> wav = new List<byte>();

            wav.AddRange(Encoding.ASCII.GetBytes("RIFF"));
            wav.AddRange(BitConverter.GetBytes(36 + pcmData.Length));
            wav.AddRange(Encoding.ASCII.GetBytes("WAVE"));
            wav.AddRange(Encoding.ASCII.GetBytes("fmt "));
            wav.AddRange(BitConverter.GetBytes(16)); // Chunk size
            wav.AddRange(BitConverter.GetBytes((short)1)); // PCM Format
            wav.AddRange(BitConverter.GetBytes((short)channels));
            wav.AddRange(BitConverter.GetBytes(sampleRate));
            wav.AddRange(BitConverter.GetBytes(sampleRate * channels * 2)); // Byte rate
            wav.AddRange(BitConverter.GetBytes((short)(channels * 2))); // Block align
            wav.AddRange(BitConverter.GetBytes((short)16)); // Bits per sample
            wav.AddRange(Encoding.ASCII.GetBytes("data"));
            wav.AddRange(BitConverter.GetBytes(pcmData.Length));
            wav.AddRange(pcmData);

            return wav.ToArray();
        }
    }


}