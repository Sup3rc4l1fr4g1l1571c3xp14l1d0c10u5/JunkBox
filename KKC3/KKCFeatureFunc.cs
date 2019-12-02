using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class KKCFeatureFunc {
        public static FeatureFuncs Create() {
            var featureFuncs = new FeatureFuncs();

            featureFuncs.NodeFeatures.Add((nodes, index) => new NodeFeature(nodes[index].Word, "", ""));
            featureFuncs.NodeFeatures.Add((nodes, index) => new NodeFeature(nodes[index].GetFeature(0), "", ""));
            featureFuncs.NodeFeatures.Add((nodes, index) => new NodeFeature(nodes[index].Word, nodes[index].Read, ""));
            featureFuncs.NodeFeatures.Add((nodes, index) => new NodeFeature(nodes[index].Word, nodes[index].GetFeature(0), ""));
            featureFuncs.NodeFeatures.Add((nodes, index) => new NodeFeature((index > 0) ? nodes[index - 1].Word : "", nodes[index].Word, (index + 1 < nodes.Count) ? nodes[index + 1].Read : ""));
            featureFuncs.EdgeFeatures.Add((prevNode, node) => new EdgeFeature(prevNode.Word, node.Word));
            featureFuncs.EdgeFeatures.Add((prevNode, node) => new EdgeFeature(prevNode.GetFeature(0), node.GetFeature(0)));

            return featureFuncs;
        }
    }
}
