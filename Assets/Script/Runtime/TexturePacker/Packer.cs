using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UnityEngine;
using UnityEngine.Rendering;

namespace MsaI.TexturePacker
{
    public class Packer
    {
        static Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]> targetsDict;
        static Dictionary<Shader, TargetData[]> atlasTargetsDict;

        internal static void PackAssets(GameObject rootGameObject)
        {
            //init
            targetsDict = new Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]>();
            atlasTargetsDict = new Dictionary<Shader, TargetData[]>();
            
            var vrmObjects = GetChildren(rootGameObject);
            foreach (var vrmObject in vrmObjects)
            {
                CreateTargetDict(vrmObject);
            }
            foreach (var target in targetsDict)
            {
                var key = target.Key;
                var targetDatas = target.Value;
                FlattenMaterials(key.texture, key.shader, key.mainColor, targetDatas);
            }
            foreach (var target in atlasTargetsDict)
            {
                var shader = target.Key;
                var targetDatas = target.Value;
                ApplyPackings(shader, targetDatas);
            }
        }

        static GameObject[] GetChildren(GameObject gameObject)
        {
            var children = new List<GameObject>();
            var transforms = gameObject.transform.GetChildren();
            foreach (var transform in transforms)
            {
                children.Add(transform.gameObject);
                children.AddRange(GetChildren(transform.gameObject));
            }
            return children.ToArray();
        }
        
        static void CreateTargetDict(GameObject gameObject)
        {
            var skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.enabled = true;
                var materials = skinnedMeshRenderer.sharedMaterials;
                var mesh = skinnedMeshRenderer.sharedMesh;
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    var subMesh = mesh.GetSubMesh(i);
                    //create dictionary for distinct texture, shader, color
                    var key = (materials[i].mainTexture, materials[i].shader, materials[i].color);
                    if (targetsDict.TryGetValue(key, out var targetDatas))
                    {
                        targetDatas = new List<TargetData>(targetDatas){new TargetData(skinnedMeshRenderer, subMesh, i)}.ToArray();
                        targetsDict[key] = targetDatas;
                    }
                    else
                    {
                        targetsDict.Add(key, new []{new TargetData(skinnedMeshRenderer, subMesh, i)});
                    }
                    //create dictionary for atlas
                    var shader = materials[i].shader;
                    if (atlasTargetsDict.TryGetValue(shader, out var atlasTargetDatas))
                    {
                        atlasTargetDatas = new List<TargetData>(atlasTargetDatas){new TargetData(skinnedMeshRenderer, subMesh, i)}.ToArray();
                        atlasTargetsDict[shader] = atlasTargetDatas;
                    }
                    else
                    {
                        atlasTargetsDict.Add(shader, new []{new TargetData(skinnedMeshRenderer, subMesh, i)});
                    }
                }
            }
        }

        static void FlattenMaterials(Texture texture, Shader shader, Color mainColor, TargetData[] targetDatas)
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
        
        static void ApplyPackings(Shader shader, TargetData[] targetDatas)
        {
            var skinMeshRenderers = targetDatas.Select(x => x.skinnedMeshRenderer).ToArray();
            var meshes = skinMeshRenderers.Select(x => x.sharedMesh).ToArray();
            var subMeshDescriptors = targetDatas.Select(x => x.subMeshDescriptor).ToArray();
            var materials = targetDatas.Select(x => x.skinnedMeshRenderer.sharedMaterials[x.materialIndex]).ToArray();
            var textures = materials.Select(x => x.mainTexture as Texture2D).ToArray();
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
            PackAndApplyTextures(textures, shader, meshesDict);
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
        
        static void PackAndApplyTextures(Texture2D[] readableTextures, Shader shader, Dictionary<Texture2D, TargetData[]> meshesDict)
        {
            var atlas = new Texture2D(1024, 1024);
            var rects = atlas.PackTextures(readableTextures, 0);
            atlas.Apply();
            var material = new Material(shader);
            material.mainTexture = atlas;
            var meshesArray = new List<Mesh>();
            for (int i = 0; i < rects.Length; i++)
            {
                var targetDatas = meshesDict[readableTextures[i]];
                var editedMeshes = new Mesh[targetDatas.Length];
                for (int j = 0; j < targetDatas.Length; j++)
                {
                    var mesh = targetDatas[j].skinnedMeshRenderer.sharedMesh;
                    var subMeshDescriptor = targetDatas[j].subMeshDescriptor;
                    
                    var uvs = mesh.uv;
                    for (var k = 0; k < uvs.Length; k++)
                    {
                        if (subMeshDescriptor.firstVertex <= k && k < subMeshDescriptor.firstVertex + subMeshDescriptor.vertexCount)
                        {
                            var uv = mesh.uv[k];
                            var rect = rects[i];
                            uvs[k] = new Vector2(rect.x + uv.x * rect.width, rect.y + uv.y * rect.height);
                        }
                    }
                    mesh.uv = uvs;
                    editedMeshes[j] = mesh;
                    targetDatas[j].skinnedMeshRenderer.sharedMesh = mesh;
                    var currentMaterials = targetDatas[j].skinnedMeshRenderer.sharedMaterials;
                    currentMaterials[targetDatas[j].materialIndex] = material;
                    targetDatas[j].skinnedMeshRenderer.sharedMaterials = currentMaterials;
                }
            }
        }
    }
}
