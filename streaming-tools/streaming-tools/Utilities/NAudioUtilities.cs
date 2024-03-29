﻿// <auto-generated/>
namespace streaming_tools.Utilities {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    using NAudio;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    /// <summary>
    ///     Utilities for simplifying interactions with the NAudio library.
    /// </summary>
    public static class NAudioUtilities {
        /// <summary>
        ///     Retrieves the input device.
        /// </summary>
        /// <param name="index">Device to retrieve.</param>
        /// <returns>The input device capabilities.</returns>
        public static WaveInCapabilities GetInputDevice(int index) {
            var caps = new WaveInCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr)index, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        ///     Retrieves the output device.
        /// </summary>
        /// <param name="index">The index of the device.</param>
        /// <returns>The output device capabilities.</returns>
        public static WaveOutCapabilities GetOutputDevice(int index) {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)index, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        /// <summary>
        ///     Converts the device name to an index.
        /// </summary>
        /// <param name="name">The name of the device.</param>
        /// <returns>The index of the device if found, -1 otherwise.</returns>
#pragma warning disable 8669
        public static int GetOutputDeviceIndex(string? name) {
#pragma warning restore 8669
            if (string.IsNullOrWhiteSpace(name))
                return -1;

            for (var i = 0; i < GetTotalOutputDevices(); i++) {
                var capability = GetOutputDevice(i);

                if (name.Equals(capability.ProductName, StringComparison.InvariantCultureIgnoreCase)) return i;
            }

            return -1;
        }

        /// <summary>
        ///     Get the total number of devices according to NAudio's wave functionality.
        /// </summary>
        /// <returns>The number of devices.</returns>
        public static int GetTotalInputDevices() {
            return WaveInterop.waveInGetNumDevs();
        }

        /// <summary>
        ///     Returns the number of output devices available in the system.
        /// </summary>
        /// <remarks>Add two to the end to get all devices?</remarks>
        /// <returns>The total number of output devices.</returns>
        public static int GetTotalOutputDevices() {
            return WaveInterop.waveOutGetNumDevs();
        }

        /// <summary>
        ///     AudioFileReader simplifies opening an audio file in NAudio
        ///     Simply pass in the filename, and it will attempt to open the
        ///     file and set up a conversion path that turns into PCM IEEE float.
        ///     ACM codecs will be used for conversion.
        ///     It provides a volume property and implements both WaveStream and
        ///     ISampleProvider, making it possibly the only stage in your audio
        ///     pipeline necessary for simple playback scenarios
        /// </summary>
        public class AudioFileReader : WaveStream, ISampleProvider {
            private readonly int destBytesPerSample;

            private readonly object lockObject;

            private readonly SampleChannel sampleChannel; // sample provider that gives us most stuff we need

            private readonly int sourceBytesPerSample;

            private WaveStream readerStream; // the waveStream which we will use for all positioning

            /// <summary>
            ///     Initializes a new instance of AudioFileReader
            /// </summary>
            /// <param name="fileName">The file to open</param>
            public AudioFileReader(string fileName) {
                this.lockObject = new object();
                this.FileName = fileName;
                this.CreateReaderStream(fileName);
                this.sourceBytesPerSample = this.readerStream.WaveFormat.BitsPerSample / 8 * this.readerStream.WaveFormat.Channels;
                this.sampleChannel = new SampleChannel(this.readerStream, false);
                this.destBytesPerSample = 4 * this.sampleChannel.WaveFormat.Channels;
                this.Length = this.SourceToDest(this.readerStream.Length);
            }

            /// <summary>
            ///     File Name
            /// </summary>
            public string FileName { get; }

            /// <summary>
            ///     Length of this stream (in bytes)
            /// </summary>
            public override long Length { get; }

            /// <summary>
            ///     Position of this stream (in bytes)
            /// </summary>
            public override long Position {
                get => this.SourceToDest(this.readerStream.Position);
                set {
                    lock (this.lockObject) {
                        this.readerStream.Position = this.DestToSource(value);
                    }
                }
            }

