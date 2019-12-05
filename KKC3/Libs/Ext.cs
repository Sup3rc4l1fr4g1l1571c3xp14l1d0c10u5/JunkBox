﻿using System;
using System.IO;
using System.Collections.Generic;

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


        public static IEnumerable<T[]> EachCons<T>(this IEnumerable<T> self, int n) {
            Queue<T> queue = new Queue<T>();
            foreach (var item in self) {
                queue.Enqueue(item);
                if (queue.Count > n) {
                    queue.Dequeue();
                }
                if (queue.Count == n) {
                    yield return queue.ToArray();
                }
            }
        }
    }
}
