﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KKC3 {
    /// <summary>
    /// ファイルに格納されたTrie木。読み取り専用だが非常に省メモリな実装。
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class StaticTrie<TKey, TValue> {
        private readonly Stream _stream;
        
        private readonly int _acceptBitVectorByteLength;
        private readonly int _acceptBitVectorHead;
        private readonly int _acceptBitVectorLength;

        private readonly int _bitVectorByteLength;
        private readonly int _bitVectorHead;
        private readonly int _bitVectorLength;

        private readonly int _labelsCount;
        private readonly int _labelsIndexTableHead;
        private readonly int _labelsValueTableHead;
        private readonly int _labelsValueTableSize;

        private readonly int _valuesCount;
        private readonly int _valuesIndexTableHead;
        private readonly int _valuesValueTableHead;
        private readonly int _valuesValueTableSize;

        private readonly Func<byte[], TKey> _keyDeserializer;
        private readonly Func<byte[], TValue> _valueDeserializer;

        public StaticTrie(Stream stream, int bitVectorByteLength, int bitVectorLength, int bitVectorHead, int acceptBitVectorByteLength, int acceptBitVectorLength, int acceptBitVectorHead, int labelsCount, int labelsIndexTableHead, int labelsValueTableSize, int labelsValueTableHead, int valuesCount, int valuesIndexTableHead, int valuesValueTableSize, int valuesValueTableHead, Func<byte[], TKey> keyDeserializer, Func<byte[], TValue> valueDeserializer) {
            _stream = stream;
            _bitVectorByteLength = bitVectorByteLength;
            _bitVectorLength = bitVectorLength;
            _bitVectorHead = bitVectorHead;
            _acceptBitVectorByteLength = acceptBitVectorByteLength;
            _acceptBitVectorLength = acceptBitVectorLength;
            _acceptBitVectorHead = acceptBitVectorHead;
            _labelsCount = labelsCount;
            _labelsIndexTableHead = labelsIndexTableHead;
            _labelsValueTableSize = labelsValueTableSize;
            _labelsValueTableHead = labelsValueTableHead;
            _valuesCount = valuesCount;
            _valuesIndexTableHead = valuesIndexTableHead;
            _valuesValueTableSize = valuesValueTableSize;
            _valuesValueTableHead = valuesValueTableHead;
            _keyDeserializer = keyDeserializer;
            _valueDeserializer = valueDeserializer;
        }

        private static byte GetBit(Stream s, int position, int startPos, int byteLength, int bitLength) {
            if (position < 0 || bitLength <= position) { throw new IndexOutOfRangeException(); }
            var index = position / 8;
            var bitPos = position % 8;
            s.Seek(startPos + index, SeekOrigin.Begin);
            return (byte)((s.ReadByte() >> bitPos) & 0x01U);
        }

        private static int? Select0(Stream s, int n, int startPos, int byteLength, int bitLength) {
            if (n <= 0) {
                return null;
            }

            s.Seek(startPos, SeekOrigin.Begin);
            for (var i = 0; i < byteLength; i++) {
                var v = s.ReadByte();
                if (n > 8 - BitCounter.Count((byte)v)) {
                    n -= (8 - BitCounter.Count((byte)v));
                } else {
                    var ret = i * 8;
                    for (;;) {
                        if ((v & 0x01) == 0) {
                            n--;
                            if (n == 0) {
                                return ret;
                            }
                        }
                        ret++;
                        v >>= 1;
                    }
                }
            }
            return null;
        }

        private static int? Rank1(Stream s, int position, int startPos, int byteLength, int bitLength) {
            if (position < 0) {
                return 0;
            }
            position += 1;
            var index = position / 8;
            var bitPos = position % 8;
            var n = 0;

            s.Seek(startPos, SeekOrigin.Begin);
            for (var i = 0; i < index; i++) {
                n += BitCounter.Count((byte)s.ReadByte());
            }
            if (bitPos > 0) {
                var v = s.ReadByte();
                while (bitPos > 0) {
                    if ((v & 0x01) != 0) {
                        n++;
                    }
                    bitPos--;
                    v >>= 1;
                }
            }
            return n;
        }

        private static T GetValue<T>(Stream stream, int node, int indexTableHead, int valueTableHead,Func<byte[], T> deserializer) {
            stream.Seek(indexTableHead + node * (2 * 4), SeekOrigin.Begin);
            var valueStart = stream.ReadInt();
            var valueLength = stream.ReadInt();

            stream.Seek(valueTableHead + valueStart, SeekOrigin.Begin);
            var valueValue = deserializer(stream.Read(valueLength));

            return valueValue;
        }



        public int? TraceChildren(int currentNode, TKey character) {
            var index = Select0(_stream, currentNode, _bitVectorHead, _bitVectorByteLength, _bitVectorLength) + 1;
            while (index.HasValue && GetBit(_stream, index.Value, _bitVectorHead, _bitVectorByteLength, _bitVectorLength) == 1) {
                var node = Rank1(_stream, index.Value, _bitVectorHead, _bitVectorByteLength, _bitVectorLength);
                if (node == null) { break; }
                var labelValue = GetValue(_stream, node.Value, _labelsIndexTableHead, _labelsValueTableHead, _keyDeserializer);
                if (labelValue.Equals(character)) {
                    return node;
                }
                index += 1;
            }
            return null;
        }

        public int? SearchNode(IEnumerable<TKey> query) {
            int? node = 1;
            foreach (var c in query) {
                node = TraceChildren(node.Value, c);
                if (node.HasValue == false) {
                    return null;
                }
            }
            return node;
        }

        public TValue Search(IEnumerable<TKey> query) {
            int? node = 1;
            foreach (var c in query) {
                node = TraceChildren(node.Value, c);
                if (node.HasValue == false) {
                    return default(TValue);
                }
            }
            if (IsAccept(node)==false) {
                return default(TValue);
            }
            var valueValue = GetValue(_stream, node.Value, _valuesIndexTableHead, _valuesValueTableHead, _valueDeserializer);
            return valueValue;
        }

        private bool IsAccept(int? node) {
            if (node.HasValue == false) {
                return false;
            }
            return GetBit(_stream, node.Value, _acceptBitVectorHead, _acceptBitVectorByteLength, _acceptBitVectorLength) != 0;
        }

        public IEnumerable<Tuple<IEnumerable<TKey>, TValue>> CommonPrefixSearch(IEnumerable<TKey> query) {
            int? node = 1;
            var keys = new List<TKey>();
            foreach (var c in query) {
                node = TraceChildren(node.Value, c);
                if (node.HasValue == false) {
                    yield break;
                }
                keys.Add(c);
                if (IsAccept(node)) {
                    var valueValue = GetValue(_stream, node.Value, _valuesIndexTableHead, _valuesValueTableHead, _valueDeserializer);
                    yield return Tuple.Create((IEnumerable<TKey> )keys, valueValue);
                }
            }
        }

        public static StaticTrie<TKey, TValue> Load(Stream s, Func<byte[], TKey> keyDeserializer, Func<byte[], TValue> valueDeserializer) {
            // header

            // FourCC
            if (s.Read(4).Select(x => (char)x).SequenceEqual("TRIE".ToCharArray()) == false) {
                return null;
            }

            if (s.Read(4).Select(x => (char)x).SequenceEqual("HEAD".ToCharArray()) == false) {
                return null;
            }
            // sizeof header
            if (s.ReadInt() != 6 * 4) {
                return null;
            }
            // BitVector: int32*int32
            var bitVectorByteLength = s.ReadInt();
            var bitVectorLength = s.ReadInt();

            // acceptBitVector: int32*int32
            var acceptBitVectorByteLength = s.ReadInt();
            var acceptBitVectorLength = s.ReadInt();

            // labels: int32
            var labelsCount = s.ReadInt();
            // values: int32
            var valuesCount = s.ReadInt();

            // BitVectorData:
            if (s.Read(4).Select(x => (char)x).SequenceEqual("BITV".ToCharArray()) == false) {
                return null;
            }
            var bitVectorHead = s.Position;
            s.Seek(bitVectorByteLength, SeekOrigin.Current);

            // acceptBitVector:
            if (s.Read(4).Select(x => (char)x).SequenceEqual("ABTV".ToCharArray()) == false) {
                return null;
            }
            var acceptBitVectorHead = s.Position;
            s.Seek(acceptBitVectorByteLength, SeekOrigin.Current);

            // labels:(chunkoffset*length)*data[]
            if (s.Read(4).Select(x => (char)x).SequenceEqual("LBLS".ToCharArray()) == false) {
                return null;
            }
            var labelsIndexTableHead = s.Position;
            s.Seek(labelsCount * 4 * 2, SeekOrigin.Current);
            var labelsValueTableSize = s.ReadInt();
            var labelsValueTableHead = s.Position;
            s.Seek(labelsValueTableSize, SeekOrigin.Current);


            // values:(chunkoffset*length)*data[]
            if (s.Read(4).Select(x => (char)x).SequenceEqual("VALS".ToCharArray()) == false) {
                return null;
            }
            var valuesIndexTableHead = s.Position;
            s.Seek(valuesCount * 4 * 2, SeekOrigin.Current);
            var valuesValueTableSize = s.ReadInt();
            var valuesValueTableHead = s.Position;
            s.Seek(valuesValueTableSize, SeekOrigin.Current);

            if (s.Read(4).Select(x => (char)x).SequenceEqual("EIRT".ToCharArray()) == false) {
                return null;
            }

            return new StaticTrie<TKey, TValue>(
                stream: s,
                // bitvector
                bitVectorByteLength: bitVectorByteLength,
                bitVectorLength: bitVectorLength,
                bitVectorHead: (int)bitVectorHead,
                // acceptBitVector
                acceptBitVectorByteLength: acceptBitVectorByteLength,
                acceptBitVectorLength: acceptBitVectorLength,
                acceptBitVectorHead: (int)acceptBitVectorHead,
                // labels
                labelsCount: labelsCount,
                labelsIndexTableHead: (int)labelsIndexTableHead,
                labelsValueTableSize: labelsValueTableSize,
                labelsValueTableHead: (int)labelsValueTableHead,
                // values
                valuesCount: valuesCount,
                valuesIndexTableHead: (int)valuesIndexTableHead,
                valuesValueTableSize: valuesValueTableSize,
                valuesValueTableHead: (int)valuesValueTableHead,
                // deserializer
                keyDeserializer:keyDeserializer,
                valueDeserializer:valueDeserializer
            );
        }
    }
}
