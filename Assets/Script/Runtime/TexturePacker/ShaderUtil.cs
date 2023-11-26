using UnityEngine;
using UnityEngine.Rendering;
namespace MsaI.TexturePacker
{
    public class ShaderUtil
    {
        internal enum RenderMode
        {
            Opaque,
            Cutout,
            Transparent
        }
        
        internal static RenderMode GetRenderMode(Material material)
        {
            if (material.HasProperty("_ZWrite"))
            {
                if (material.GetInt("_ZWrite") == 1)
                {
                    if (material.renderQueue >= (int)RenderQueue.AlphaTest)
                    {
                        return RenderMode.Cutout;
                    }
                }
                if (material.GetInt("_ZWrite") == 0)
                {
                    return RenderMode.Transparent;
                }
            }
            return RenderMode.Opaque;
        }

        internal static Material SetBlendMode(Material material, RenderMode renderMode)
        {
            switch (renderMode)
            {
                case RenderMode.Opaque:
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetInt("_SrcBlend", (int)BlendMode.One);
                    material.SetInt("_DstBlend", (int)BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON"); 
                    material.renderQueue = -1;
                    break;
                case RenderMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)BlendMode.One);
                    material.SetInt("_DstBlend", (int)BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    break;
                case RenderMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
            return material;
        }
    }
}
