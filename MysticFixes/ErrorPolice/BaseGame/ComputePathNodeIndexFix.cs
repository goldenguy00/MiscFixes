using HarmonyLib;
using RoR2.Navigation;

namespace MiscFixes.ErrorPolice.BaseGame
{
    // A PathRequest can name its start/end by node index, which ComputePath uses without checking.
    // An index taken from a different graph (ground vs air, or a previous stage) overruns the
    // per-node arrays and throws. Treat an out-of-range index as invalid, which ComputePath already
    // handles by returning an unreachable path.
    [HarmonyPatch(typeof(NodeGraph), nameof(NodeGraph.ComputePath))]
    internal static class ComputePathNodeIndexFix
    {
        private static void Prefix(NodeGraph.PathRequest pathRequest, NodeGraph.Node[] ___nodes)
        {
            if (pathRequest == null || ___nodes == null)
                return;

            int nodeCount = ___nodes.Length;
            bool IsOutOfRange(NodeGraph.NodeIndex? nodeIndex)
            {
                int index = nodeIndex.HasValue ? nodeIndex.Value.nodeIndex : NodeGraph.NodeIndex.invalid.nodeIndex;
                return index != NodeGraph.NodeIndex.invalid.nodeIndex && (index < 0 || index >= nodeCount);
            }

            if (IsOutOfRange(pathRequest.startPos.nodeIndex))
                pathRequest.startPos = NodeGraph.NodeIndex.invalid;
            if (IsOutOfRange(pathRequest.endPos.nodeIndex))
                pathRequest.endPos = NodeGraph.NodeIndex.invalid;
        }
    }
}
