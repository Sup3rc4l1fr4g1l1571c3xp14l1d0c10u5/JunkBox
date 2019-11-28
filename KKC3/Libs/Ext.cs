﻿using System;
using System.IO;

 namespace KKC3 {
    public static class Ext {
        public static int Write(this Stream self, params byte[] data) {
            self.Write(data, 0, data.Length);
            return data.Length;
        }
        public static int Write(this Stream self, int data) {
            return self.Write(BitConverter.GetBytes(data));
        }
        public static byte[] Read(this Stream self, int num) {
            byte[] buf = new byte[num];
            self.Read(buf, 0, num);
            return buf;
        }
        public static int ReadInt(this Stream self) {
            return BitConverter.ToInt32(Read(self, 4),0);
        }

    }
}
