using UnityEditor;
using UnityEngine;

namespace Matterless.Floorcraft.Editor
{
    public class TextureArrayCreator  
    {
        [MenuItem("Matterless/CreateTextureArray")]
        static void CreateTextureArray()
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.TopLevel);
            Debug.Log(selectedObjects.Length);
            Texture2D[] textures = new Texture2D[selectedObjects.Length];
            
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                textures[i] = (Texture2D) selectedObjects[i];
            }            
            
            Texture2DArray textureArray = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, false);
            for (int i = 0; i < textures.Length; i++)
                textureArray.SetPixels(textures[i].GetPixels(0), i, 0);
            
            textureArray.Apply();
            AssetDatabase.CreateAsset(textureArray, "Assets/TextureArray.asset");
        }
        
        [MenuItem("Matterless/CreateNormalMapTextureArray")]
        static void CreateNormalMapTextureArray()
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.TopLevel);
            Texture2D[] textures = new Texture2D[selectedObjects.Length];
            
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                textures[i] = (Texture2D) selectedObjects[i];
            }
            
            // Why we need this -> https://docs.unity3d.com/ScriptReference/QualitySettings-masterTextureLimit.html
            int storedQualitySetting = QualitySettings.masterTextureLimit;
            QualitySettings.masterTextureLimit = 0;
            
            Texture2DArray textureArray = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, 9, true);
            for (int i = 0; i < textures.Length; i++)
            for (int mip = 0; mip < textureArray.mipmapCount; mip++)
                Graphics.CopyTexture(textures[i], 0, mip, textureArray, i, mip);
            
            //textureArray.Apply();
            AssetDatabase.CreateAsset(textureArray, "Assets/TextureArray.asset");
            
            QualitySettings.masterTextureLimit = storedQualitySetting;
        }
    }
}

