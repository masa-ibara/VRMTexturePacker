using UnityEngine;
using UnityEngine.Rendering;

namespace MsaI.Runtime.TexturePacker
{
    public class TargetData
    {
        internal SkinnedMeshRenderer skinnedMeshRenderer;
        internal int subMeshIndex;

        internal TargetData(SkinnedMeshRenderer skinnedMeshRenderer, int subMeshIndex)
        {
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            this.subMeshIndex = subMeshIndex;
        }
    }
}
