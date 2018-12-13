using System;
using System.Drawing;
using System.Linq;
using CNN.Extensions;

namespace CNN {
    public static class MNist {
        public class MNistImage {
            public int Index { get; }
            public int Width { get; }
            public int Height { get; }

            public byte[] Pixels { get; }
            public MNistImage(int index, int width, int height, byte[] pixels) {
                Index = index;
                Width = width;
                Height = height;
                Pixels = pixels;
            }
            public MNistImage Resize(int width, int height) {
                if (Width == width && Height == height) {
                    return new MNistImage(Index, Width, Height, Pixels);
                }
                byte[] newPixels = new byte[width * height];
                for (var y = 0; y < height; y++) {
                    for (var x = 0; x < width; x++) {
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
                return new MNistImage(Index, width, height, newPixels);
            }

            public double[] ToNormalizedVector() {
                return Pixels.Select(y => y / 255.0 * 0.99 + 0.01).ToArray();
            }

            public Bitmap ToBitmap() {
                var bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                for (var y = 0; y < Height; y++) {
                    for (var x = 0; x < Width; x++) {
                        bmp.SetPixel(x, y, Color.FromArgb(Pixels[y * Width + x], Pixels[y * Width + x], Pixels[y * Width + x]));
                    }
                }
                return bmp;
            }
        }
        public static MNistImage[] LoadImages(string file) {

            using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open))
            using (var br = new System.IO.BinaryReader(fs)) {
                var magic = br.ReadBytes(4);
                if (!magic.SequenceEqual(new byte[] { 0x00, 0x00, 0x08, 0x03 })) {
                    throw new Exception();
                }
                var imageCount = br.ReadBytes(4).Reverse().ToArray().Apply(x => BitConverter.ToInt32(x, 0));
                var imageWidth = br.ReadBytes(4).Reverse().ToArray().Apply(x => BitConverter.ToInt32(x, 0));
                var imageHeight = br.ReadBytes(4).Reverse().ToArray().Apply(x => BitConverter.ToInt32(x, 0));
                return imageCount.Times().Select(x => new MNistImage(x, imageWidth, imageHeight, br.ReadBytes(imageWidth * imageHeight))).ToArray();
            }
        }
        public class MNistLabel {
            public int Index { get; }
            public byte Value { get; }
            public MNistLabel(int index, byte value) {
                Index = index;
                Value = value;
            }

        }
        public static MNistLabel[] LoadLabels(string file) {

            using (var fs = new System.IO.FileStream(file, System.IO.FileMode.Open))
            using (var br = new System.IO.BinaryReader(fs)) {
                var magic = br.ReadBytes(4);
                if (!magic.SequenceEqual(new byte[] { 0x00, 0x00, 0x08, 0x01 })) {
                    throw new Exception();
                }
                var imageCount = br.ReadBytes(4).Reverse().ToArray().Apply(x => BitConverter.ToInt32(x, 0));
                return imageCount.Times().Select(x => new MNistLabel(x, br.ReadByte())).ToArray();
            }
        }
    }
}