using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MsaI.TexturePacker
{
    public class Packer
    {
        static Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]> targetsDict;
        static Dictionary<(Shader shader, ShaderUtil.RenderMode renderMode), TargetData[]> atlasTargetsDict;

        internal static void PackAssets(GameObject rootGameObject)
        {
            //init
            targetsDict = new Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]>();
            atlasTargetsDict = new Dictionary<(Shader shader, ShaderUtil.RenderMode renderMode), TargetData[]>();
            
            CreateTargetDict(rootGameObject);
            foreach (var target in targetsDict)
            {
                var key = target.Key;
                var targetDatas = target.Value;
                BakeTextures(key.texture, key.shader, key.mainColor, targetDatas);
            }
            foreach (var target in atlasTargetsDict)
            {
                var key = target.Key;
                var targetDatas = target.Value;
                ApplyPackings(key.shader, key.renderMode, targetDatas);
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
            skinnedMeshRenderer.enabled = true;
            var materials = skinnedMeshRenderer.sharedMaterials;
            var mesh = skinnedMeshRenderer.sharedMesh;
            for (int i = 0; i < materials.Length; i++)
            {
                var subMesh = mesh.GetSubMesh(i);
                var newTargetData = new TargetData(skinnedMeshRenderer, subMesh, i);
                //create dictionary for distinct texture, shader, color
                var key = (materials[i].mainTexture, materials[i].shader, materials[i].color);
                if (targetsDict.TryGetValue(key, out var targetDatas))
                {
                    targetDatas = new List<TargetData>(targetDatas){newTargetData}.ToArray();
                    targetsDict[key] = targetDatas;
                }
                else
                {
                    targetsDict.Add(key, new []{newTargetData});
                }
                //create dictionary for atlas
                var atlasKey = (materials[i].shader, ShaderUtil.GetRenderMode(materials[i]));
                if (atlasTargetsDict.TryGetValue(atlasKey, out var atlasTargetDatas))
                {
                    atlasTargetDatas = new List<TargetData>(atlasTargetDatas){newTargetData}.ToArray();
                    atlasTargetsDict[atlasKey] = atlasTargetDatas;
                }
                else
                {
                    atlasTargetsDict.Add(atlasKey, new []{newTargetData});
                }
            }
        }

        static void BakeTextures(Texture texture, Shader shader, Color mainColor, TargetData[] targetDatas)
        {
            // Create texture with multiplied color
            var texture2D = texture as Texture2D;
            var editedTexture = CreateMultipliedColorTexture(texture2D, mainColor);
            var material = new Material(shader);
            material.mainTexture = editedTexture;

            for (int i = 0; i < targetDatas.Length; i++)
            {
                var skinnedMeshRenderer = targetDatas[i].skinnedMeshRenderer;
                var currentMaterials = skinnedMeshRenderer.sharedMaterials;
                currentMaterials[targetDatas[i].materialIndex] = material;
                skinnedMeshRenderer.sharedMaterials = currentMaterials;
            }
        }
        
        static void ApplyPackings(Shader shader, ShaderUtil.RenderMode renderMode, TargetData[] targetDatas)
        {
            var materials = targetDatas.Select(x => x.skinnedMeshRenderer.sharedMaterials[x.materialIndex]).ToArray();
            var textures = materials.Select(x => x.mainTexture as Texture2D).Distinct().ToArray();
            var meshesDict = new Dictionary<Texture2D, TargetData[]>();
            foreach (var targetData in targetDatas)
            {
                var texture = targetData.skinnedMeshRenderer.sharedMaterials[targetData.materialIndex].mainTexture as Texture2D;
                if (meshesDict.TryGetValue(texture, out var array))
                {
                    meshesDict[texture] = new List<TargetData>(array){targetData}.ToArray();
                }
                else
                {
                    meshesDict.Add(texture, new []{targetData});
                }
            }
            PackAndApplyTextures(textures, shader, renderMode, meshesDict);
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
        
        static void PackAndApplyTextures(Texture2D[] readableTextures, Shader shader, ShaderUtil.RenderMode renderMode, Dictionary<Texture2D, TargetData[]> meshesDict)
        {
            var atlas = new Texture2D(1024, 1024);
            var maximumAtlasSize = 512;
            if (readableTextures.Length > 9)
            {
                maximumAtlasSize = 2048;
            }
            else if (readableTextures.Length > 4)
            {
                maximumAtlasSize = 1024;
            }
            var rects = atlas.PackTextures(readableTextures, 0, maximumAtlasSize);
            atlas.Apply();
            var material = new Material(shader);
            material = ShaderUtil.SetBlendMode(material, renderMode);
            material.mainTexture = atlas;
            for (int i = 0; i < rects.Length; i++)
            {
                var rect = rects[i];
                var targetDatas = meshesDict[readableTextures[i]];
                for (int j = 0; j < targetDatas.Length; j++)
                {
                    var mesh = targetDatas[j].skinnedMeshRenderer.sharedMesh;
                    var subMeshDescriptor = targetDatas[j].subMeshDescriptor;
                    var uvs = mesh.uv;
                    for (var k = 0; k < uvs.Length; k++)
                    {
                        if (subMeshDescriptor.firstVertex <= k && k < subMeshDescriptor.firstVertex + subMeshDescriptor.vertexCount)
                        {
                            var uv = uvs[k];
                            var uvx = uv.x % 1;
                            var uvy = uv.y % 1;
                            uvs[k] = new Vector2(rect.x + uvx * rect.width, rect.y + uvy * rect.height);
                        }
                    }
                    mesh.uv = uvs;
                    targetDatas[j].skinnedMeshRenderer.sharedMesh = mesh;
                    var currentMaterials = targetDatas[j].skinnedMeshRenderer.sharedMaterials;
                    currentMaterials[targetDatas[j].materialIndex] = material;
                    targetDatas[j].skinnedMeshRenderer.sharedMaterials = currentMaterials;
                }
            }
        }
    }
}
