using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class KKCFeatureFunc {
        public static FeatureFuncs Create() {
            var featureFuncs = new FeatureFuncs();

            featureFuncs.NodeFeatures.Add((nodes, index) => "S0" + nodes[index].Word);
            featureFuncs.NodeFeatures.Add((nodes, index) => "P" + nodes[index].GetFeature(0));
            featureFuncs.NodeFeatures.Add((nodes, index) => "S0" + nodes[index].Word + "\tR0" + nodes[index].Read);
            featureFuncs.NodeFeatures.Add((nodes, index) => "S" + nodes[index].Word + "\tP" + nodes[index].GetFeature(0));
            featureFuncs.NodeFeatures.Add((nodes, index) => "S1" + ((index > 0) ? nodes[index - 1].Word : "") + "\tS0" + nodes[index].Word + "\t+R1" + ((index + 1 < nodes.Count) ? nodes[index + 1].Read : ""));
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "ES" + prevNode.Word + "\tED" + node.Word);
            featureFuncs.EdgeFeatures.Add((prevNode, node) => "PS" + prevNode.GetFeature(0) + "\tPD" + node.GetFeature(0));

            return featureFuncs;
        }
    }
}
