﻿using System;
using System.Collections.Generic;
 using System.Linq;

 namespace KKC3 {
    public class Trie<TKey, TValue> {
        public class Constructor {
            internal class Node {
                // The node of the tree.
                // Each node has one character as its member.
                public TKey Key { get; }
                public TValue Value { get; set; }

                public List<Node> Children{ get; }
                public bool Accept{ get; set; }

                public Node(TKey key, TValue value) {
                    Key = key;
                    Value = value;
                    Children = new List<Node>();
                    Accept = false;
                }
                public override string ToString() {
                    return Key.ToString();
                }

                public void add_child(Node child) {
                    Children.Add(child);
                }
            }

            private Node Root { get; }

            public Constructor() {
                Root = new Node(default(TKey), default(TValue));
            }

            public Constructor Add(IEnumerable<TKey> keys, TValue value) {
                Node node = Root;
                foreach (var key in keys) {
                    var child = node.Children.Find(x => x.Key.Equals(key));
                    if (child == null) {
                        child = new Node(key, default(TValue));
                        node.add_child(child);
                    }
                    node = child;
                }
                node.Accept = true;
                node.Value = value;
                return this;
            }

            public void Show() {
                show_(Root);
            }

            private void show_(Node node, int depth = 0) {
                Console.WriteLine($"{String.Concat(Enumerable.Repeat(' ', depth))}{node}");
                foreach (var child in node.Children) {
                    show_(child, depth + 1);
                }
            }

            public Trie<TKey, TValue> Create() {
                // from collections import deque

                var bitArray = new BitVector() { 1, 0 };  // [1, 0] indicates the 0th node
                var labels = new List<TKey>() { default(TKey) };
                var values = new Dictionary<int, TValue>();

                // dumps by Breadth-first search
                var queue = new Queue<Node>();
                queue.Enqueue(Root);

                var acceptBitVector = new BitVector();

                int index = 1;
                while (queue.Count != 0) {
                    var node = queue.Dequeue();
                    labels.Add(node.Key);

                    if (node.Accept) {
                        acceptBitVector[index] = 1;
                        values[index] = node.Value;
                    }
                    foreach (var child in node.Children) {
                        queue.Enqueue(child);
                        bitArray.Add(1);
                    }
                    bitArray.Add(0);
                    index++;
                }
                return new Trie<TKey, TValue>(bitArray, labels, acceptBitVector, values);
            }
        }


        public BitVector bitVector { get; }
        public List<TKey> labels { get; }
        public BitVector acceptBitVector { get; }
        public List<TValue> values { get; }


        internal Trie(BitVector bitVector, List<TKey> labels, BitVector acceptBitVector, Dictionary<int, TValue> values) {
            this.bitVector = bitVector;
            this.labels = labels;
            this.acceptBitVector = acceptBitVector;
            this.values = Enumerable.Repeat(default(TValue), acceptBitVector.Length).ToList();
            foreach (var kv in values) {
                this.values[kv.Key] = kv.Value;
            }
        }

        private int? TraceChildren(int currentNode, TKey character) {
            var index = bitVector.Select(currentNode, 0) + 1;
            while (index.HasValue && bitVector[index.Value] == 1) {
                var node = bitVector.Rank(index.Value, 1);
                if (labels[node].Equals(character)) {
                    return node;
                }
                index += 1;
            }
            return null;
        }

        public int? Search(IEnumerable<TKey> query) {
            int? node = 1;
            foreach (var c in query) {
                node = TraceChildren(node.Value, c);
                if (node.HasValue == false) {
                    // the query is not in the tree
                    return null;
                }
            }
            return node;
        }

        public int? Parent(int? node) {
            if (node.HasValue == false) {
                return null;
            }
            var idx = bitVector.Select(node.Value, 1);
            if (idx == null) {
                return null;
            }
            return bitVector.Rank(idx.Value - 1, 0);
        }

        public bool IsAccept(int? node) {
            if (node.HasValue == false) {
                return false;
            }
            return acceptBitVector[node.Value] != 0;
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
                    yield return Tuple.Create((IEnumerable<TKey>)keys, values[node.Value]);
                }
            }
        }

        public void ToStaticTrie(System.IO.Stream s, Func<TKey, byte[]> keySerializer, Func<TValue, byte[]> valueSerializer) {
            // header

            // FourCC
            s.Write("TRIE".ToCharArray().Select(x => (byte)x).ToArray());

            s.Write("HEAD".ToCharArray().Select(x => (byte)x).ToArray());
            // sizeof header
            s.Write(6 * 4);
            // BitVector: int32*int32
            s.Write(bitVector.ByteLength);
            s.Write(bitVector.Length);
            // acceptBitVector: int32*int32
            s.Write(acceptBitVector.ByteLength);
            s.Write(acceptBitVector.Length);
            // labels: int32
            s.Write(labels.Count);
            // values: int32
            s.Write(values.Count);

            // BitVectorData:
            s.Write("BITV".ToCharArray().Select(x => (byte)x).ToArray());
            s.Write(bitVector.Bytes.ToArray());

            // acceptBitVector:
            s.Write("ABTV".ToCharArray().Select(x => (byte)x).ToArray());
            s.Write(acceptBitVector.Bytes.ToArray());

            s.Write("LBLS".ToCharArray().Select(x => (byte)x).ToArray());
            // labels:(chunkoffset*length)*data[]
            int size = 0;
            foreach (var label in labels) {
                var len = keySerializer(label).Length;
                s.Write(size);
                s.Write(len);
                size += len;
            }
            s.Write(size);
            foreach (var label in labels) {
                s.Write(keySerializer(label));
            }

            s.Write("VALS".ToCharArray().Select(x => (byte)x).ToArray());
            // values:(chunkoffset*length)*data[]
            size = 0;
            foreach (var value in values) {
                var len = value == null ? 0 : valueSerializer(value).Length;
                s.Write(size);
                s.Write(len);
                size += len;
            }
            s.Write(size);
            foreach (var value in values) {
                if (value != null) {
                    s.Write(valueSerializer(value));
                }
            }

            s.Write("EIRT".ToCharArray().Select(x => (byte)x).ToArray());


        }

    }
}
