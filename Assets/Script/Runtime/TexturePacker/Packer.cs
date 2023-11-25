using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UnityEngine;
using UnityEngine.Rendering;

namespace MsaI.TexturePacker
{
    public class Packer
    {
        static Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]> targetsDict = new Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]>();

        internal static void PackAssets(GameObject rootGameObject)
        {
            targetsDict = new Dictionary<(Texture texture, Shader shader, Color mainColor), TargetData[]>(); // Clear
            var vrmObjects = GetChildren(rootGameObject);
            foreach (var vrmObject in vrmObjects)
            {
                AddGameObjectToDict(vrmObject);
            }
            foreach (var target in targetsDict)
            {
                var key = target.Key;
                var targetDatas = target.Value;
                FlattenMaterials(key.texture, key.shader, key.mainColor, targetDatas);
            }
            var textures = targetsDict.Keys.Select(x => x.texture);
            
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
        
        static void AddGameObjectToDict(GameObject gameObject)
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
        
        static void ApplyPackings(Texture texture, Shader shader, Color mainColor, TargetData[] targetDatas)
        {
            // Create texture with multiplied color
            var texture2D = texture as Texture2D;
            var editedTexture = CreateMultipliedColorTexture(texture2D, mainColor);

            var skinMeshRenderers = targetDatas.Select(x => x.skinnedMeshRenderer).ToArray();
            var meshes = skinMeshRenderers.Select(x => x.sharedMesh).ToArray();
            var subMeshDescriptors = targetDatas.Select(x => x.subMeshDescriptor).ToArray();
            
            var result = PackTextures(editedTexture, meshes, subMeshDescriptors);
            var material = new Material(shader);
            material.mainTexture = result.Item1;
            var editedMeshes = result.Item2;
            
            for (int i = 0; i < targetDatas.Length; i++)
            {
                var skinnedMeshRenderer = targetDatas[i].skinnedMeshRenderer;
                var currentMaterials = skinnedMeshRenderer.sharedMaterials;
                currentMaterials[targetDatas[i].materialIndex] = material;
                skinnedMeshRenderer.sharedMaterials = currentMaterials;
                skinnedMeshRenderer.sharedMesh = editedMeshes[i];
            }
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
        
        static (Texture2D, Mesh[]) PackTextures(Texture2D[] readableTextures, Mesh[] meshes, SubMeshDescriptor[] subMeshDescriptors)
        {
            Mesh[] editedMeshes = new Mesh[meshes.Length];
            var atlas = new Texture2D(1024, 1024);
            var rects = atlas.PackTextures(readableTextures, 0);
            atlas.Apply();
            for (int i = 0; i < rects.Length; i++)
            {
                var uvs = meshes[i].uv;
                for (var j = 0; j < uvs.Length; j++)
                {
                    if (subMeshDescriptors[i].firstVertex <= j && j < subMeshDescriptors[i].firstVertex + subMeshDescriptors[i].vertexCount)
                    {
                        var uv = meshes[i].uv[j];
                        var rect = rects[i];
                        uvs[j] = new Vector2(rect.x + uv.x * rect.width, rect.y + uv.y * rect.height);
                    }
                }
                var mesh = meshes[i];
                mesh.uv = uvs;
                editedMeshes[i] = mesh;
            }
            return (atlas, editedMeshes);
        }
    }
}
