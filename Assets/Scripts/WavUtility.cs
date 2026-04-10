using UnityEngine;
using System.IO;

namespace AiSims
{
    /// <summary>
    /// Utility class to convert Unity AudioClip to WAV byte[].
    /// Only supports PCM16 WAV format (sufficient for Whisper API).
    /// </summary>
    public static class WavUtility
    {
        public static byte[] FromAudioClip(AudioClip clip)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Reserve space for header
                int headerSize = 44;
                stream.Position = headerSize;

                // Convert float samples → PCM16
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                short[] intData = new short[samples.Length];

                byte[] bytesData = new byte[samples.Length * 2];
                int rescaleFactor = 32767;

                for (int i = 0; i < samples.Length; i++)
                {
                    intData[i] = (short)(samples[i] * rescaleFactor);
                    byte[] byteArr = System.BitConverter.GetBytes(intData[i]);
                    byteArr.CopyTo(bytesData, i * 2);
                }

                stream.Write(bytesData, 0, bytesData.Length);

                // Write WAV header
                WriteHeader(stream, clip, bytesData.Length);

                return stream.ToArray();
            }
        }

        private static void WriteHeader(Stream stream, AudioClip clip, int dataLength)
        {
            int hz = clip.frequency;
            int channels = clip.channels;
            int samples = clip.samples;

            stream.Position = 0;

            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(dataLength + 36); // File size - 8
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16); // Subchunk1 size
                writer.Write((ushort)1); // PCM format
                writer.Write((ushort)channels);
                writer.Write(hz);
                writer.Write(hz * channels * 2); // byte rate
                writer.Write((ushort)(channels * 2)); // block align
                writer.Write((ushort)16); // bits per sample
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(dataLength);
            }
        }
    }
}
