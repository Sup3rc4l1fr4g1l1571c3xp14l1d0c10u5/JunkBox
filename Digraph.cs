using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SugiyamaCS {
    public class Digraph {
        private readonly List<List<int>> _matrix = new List<List<int>>();

        private IEnumerable<int> Nodes => _matrix.Count.Times();

        public void AddEdge(int from, int to) {
            _matrix.Get(from, () => new List<int>()).Add(to);
            _matrix.Get(to, () => new List<int>());
        }

        public void RemoveEdge(int from, int to) {
            var e = _matrix.Get(from);
            if (e != null) {
                e.Remove(to);
            }
        }

        public IEnumerable<Tuple<int, int>> Edges {
            get {
                for (int f = 0; f < _matrix.Count; f++) {
                    for (int t = 0; t < _matrix[f].Count; t++) {
                        yield return Tuple.Create(f, _matrix[f][t]);
                    }
                }
            }
        }

        public Digraph() {
            _matrix = new List<List<int>>();
            DummyNodes = new List<int>();
        }

        public Digraph inverse() {
            var g = new Digraph();
#if false
            for (var a = 0; a < _matrix.Count; a++)
            {
                g._matrix.Add(new List<int>());
            }
            for (var a = 0; a < _matrix.Count; a++)
            {
                var arc = _matrix[a];
                foreach (int t in arc)
                {
                    g._matrix.Get(t, () => new List<int>()).Add(a);
                }
            }
#else
            foreach (var e in Edges) {
                g.AddEdge(e.Item2, e.Item1);
            }
#endif
            return g;
        }


        private List<List<int>> layerize() {

            var g = this.inverse();

            var taken = new HashSet<int>();
            var layers = new List<List<int>>();

            //            while (g._matrix.Count.Times().Any(i => taken.Contains(i) == false)) {
            while (Nodes.Any(i => taken.Contains(i) == false)) {
                // 
                var sinks = new List<int>();    // 沈点（sink vertex）：出次数がゼロの頂点
                foreach (var j in Nodes) {
                    var arc = g._matrix[j];
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
        private List<int> initLayer(List<int> freeLayer, List<int> fixedLayer) {

            return freeLayer.Select(x => new {
                p = (_matrix[x] == null || _matrix[x].Count == 0)
                            ? 0.5
                            : _matrix[x].Aggregate(0.0, (sum, i) => sum + fixedLayer.IndexOf(i)) / _matrix[x].Count,
                x = x
            }
                )
                .OrderBy((a) => a.p)
                .Select(x => x.x)
                .ToList();
        }

        /**
         *  assuming u and v are node in the free layer
         *  return the number of arc related to u or v only that are crossing IF u is before v
         *
         */
        private int n_crossing(int u, int v, List<int> fixedLayer) {

            var p = 0;
            var n = 0;
            fixedLayer.ForEach(x => {
                if (_matrix[u].Any(y => x == y)) {
                    n += p;
                }
                if (_matrix[v].Any(y => x == y)) {
                    p += 1;
                }
            });

            return n;
        }

        private void orderLayer(List<int> freeLayer, List<int> fixedLayer) {

            // buble sort
            // swap position of adjacent node if it reduce the number of crossing
            for (var i = 1; i < freeLayer.Count; i++) {
                for (var j = 0; j < freeLayer.Count - i; j++) {

                    var a = freeLayer[j];
                    var b = freeLayer[j + 1];

                    if (n_crossing(a, b, fixedLayer) > n_crossing(b, a, fixedLayer)) {
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
        public List<List<int>> layerOrdering(List<List<int>> layers) {

            if (layers.Count <= 1) {
                return layers;
            }

            var g = this.inverse();

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

        void addDummy(List<List<int>> layers) {

            var layerById = new Dictionary<int, int>();
            for (var i = 0; i < layers.Count; i++) {
                var list = layers[i];
                foreach (var x in list) {
                    layerById[x] = i;
                }
            }
            this.DummyNodes.Clear();

            for (var a = 0; a < _matrix.Count; a++) {
                for (var i = _matrix[a].Count - 1; i >= 0; i--) {
                    var b = _matrix[a][i];
                    var child = b;

                    for (var k = layerById[b] - 1; k > layerById[a]; k--) {

                        var x = _matrix.Count;
                        _matrix.Add(new List<int> { child });

                        layerById[x] = k;
                        layers[k].Add(x);
                        DummyNodes.Add(k);

                        child = x;
                    }

                    _matrix[a][i] = child;
                }
            }
        }

        public List<int> DummyNodes { get; }

        private Digraph clone() {
            var g = new Digraph();
            for (var arc = 0; arc < _matrix.Count; arc++) {
                g._matrix.Set(arc, _matrix[arc].ToList());
            }
            g.DummyNodes.AddRange(DummyNodes);
            foreach (var v in this.invert) { g.invert.Add(v); }
            foreach (var v in this.othertree) { g.othertree.Add(v); }
            return g;
        }

        public class GraphPosition {
            public List<Point> position;
            public Digraph Digraph;
        }

        public HashSet<Tuple<int, int>> invert = new HashSet<Tuple<int, int>>();

        public HashSet<int> dfs_recur(int node, HashSet<int> visited, HashSet<int> visited2) {
            var adj = this._matrix[node];
            visited.Add(node);
            visited2.Add(node);
            //document.write(node.getVertex());
            for (var i = 0; i < adj.Count; i++) {
                var dest = adj[i];
                if (othertree.Contains(dest) == false) {
                    if (visited2.Contains(dest) == false) {
                        dfs_recur(dest, visited, visited2);
                    } else {
                        invert.Add(Tuple.Create(node, dest));
                    }
                }
            }
            visited2.Remove(node);
            return visited;
        }

        private HashSet<int> dfs(int start) {
            return this.dfs_recur(start, new HashSet<int>(), new HashSet<int>());
        }

        private HashSet<int> othertree = new HashSet<int>();

        private int returnNodeOutTrees() {
            for (var i = 0; i < this._matrix.Count; i++) {
                if (this.othertree.Contains(i) == false) {
                    return i;
                };
            }
            return -1;
        }

        public Digraph RemoveCicle() {
            var g = this.clone();
            var a = g.returnNodeOutTrees();
            while (a != -1) {
                var v = g.dfs(a);
                foreach (var x in v) {
                    g.othertree.Add(x);
                }
                a = g.returnNodeOutTrees();
            }
            return g;
        }

        // index
        public GraphPosition computePosition() {
            var g = this.RemoveCicle();
            g = g.clone();
            foreach (var invert in g.invert) {
                g.RemoveEdge(invert.Item1, invert.Item2);
            }
            foreach (var invert in g.invert) {
                g.AddEdge(invert.Item2, invert.Item1);
            }
            var layers = g.layerize();

            g.addDummy(layers);

            var orderedLayers = g.layerOrdering(layers);

            var l = 100;
            var position = new List<Point>();

            for (var ky = 0; ky < orderedLayers.Count; ky++) {
                var list = orderedLayers[ky];
                for (var kx = 0; kx < list.Count; kx++) {
                    var x = list[kx];
                    position.Set(x, new Point(
                        (int)((kx + 0.5) * l / list.Count),
                        (int)((ky + 0.5) * l / orderedLayers.Count)
                    ));
                }
            }

            return new GraphPosition { Digraph = g, position = position };
        }
    }
    public static class Ext {
        public static int IndexOf<T>(this T[] self, T t) {
            for (int i = 0; i < self.Length; i++) {
                if (self[i].Equals(t)) {
                    return i;
                }
            }
            return -1;
        }
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