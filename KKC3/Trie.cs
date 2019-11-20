﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public class Trie<TKey, TValue> {
        public class Constructor {
            internal class Node {
                // The node of the tree.
                // Each node has one character as its member.
                public TKey key;
                public TValue value;

                public List<Node> children;
                public bool accept;

                public Node(TKey key, TValue value) {
                    this.key = key;
                    this.value = value;
                    this.children = new List<Node>();
                    this.accept = false;
                }
                public override string ToString() {
                    return this.key.ToString();
                }

                public void add_child(Node child) {
                    this.children.Add(child);
                }
            }

            private Node tree;

            //This class has:
            //    a function which constructs a tree by words
            //    a function which dumps the tree as a LOUDS bit-string

            public Constructor() {
                this.tree = new Node(default(TKey), default(TValue));  //The root node
            }

            //Add a word to the tree
            public Constructor add(IEnumerable<TKey> keys, TValue value) {
                Node prev = null;
                Node node = this.tree;
                foreach (var key in keys) {
                    var child = node.children.Find(x => x.key.Equals(key));
                    if (child == null) {
                        child = new Node(key, default(TValue));
                        node.add_child(child);
                    }
                    prev = node;
                    node = child;
                }
                node.accept = true;
                node.value = value;
                return this;
            }

            public void show() {
                this.show_(this.tree);
            }

            private void show_(Node node, int depth = 0) {
                Console.WriteLine($"{String.Concat(Enumerable.Repeat(' ', depth))}{node}");
                foreach (var child in node.children) {
                    this.show_(child, depth + 1);
                }
            }

            // Dump a LOUDS bit-string
            public Trie<TKey, TValue> Create() {
                // from collections import deque

                var bit_array = new BitVector() { 1, 0 };  // [1, 0] indicates the 0th node
                var labels = new List<TKey>() { default(TKey) };
                var values = new Dictionary<int, TValue>();

                // dumps by Breadth-first search
                var queue = new Queue<Node>();
                queue.Enqueue(this.tree);

                var acceptBitVector = new BitVector();

                int index = 1;
                while (queue.Count != 0) {
                    var node = queue.Dequeue();
                    labels.Add(node.key);

                    if (node.accept) {
                        acceptBitVector[index] = 1;
                        values[index] = node.value;
                    }
                    foreach (var child in node.children) {
                        queue.Enqueue(child);
                        bit_array.Add(1);
                    }
                    bit_array.Add(0);
                    index++;
                }
                return new Trie<TKey, TValue>(bit_array, labels, acceptBitVector, values);
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

        public int? trace_children(int current_node, TKey character) {
            var index = this.bitVector.select(current_node, 0) + 1;
            while (index.HasValue && this.bitVector[index.Value] == 1) {
                var node = this.bitVector.rank(index.Value, 1);
                if (this.labels[node].Equals(character)) {
                    return node;
                }
                index += 1;
            }
            return null;
        }

        public int? search(IEnumerable<TKey> query) {
            int? node = 1;
            foreach (var c in query) {
                node = this.trace_children(node.Value, c);
                if (node.HasValue == false) {
                    // the query is not in the tree
                    return null;
                }
            }
            return node;
        }

        public int? parent(int? node) {
            if (node.HasValue == false) {
                return null;
            }
            var idx = this.bitVector.select(node.Value, 1);
            return this.bitVector.rank(idx.Value - 1, 0);
        }

        public bool isAccept(int? node) {
            if (node.HasValue == false) {
                return false;
            }
            return this.acceptBitVector[node.Value] != 0;
        }

        public IEnumerable<TValue> CommonPrefixSearch(IEnumerable<TKey> query) {
            int? node = 1;
            foreach (var c in query) {
                node = this.trace_children(node.Value, c);
                if (node.HasValue == false) {
                    yield break;
                }
                if (isAccept(node)) {
                    yield return values[node.Value];
                }
            }
        }

    }

}
