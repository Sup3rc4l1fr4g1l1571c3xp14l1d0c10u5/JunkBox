using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SugiyamaCS {
    public class Digraph {
        private class NodeData {
            // そのうち何か入れる
        }

        private readonly Dictionary<int, NodeData> _nodeMap;
        private readonly HashSet<Tuple<int, int>> _edgeMap;
        private int _idGen;

        public Digraph() {
            _nodeMap = new Dictionary<int, NodeData>();
            _edgeMap = new HashSet<Tuple<int, int>>();
            _idGen = 0;
        }

        public IEnumerable<int> Nodes => _nodeMap.Keys;
        public IEnumerable<Tuple<int, int>> Edges => _edgeMap;

        public Digraph Clone() {
            var g = new Digraph();
            foreach (var kv in _nodeMap) {
                g._nodeMap.Add(kv.Key, kv.Value);
            }
            foreach (var edge in _edgeMap) {
                g._edgeMap.Add(edge);
            }
            g._idGen = _idGen;
            return g;
        }

        public Digraph Inverse() {
            var g = new Digraph();
            foreach (var kv in _nodeMap) {
                g._nodeMap.Add(kv.Key, kv.Value);
            }
            foreach (var e in this.Edges) {
                g.AddEdge(e.Item2, e.Item1);
            }
            return g;
        }

        public int AddNode() {
            var id = _idGen++;
            _nodeMap.Add(id, new NodeData());
            return id;
        }

        public void RemoveNode(int id) {
            if (_nodeMap.ContainsKey(id) == false) {
                throw new Exception($"node id:{id} not found");
            }
            _nodeMap.Remove(id);
        }

        public void AddEdge(Tuple<int, int> edge) {
            if (_nodeMap.ContainsKey(edge.Item1) == false) {
                throw new Exception($"node id:{edge.Item1} not found");
            } else if (_nodeMap.ContainsKey(edge.Item2) == false) {
                throw new Exception($"node id:{edge.Item2} not found");
            } else {
                _edgeMap.Add(edge);
            }
        }

        public void AddEdge(int from, int to) {
            AddEdge(Tuple.Create(from, to));
        }

        public void RemoveEdge(Tuple<int, int> edge) {
            if (_nodeMap.ContainsKey(edge.Item1) == false) {
                throw new Exception($"node id:{edge.Item1} not found");
            } else if (_nodeMap.ContainsKey(edge.Item2) == false) {
                throw new Exception($"node id:{edge.Item2} not found");
            } else {
                _edgeMap.Remove(edge);
            }
        }

        public void RemoveEdge(int from, int to) {
            RemoveEdge(Tuple.Create(from, to));
        }

        public IEnumerable<int> To(int from) {
            return _edgeMap.Where(x => x.Item1 == from).Select(x => x.Item2);
        }

        public IEnumerable<int> From(int to) {
            return _edgeMap.Where(x => x.Item2 == to).Select(x => x.Item1);
        }

    }

    public static class DigraphExt {


        private static List<List<int>> layerize(this Digraph self) {

            var g = self.Inverse();

            var taken = new HashSet<int>();
            var layers = new List<List<int>>();

            while (g.Nodes.Any(i => taken.Contains(i) == false)) {
                // 
                var sinks = new List<int>();    // 沈点（sink vertex）：出次数がゼロの頂点
                foreach (var j in g.Nodes) {
                    var arc = g.To(j);
                    if (taken.Contains(j) == false && arc.All(i => taken.Contains(i))) {
                        // ノード j は沈点ではないが、ノード j から出ている辺が全て沈点に繋がっている場合、
                        // ノード j も沈点とする
                        sinks.Add(j);
                    }
                }

                foreach (var i in sinks) {
                    taken.Add(i);
                }

                layers.Add(sinks);
            }

            return layers;
        }

        /**
         * assuming the fixed layer is fixed,
         *  get a naive ordering for the freeLayer
         *
         *  based on the barycenter,
         *    for each node X is the free layer, get the barycenter of all the node that are connected to X in the fixed layer
         *    sort node with this value
         */
        private static List<int> initLayer(this Digraph self, List<int> freeLayer, List<int> fixedLayer) {

            return freeLayer.Select(x => {
                var arc = self.To(x).ToList();
                return new {
                    p = (arc.Count == 0)
                        ? 0.5
                        : arc.Sum(fixedLayer.IndexOf) * 1.0 / arc.Count,
                    x = x
                };
            })
                .OrderBy((a) => a.p)
                .Select(x => x.x)
                .ToList();
        }

        /**
         *  assuming u and v are node in the free layer
         *  return the number of arc related to u or v only that are crossing IF u is before v
         *
         */
        private static int n_crossing(this Digraph self, int u, int v, List<int> fixedLayer) {

            var p = 0;
            var n = 0;
            fixedLayer.ForEach(x => {
                if (self.To(u).Any(y => x == y)) {
                    n += p;
                }
                if (self.To(v).Any(y => x == y)) {
                    p += 1;
                }
            });

            return n;
        }

        private static void orderLayer(this Digraph self, List<int> freeLayer, List<int> fixedLayer) {

            // buble sort
            // swap position of adjacent node if it reduce the number of crossing
            for (var i = 1; i < freeLayer.Count; i++) {
                for (var j = 0; j < freeLayer.Count - i; j++) {

                    var a = freeLayer[j];
                    var b = freeLayer[j + 1];

                    if (self.n_crossing(a, b, fixedLayer) > self.n_crossing(b, a, fixedLayer)) {
                        // swap
                        freeLayer[j] = b;
                        freeLayer[j + 1] = a;
                    }
                }
            }
        }

        /**
         * assuming the graph node have been grouped by layer,
         * where for a layer N, all the node connection are located in the N+1 layer
         *
         * order each layer in a way to minimise the crossing between connection
         *
         */
        public static List<List<int>> layerOrdering(this Digraph self, List<List<int>> layers) {

            if (layers.Count <= 1) {
                return layers;
            }

            var g = self.Inverse();

            // start from top to bottom, init the layers with naive sorting
            for (var i = 1; i < layers.Count; i++) {
                layers[i] = g.initLayer(layers[i], layers[i - 1]);
            }
            // from bottom to top, optimize the layer
            for (var i = layers.Count - 2; i >= 0; i--) {
                g.orderLayer(layers[i], layers[i + 1]);
            }

            // from top to bottom, optimize the layer
            for (var i = 1; i < layers.Count; i++) {
                g.orderLayer(layers[i], layers[i - 1]);
            }

            return layers;
        }

        private static void addDummy(this Digraph self, List<List<int>> layers, Dictionary<Tuple<int, int>, List<int>> edgeJoints, HashSet<Tuple<int, int>> invertEdges) {

            var layerById = new Dictionary<int, int>();
            for (var i = 0; i < layers.Count; i++) {
                var list = layers[i];
                foreach (var x in list) {
                    layerById[x] = i;
                }
            }

            var nodes = self.Nodes.ToList();
            foreach (var a in nodes) {
                var adv = self.To(a).ToList();
                for (var i = adv.Count - 1; i >= 0; i--) {
                    var b = adv[i];
                    var child = b;
                    var edge = Tuple.Create(a, b);
                    self.RemoveEdge(edge);
                    var joints = new List<int>();
                    joints.Add(b);
                    for (var k = layerById[b] - 1; k > layerById[a]; k--) {

                        var newdummy = self.AddNode();

                        self.AddEdge(child, newdummy);

                        layerById[newdummy] = k;
                        layers[k].Add(newdummy);
                        joints.Add(newdummy);
                        child = newdummy;
                    }
                    joints.Add(a);

                    var invEdge = Tuple.Create(b, a);
                    if (invertEdges.Contains(invEdge))
                    {
                        edgeJoints.Add(invEdge, joints);
                    }
                    else
                    {
                        edgeJoints.Add(edge, joints);
                    }

                    self.AddEdge(a, child);
                }
            }
        }

        public class GraphLayout {
            public Dictionary<int, Point> Positions;
            public Dictionary<Tuple<int, int>, List<int>> Joints;
        }


        public static GraphLayout ComputeLayout(this Digraph self) {
            var invertEdges = self.RemoveCicle();
            var g = self.Clone();
            foreach (var invert in invertEdges) {
                g.RemoveEdge(invert.Item1, invert.Item2);
            }
            foreach (var invert in invertEdges) {
                g.AddEdge(invert.Item2, invert.Item1);
            }
            var layers = g.layerize();

            var edgeJoints = new Dictionary<Tuple<int, int>, List<int>>();
             g.addDummy(layers, edgeJoints,invertEdges);

            var orderedLayers = g.layerOrdering(layers);

            var l = 300;
            var nodePosition = new Dictionary<int, Point>();

            for (var ky = 0; ky < orderedLayers.Count; ky++) {
                var list = orderedLayers[ky];
                for (var kx = 0; kx < list.Count; kx++) {
                    var x = list[kx];
                    nodePosition[x] = new Point(
                        (int)((kx + 0.5) * l / list.Count),  
                        (int)((ky + 0.5) * l / orderedLayers.Count)
                    );
                }
            }

            return new GraphLayout { Positions = nodePosition, Joints = edgeJoints };
        }
    }

    /// <summary>
    /// 循環除去
    /// </summary>
    public static class RemoveCycle {

        private static HashSet<int> DepthFirstSearch(this Digraph self, int node, HashSet<Tuple<int, int>> invert, HashSet<int> othertree, HashSet<int> visited, HashSet<int> path) {
            var adj = self.To(node).ToList();
            visited.Add(node);
            path.Add(node);
            foreach (var dest in adj) {
                if (othertree.Contains(dest) == false && invert.Contains(Tuple.Create(node, dest)) == false) {
                    if (path.Contains(dest) == false) {
                        self.DepthFirstSearch(dest, invert, othertree, visited, path);
                    } else {
                        invert.Add(Tuple.Create(node, dest));
                    }
                }
            }
            path.Remove(node);
            return visited;
        }


        public static HashSet<Tuple<int, int>> RemoveCicle(this Digraph self) {
            var invert = new HashSet<Tuple<int, int>>();
            var othertree = new HashSet<int>();
            for (;;) {
                var a = self.Nodes.Where(i => othertree.Contains(i) == false).DefaultIfEmpty(-1).First();
                if (a == -1)
                {
                    break;
                }
                var v = self.DepthFirstSearch(a, invert, othertree, new HashSet<int>(), new HashSet<int>());
                foreach (var x in v) {
                    othertree.Add(x);
                }
            }
            return invert;
        }
    }

    public static class Ext {
        public static IEnumerable<int> Times(this int self) {
            return Enumerable.Range(0, self);
        }
        public static T Get<T>(this List<T> self, int index) {
            if (self.Count <= index) {
                return default(T);
            }
            return self[index];
        }
        public static T Get<T>(this List<T> self, int index, Func<T> makeDef) {
            if (self.Count <= index) {
                self.AddRange(Enumerable.Range(0, index - self.Count + 1).Select((x) => makeDef()));
                self[index] = makeDef();
            }
            return self[index];
        }
        public static void Set<T>(this List<T> self, int index, T value) {
            if (self.Count <= index) {
                self.AddRange(Enumerable.Repeat(default(T), index - self.Count + 1));
            }
            self[index] = value;
        }
        public static void Set<T>(this List<T> self, int index, T value, Func<T> makeDef) {
            if (self.Count <= index) {
                self.AddRange(Enumerable.Range(0, index - self.Count + 1).Select((x) => makeDef()));
            }
            self[index] = value;
        }
    }

}