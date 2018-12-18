using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Drawing;

namespace LibMNIST
{
    public static class MNIST
    {
        public class Image
        {
            public int Index { get; }
            public int Width { get; }
            public int Height { get; }

            public byte[] Pixels { get; }
            public Image(int index, int width, int height, byte[] pixels)
            {
                Index = index;
                Width = width;
                Height = height;
                Pixels = pixels;
            }
            public Image Resize(int width, int height)
            {
                if (Width == width && Height == height)
                {
                    return new Image(Index, Width, Height, Pixels);
                }
                byte[] newPixels = new byte[width * height];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var sx = ((double)x) * Width / width;
                        var sy = ((double)y) * Height / height;
                        var x1 = Math.Max(0, (int)Math.Truncate(sx));
                        var y1 = Math.Max(0, (int)Math.Truncate(sy));
                        var x2 = Math.Min(Width - 1, (int)Math.Truncate(sx) + 1);
                        var y2 = Math.Min(Height - 1, (int)Math.Truncate(sy) + 1);
                        var rateX = sx - x1;
                        var rateY = sy - y1;
                        var step1X = Pixels[y1 * Width + x1] * (1 - rateX) + Pixels[y1 * Width + x2] * rateX;
                        var step1Y = Pixels[y2 * Width + x1] * (1 - rateX) + Pixels[y2 * Width + x2] * rateX;
                        var ret = step1X * (1 - rateY) + step1Y * (rateY);
                        newPixels[y * width + x] = (byte)Math.Min(255, Math.Max(0, ret));
                    }
                }
                return new Image(Index, width, height, newPixels);
            }

            public double[] ToNormalizedVector()
            {
                return Pixels.Select(y => y / 255.0 * 0.99 + 0.01).ToArray();
            }

            public Bitmap ToBitmap()
            {
                var bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        bmp.SetPixel(x, y, Color.FromArgb(Pixels[y * Width + x], Pixels[y * Width + x], Pixels[y * Width + x]));
                    }
                }
                return bmp;
            }
        }

        public class ImageLoader : IDisposable, IEnumerable<Image>
        {
            private FileStream _fileStream;
            private BinaryReader _binaryReader;
            public int ImageCount { get; }
            public int ImageWidth { get; }
            public int ImageHeight { get; }

            public ImageLoader(string file)
            {

                _fileStream = new FileStream(file, FileMode.Open);
                _binaryReader = new BinaryReader(_fileStream);
                var magic = _binaryReader.ReadBytes(4);
                if (!magic.SequenceEqual(new byte[] { 0x00, 0x00, 0x08, 0x03 }))
                {
                    throw new Exception();
                }
                ImageCount = BitConverter.ToInt32(_binaryReader.ReadBytes(4).Reverse().ToArray(),0);
                ImageWidth = BitConverter.ToInt32(_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                ImageHeight = BitConverter.ToInt32(_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
            }

            #region IDisposable Support
            private bool _disposedValue;

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _fileStream.Dispose();
                        _fileStream = null;
                        _binaryReader.Dispose();
                        _binaryReader = null;
                    }
                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            public IEnumerator<Image> GetEnumerator()
            {
                for (var x = 0; x < ImageCount; x++) {
                    yield return new Image(x, ImageWidth, ImageHeight, _binaryReader.ReadBytes(ImageWidth * ImageHeight));
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            #endregion
        }

        public class Label
        {
            public int Index { get; }
            public byte Value { get; }
            public Label(int index, byte value)
            {
                Index = index;
                Value = value;
            }

        }
        public class LabelLoader : IDisposable, IEnumerable<Label>
        {
            private FileStream _fileStream;
            private BinaryReader _binaryReader;
            public int LabelCount { get; }

            public LabelLoader(string file)
            {

                _fileStream = new FileStream(file, FileMode.Open);
                _binaryReader = new BinaryReader(_fileStream);
                var magic = _binaryReader.ReadBytes(4);
                if (!magic.SequenceEqual(new byte[] { 0x00, 0x00, 0x08, 0x01 }))
                {
                    throw new Exception();
                }
                LabelCount = BitConverter.ToInt32(_binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
            }

            #region IDisposable Support
            private bool _disposedValue;

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _fileStream.Dispose();
                        _fileStream = null;
                        _binaryReader.Dispose();
                        _binaryReader = null;
                    }
                    _disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            public IEnumerator<Label> GetEnumerator()
            {
                for (var x = 0; x < LabelCount; x++)
                {
                    yield return new Label(x, _binaryReader.ReadByte());
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            #endregion
        }
    }
}