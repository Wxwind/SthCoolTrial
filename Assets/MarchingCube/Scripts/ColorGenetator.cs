using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
public class ColorGenetator : MonoBehaviour
{
    public Material mat;
    public Gradient gradient;
[PreviewField]
    public Texture2D texture;
    private static readonly int GradColorTex = Shader.PropertyToID("_GradColorTex");
    const int textureResolution = 50;

    void Init () {
        if (texture == null || texture.width != textureResolution) {
            texture = new Texture2D (textureResolution, 1, TextureFormat.RGBA32, false);
        }
    }

    void OnEnable () {
        Init ();
        UpdateTexture ();

        MeshGenerator m = FindObjectOfType<MeshGenerator> ();
        mat.SetTexture (GradColorTex, texture);
        
        // byte[] data = texture.EncodeToPNG();
        // //保存图片到 你的路径
        // var timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        // FileStream file = File.Create("./aaa.png");
        // file.Write(data, 0, data.Length);
        // file.Close();
        // Debug.Log("saveImage");
    }

    void UpdateTexture () {
        if (gradient != null) {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++) {
                Color gradientCol = gradient.Evaluate (i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            texture.SetPixels (colours);
            texture.Apply ();
        }
    }
}
