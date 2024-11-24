using UnityEngine;
using ColorArray = Unity.Collections.NativeArray<UnityEngine.Color32>;

namespace ScreenSpaceDeforms{
public static class MetaTextureUtil
{
    // NOTE:
    // This system be able to make more faster by using BurstCompiler.
    // https://unity.com/ja/blog/engine-platform/accessing-texture-data-efficiently
    // https://discussions.unity.com/t/ecs-job-system-way-of-doing-texture2d-setpixel/724497/4

    public static (Texture2D texture, ColorArray pixelData) GenerateNewTexture(int width, int height, Color color)
    {
        // CAUTION:
        // Need to avoid SRGB(Default).

        var texture2D = new Texture2D(width, height, TextureFormat.RGBA32, mipChain:false, linear:true);
        var pixelData = texture2D.GetPixelData<Color32>(0);

        for (var i = 0; i < pixelData.Length; i++ )
        {
            pixelData[i] = color;
        }

        texture2D.Apply();

        return (texture2D, pixelData);
    }

    public static Color32 GetPixel(ColorArray pixelData, int width, int x, int y)
    {
        return pixelData[y * width + x];
    }

    public static void SetPixel(ColorArray pixelData, int width, int x, int y, Color32 color)
    {
        pixelData[y * width + x] = color;
    }

    public static void ApplyGaussianDistribution(ColorArray pixelData,
                                                 int   width,    int   height,
                                                 int   centerX,  int   centerY,
                                                 float sigma,    float power,
                                                 float clampMin, float clampMax, int clampTarget,
                                                 Color color)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var distance       = Mathf.Sqrt(Mathf.Pow(centerX - x, 2) + Mathf.Pow(centerY - y, 2));
                var gaussianFactor = Mathf.Exp(-Mathf.Pow(distance, 2) / (2 * Mathf.Pow(sigma, 2)));

                if (gaussianFactor <= 0.01f)
                {
                    continue;
                }

                var currentColor = GetPixel(pixelData, width, x, y);
                var newColor     = currentColor + color * gaussianFactor * power;

                newColor = new Color(clampTarget == 0 ? Mathf.Clamp(newColor.r, clampMin, clampMax) : newColor.r,
                                     clampTarget == 1 ? Mathf.Clamp(newColor.g, clampMin, clampMax) : newColor.g,
                                     clampTarget == 2 ? Mathf.Clamp(newColor.b, clampMin, clampMax) : newColor.b,
                                     clampTarget == 3 ? Mathf.Clamp(newColor.a, clampMin, clampMax) : newColor.a);

                SetPixel(pixelData, width, x, y, newColor);
            }
        }
    }
}}