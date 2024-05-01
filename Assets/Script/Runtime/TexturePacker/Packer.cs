using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            Debug.Log($"Packing {textures.Length} textures");
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
            // 重複処理を避けるためのDictionary
            var processedMeshDict = new Dictionary<(Mesh mesh, int subMeshIndex), Mesh>();
            for (int i = 0; i < rects.Length; i++)
            {
                var rect = rects[i];
                var targetDatas = meshesDict[readableTextures[i]];
                for (int j = 0; j < targetDatas.Length; j++)
                {
                    var mesh = targetDatas[j].skinnedMeshRenderer.sharedMesh;
                    var subMeshIndex = targetDatas[j].subMeshIndex;
                    var subMeshDescriptor = mesh.GetSubMesh(subMeshIndex);
                    if (processedMeshDict.TryGetValue((mesh, subMeshIndex), out var processedMesh) && processedMesh != null)
                    {
                        continue;
                    }
                    var uvs = mesh.uv;
                    var limit = Math.Min(subMeshDescriptor.firstVertex + subMeshDescriptor.vertexCount, uvs.Length);
                    for (int k = subMeshDescriptor.firstVertex; k < limit; k++)
                    {
                        var uv = uvs[k];
                        var uvx = uv.x % 1;
                        var uvy = uv.y % 1;
                        uvs[k] = new Vector2(rect.x + uvx * rect.width, rect.y + uvy * rect.height);
                    }
                    mesh.uv = uvs;
                    processedMeshDict[(mesh, subMeshIndex)] = mesh;
                    targetDatas[j].skinnedMeshRenderer.sharedMesh = mesh;
                    // Set material
                    var currentMaterials = targetDatas[j].skinnedMeshRenderer.sharedMaterials;
                    currentMaterials[subMeshIndex] = material;
                    targetDatas[j].skinnedMeshRenderer.materials = currentMaterials;
                }
            }
        }
    }
}
