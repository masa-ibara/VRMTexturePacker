using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MsaI.Runtime.TexturePacker
{
    public class Packer
    {
        static Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]> targetsDict;
        static Dictionary<(Shader shader, ShaderUtil.RenderMode renderMode), TargetData[]> atlasTargetsDict;

        internal static void PackAssets(GameObject rootGameObject, bool highCompression = false)
        {
            //init
            targetsDict = new Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]>();
            atlasTargetsDict = new Dictionary<(Shader shader, ShaderUtil.RenderMode renderMode), TargetData[]>();
            
            CreateTargetDict(rootGameObject);
            foreach (var target in targetsDict)
            {
                var key = target.Key;
                var targetDatas = target.Value;
                BakeTextures(key.texture, key.mainColor, targetDatas);
            }
            foreach (var target in atlasTargetsDict)
            {
                var key = target.Key;
                var targetDatas = target.Value;
                ApplyPackings(key.shader, key.renderMode, targetDatas, highCompression);
            }
        }

        static void CreateTargetDict(GameObject rootGameObject)
        {
            var skinnedMeshRenderers = rootGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                AddToTargetDict(skinnedMeshRenderer);
            }
        }

        static void AddToTargetDict(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            // ToDo: CutOutかつColorのAlphaが0のMeshを削除し、そのMaterialを排除する
            skinnedMeshRenderer.enabled = true;
            var materials = skinnedMeshRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                var renderMode = ShaderUtil.GetRenderMode(mat);
                var newTargetData = new TargetData(skinnedMeshRenderer, i);
                //create dictionary for distinct texture, shader, color
                var key = (mat.mainTexture, mat.shader, mat.color);
                if (targetsDict.TryGetValue(key, out var targetDatas))
                {
                    Array.Resize(ref targetDatas, targetDatas.Length + 1);
                    targetDatas[^1] = newTargetData;
                    targetsDict[key] = targetDatas;
                }
                else
                {
                    targetsDict.Add(key, new []{newTargetData});
                }
                //create dictionary for atlas
                var atlasKey = (mat.shader, renderMode);
                if (atlasTargetsDict.TryGetValue(atlasKey, out var atlasTargetDatas))
                {
                    Array.Resize(ref atlasTargetDatas, atlasTargetDatas.Length + 1);
                    atlasTargetDatas[^1] = newTargetData;
                    atlasTargetsDict[atlasKey] = atlasTargetDatas;
                }
                else
                {
                    atlasTargetsDict.Add(atlasKey, new []{newTargetData});
                }
            }
        }

        static void BakeTextures(Texture texture, Color mainColor, TargetData[] targetDatas)
        {
            // Create texture with multiplied color
            var texture2D = texture as Texture2D;
            var editedTexture = CreateMultipliedColorTexture(texture2D, mainColor);
            for (int i = 0; i < targetDatas.Length; i++)
            {
                var skinnedMeshRenderer = targetDatas[i].skinnedMeshRenderer;
                var currentMaterials = skinnedMeshRenderer.sharedMaterials;
                currentMaterials[targetDatas[i].subMeshIndex].mainTexture = editedTexture;
                skinnedMeshRenderer.materials = currentMaterials;
            }
        }
        
        static void ApplyPackings(Shader shader, ShaderUtil.RenderMode renderMode, TargetData[] targetDatas, bool highCompression = false)
        {
            var materials = targetDatas.Select(x => x.skinnedMeshRenderer.sharedMaterials[x.subMeshIndex]).ToArray();
            var textures = materials.Select(x => x.mainTexture as Texture2D).Distinct().ToArray();
            var meshesDict = new Dictionary<Texture2D, TargetData[]>();
            foreach (var targetData in targetDatas)
            {
                var texture = targetData.skinnedMeshRenderer.sharedMaterials[targetData.subMeshIndex].mainTexture as Texture2D;
                if (texture == null)
                {
                    // Textureが無い場合でも単色で埋めているはずなのでここには到達しないはず
                    continue;
                }
                if (meshesDict.TryGetValue(texture, out var array))
                {
                    Array.Resize(ref array, array.Length + 1);
                    array[^1] = targetData;
                    meshesDict[texture] = array;
                }
                else
                {
                    meshesDict.Add(texture, new []{targetData});
                }
            }
            PackAndApplyTextures(textures, shader, renderMode, meshesDict, highCompression);
        }
        
        static Texture2D CreateMultipliedColorTexture(Texture2D texture2D, Color color)
        {
            Texture2D editedTexture;
            Color[] pixels; 
            if (texture2D != null && texture2D.isReadable)
            {
                editedTexture = new Texture2D(texture2D.width, texture2D.height);
                pixels = texture2D.GetPixels();
            }
            else
            {
                // If texture is not readable, use white texture
                editedTexture = new Texture2D(64, 64);
                pixels = Enumerable.Repeat(Color.white, Screen.width * Screen.height).ToArray();
            }
            // If color is used, Multiply the color to texture
            for (int j = 0; j < pixels.Length; j++)
            {
                pixels[j] *= color;
            }
            editedTexture.SetPixels(pixels);
            editedTexture.Apply();
            return editedTexture;
        }
        
        static int CalculateAtlasSize(Texture2D[] textures)
        {
            var score = 0;
            foreach (var texture in textures)
            {
                var max = Math.Max(texture.width, texture.height);
                score += max / 256;
            }
            int maximumAtlasSize;
            // 半分の解像度をターゲットにする 8*8(2048px)=64Scoreを1024pxに収める
            if (score > 64)
            {
                maximumAtlasSize = 2048;
            }
            else if (score > 16)
            {
                maximumAtlasSize = 1024;
            }
            else if (score > 4)
            {
                maximumAtlasSize = 512;
            }
            else
            {
                maximumAtlasSize = 256;
            }
            return maximumAtlasSize;
        }
        
        static void PackAndApplyTextures(Texture2D[] readableTextures, Shader shader, ShaderUtil.RenderMode renderMode, Dictionary<Texture2D, TargetData[]> meshesDict, bool highCompression = false)
        {
            var atlas = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var maxAtlasSize = 2048;
            if (highCompression)
            {
                maxAtlasSize = CalculateAtlasSize(readableTextures);
            }
            var rects = atlas.PackTextures(readableTextures, 0, maxAtlasSize, false);
            atlas.Apply();
            var material = new Material(shader);
            ShaderUtil.SetBlendMode(ref material, renderMode);
            material.mainTexture = atlas;
            for (int i = 0; i < rects.Length; i++)
            {
                var rect = rects[i];
                var targetDatas = meshesDict[readableTextures[i]];
                foreach (var t in targetDatas)
                {
                    var subMeshIndex = t.subMeshIndex;
                    // Apply material
                    var currentMaterials = t.skinnedMeshRenderer.sharedMaterials;
                    currentMaterials[subMeshIndex] = material;
                    t.skinnedMeshRenderer.materials = currentMaterials;
                    // Shift UVs
                    var mesh = t.skinnedMeshRenderer.sharedMesh;
                    var subMeshDescriptor = mesh.GetSubMesh(subMeshIndex);
                    var uvs = mesh.uv;
                    uvs = FixOffsetUvs(uvs);
                    var limit = Math.Min(subMeshDescriptor.firstVertex + subMeshDescriptor.vertexCount, uvs.Length);
                    for (int k = subMeshDescriptor.firstVertex; k < limit; k++)
                    {
                        var uv = uvs[k];
                        uvs[k] = new Vector2(rect.x + uv.x * rect.width, rect.y + uv.y * rect.height);
                    }
                    // sharedMeshしか編集できないため、既存のMeshに対象のsubMeshIndex箇所のみ上書きする
                    // 他のsubMeshIndexで既に編集されたMeshを再利用しないと他のsubMeshIndexで編集したUVの変更がリセットされてしまう
                    mesh.uv = uvs;
                    t.skinnedMeshRenderer.sharedMesh = mesh;
                }
            }
        }
        
        static Vector2[] FixOffsetUvs(Vector2[] uvs)
        {
            for (int i = 0; i < uvs.Length; i++)
            {
                var uv = uvs[i];
                // x
                if (uv.x > 1)
                {
                    while (uv.x > 1)
                    {
                        uv.x -= 1;
                    }
                }
                else if (uv.x < 0)
                {
                    while (uv.x < 0)
                    {
                        uv.x += 1;
                    }
                }
                // y
                if (uv.y > 1)
                {
                    while (uv.y > 1)
                    {
                        uv.y -= 1;
                    }
                }
                else if (uv.y < 0)
                {
                    while (uv.y < 0)
                    {
                        uv.y += 1;
                    }
                }
                uvs[i] = uv;
            }
            return uvs;
        }
    }
}