            /// <summary>
            ///     Gets or Sets the Volume of this AudioFileReader. 1.0f is full volume
            /// </summary>
            public float Volume {
                get => this.sampleChannel.Volume;
                set => this.sampleChannel.Volume = value;
            }

            /// <summary>
            ///     WaveFormat of this stream
            /// </summary>
            public override WaveFormat WaveFormat => this.sampleChannel.WaveFormat;

            /// <summary>
            ///     Reads from this wave stream
            /// </summary>
            /// <param name="buffer">Audio buffer</param>
            /// <param name="offset">Offset into buffer</param>
            /// <param name="count">Number of bytes required</param>
            /// <returns>Number of bytes read</returns>
            public override int Read(byte[] buffer, int offset, int count) {
                var waveBuffer = new WaveBuffer(buffer);
                var samplesRequired = count / 4;
                var samplesRead = this.Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
                return samplesRead * 4;
            }

            /// <summary>
            ///     Reads audio from this sample provider
            /// </summary>
            /// <param name="buffer">Sample buffer</param>
            /// <param name="offset">Offset into sample buffer</param>
            /// <param name="count">Number of samples required</param>
            /// <returns>Number of samples read</returns>
            public int Read(float[] buffer, int offset, int count) {
                lock (this.lockObject) {
                    return this.sampleChannel.Read(buffer, offset, count);
                }
            }

            /// <summary>
            ///     Disposes this AudioFileReader
            /// </summary>
            /// <param name="disposing">True if called from Dispose</param>
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    if (this.readerStream != null) {
                        this.readerStream.Dispose();
                        this.readerStream = null;
                    }
                }

                base.Dispose(disposing);
            }

            /// <summary>
            ///     Creates the reader stream, supporting all filetypes in the core NAudio library,
            ///     and ensuring we are in PCM format
            /// </summary>
            /// <param name="fileName">File Name</param>
            private void CreateReaderStream(string fileName) {
                if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) {
                    this.readerStream = new WaveFileReader(fileName);
                    if (this.readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && this.readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat) {
                        this.readerStream = WaveFormatConversionStream.CreatePcmStream(this.readerStream);
                        this.readerStream = new BlockAlignReductionStream(this.readerStream);
                    }
                } else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) {
                    this.readerStream = new Mp3FileReader(fileName);
                } else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase)) {
                    this.readerStream = new AiffFileReader(fileName);
                }
            }

            /// <summary>
            ///     Helper to convert dest to source bytes
            /// </summary>
            private long DestToSource(long destBytes) {
                return this.sourceBytesPerSample * (destBytes / this.destBytesPerSample);
            }

            /// <summary>
            ///     Helper to convert source to dest bytes
            /// </summary>
            private long SourceToDest(long sourceBytes) {
                return this.destBytesPerSample * (sourceBytes / this.sourceBytesPerSample);
            }
        }

        /// <summary>
        ///     Class for reading from MP3 files
        /// </summary>
        public class Mp3FileReader : Mp3FileReaderBase {
            /// <summary>Supports opening a MP3 file</summary>
            public Mp3FileReader(string mp3FileName)
                : base(File.OpenRead(mp3FileName), CreateAcmFrameDecompressor, true) { }

            /// <summary>
            ///     Opens MP3 from a stream rather than a file
            ///     Will not dispose of this stream itself
            /// </summary>
            /// <param name="inputStream">The incoming stream containing MP3 data</param>
            public Mp3FileReader(Stream inputStream)
                : base(inputStream, CreateAcmFrameDecompressor, false) { }

            /// <summary>
            ///     Creates an ACM MP3 Frame decompressor. This is the default with NAudio
            /// </summary>
            /// <param name="mp3Format">A WaveFormat object based </param>
            /// <returns></returns>
            public static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format) {
                // new DmoMp3FrameDecompressor(this.Mp3WaveFormat); 
                return new AcmMp3FrameDecompressor(mp3Format);
            }
        }
    }
}