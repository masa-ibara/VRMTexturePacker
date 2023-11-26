using UnityEngine;
using UnityEngine.Rendering;

namespace MsaI.Runtime.TexturePacker
{
    public class TargetData
    {
        internal SkinnedMeshRenderer skinnedMeshRenderer;
        internal SubMeshDescriptor subMeshDescriptor;
        internal int materialIndex;

        internal TargetData(SkinnedMeshRenderer skinnedMeshRenderer, SubMeshDescriptor subMeshDescriptor, int materialIndex)
        {
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            this.subMeshDescriptor = subMeshDescriptor;
            this.materialIndex = materialIndex;
        }
    }
}
