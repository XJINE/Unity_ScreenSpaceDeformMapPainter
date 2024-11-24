using UnityEngine;
using XGUI;
using System.IO;

namespace ScreenSpaceDeforms {
public partial class ScreenSpaceDeformMapPainter {

    #region Field

    private readonly FlexWindow _guiWindow = new (nameof(ScreenSpaceDeformMapPainter)) { MinWidth  = 300, MinHeight = 200 };

    private static readonly FoldoutPanel PaintPanel      = new ("Paint");
    private static readonly FoldoutPanel SamplePanel     = new ("Sample");
    private static readonly FoldoutPanel InitializePanel = new ("Initialize");
    private static readonly FoldoutPanel SaveLoadPanel   = new ("Save / Load");

    private readonly FlexGUI<PaintMode>  _paintModeGUI    = new ("Paint Mode");
    private readonly FlexGUI<float>      _powerGUI        = new ("Power", 0, 255);
    private readonly FlexGUI<float>      _sigmaGUI        = new ("Sigma", 0, 255);
    private readonly FlexGUI<Vector2>    _clampGUI        = new ("Clamp",         Vector2.one * -1, Vector2.one * 1)       { Slider = false };
    private readonly FlexGUI<Vector2Int> _samplesCountGUI = new ("Samples Count", Vector2Int.zero,  Vector2Int.one * 4096) { Slider = false };
    private readonly FlexGUI<float>      _samplesScaleGUI = new ("Samples Scale", 0,                100)                   { Slider = false };
    private readonly FlexGUI<Vector2Int> _initSizeGUI     = new ("Init Size",     Vector2Int.zero,  Vector2Int.one * 4096) { Slider = false };

    private static string _statusMessage = "";
    private string        _loadFilePath  = "";

    #endregion Field

    public void OnGUI()
    {
        DrawLabel();

        _guiWindow.Show(() =>
        {
            GUILayout.Label("Status : " + _statusMessage);

            PaintPanel.Show(() =>
            {
                currentPaintMode = _paintModeGUI.Show(currentPaintMode);
                paintPower       = _powerGUI    .Show(paintPower);
                paintSigma       = _sigmaGUI    .Show(paintSigma);
                paintClamp       = _clampGUI    .Show(paintClamp);
            });

            SamplePanel.Show(() =>
            {
                sampleObjectCount = _samplesCountGUI.Show(sampleObjectCount);
                sampleObjectScale = _samplesScaleGUI.Show(sampleObjectScale);

                if(GUILayout.Button("Create Samples"))
                {
                    InitializeSampleObjects();
                }

                if (GUILayout.Button("Toggle Visibility"))
                {
                    ToggleSampleObjectsVisibility();
                }
            });

            InitializePanel.Show(() =>
            {
                initSize = _initSizeGUI.Show(initSize);

                if (GUILayout.Button("Initialize Texture"))
                {
                    InitializeTexture();
                }
            });

            SaveLoadPanel.Show(() =>
            {
                _loadFilePath = GUILayout.TextField(_loadFilePath);

                if (GUILayout.Button("Load"))
                {
                    LoadTexture(_loadFilePath);
                }
                if (GUILayout.Button("Save"))
                {
                    SaveTexture(_texture2D);
                }
            });
        });
    }

    public void ToggleGUI()
    {
        _guiWindow.IsVisible = !_guiWindow.IsVisible;
    }

    public void DrawLabel()
    {
        var pixelCoord = Input.mousePosition;
        if(pixelCoord.x < 0 || Screen.width  < pixelCoord.x
        || pixelCoord.y < 0 || Screen.height < pixelCoord.y)
        {
            return;
        }

        var style = new GUIStyle(GUI.skin.label);
        style.fontStyle = FontStyle.Bold;
        style.fontSize  = 18;
        style.normal.textColor = Color.black;

        var offset    = Vector2.one * 20;
        var labelRect = new Rect(new Vector2(pixelCoord.x, Screen.height - pixelCoord.y) + offset, Vector2.one * 300);

        var texCoordX = (int)(_texture2D.width  * (pixelCoord.x / Screen.width ));
        var texCoordY = (int)(_texture2D.height * (pixelCoord.y / Screen.height));

        var pixelData = MetaTextureUtil.GetPixel(_pixelData, _texture2D.width, texCoordX, texCoordY);

        var textRG = "RG：" + pixelData.r + ", " + pixelData.g;
        var text01 = "01:" + (pixelData.r / 255f).ToString("F2") + ", " + (pixelData.g / 255f).ToString("F2");
        var textXY = "XY:" + texCoordX + ", " + texCoordY;
        var text = textRG + "\n" + text01 + "\n" + textXY;

        GUI.Label(labelRect, text, style);
    }

    private static void BoxGroupedGUI(System.Action action)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        action();
        GUILayout.EndVertical();
    }

    private void LoadTexture(string filePath)
    {
        filePath = filePath.Trim('"');

        if (!File.Exists(filePath))
        {
            _statusMessage = "File not found : " + filePath;
        }

        // CAUTION:
        // LoadImage makes Texture2D in ARGB32.
        // However, GetPixelData<Color32> uses RGBA32.

        var fileData    = File.ReadAllBytes(filePath);
        var argbTexture = new Texture2D(0, 0, TextureFormat.RGBA32, mipChain:false, linear:true);

        _statusMessage = argbTexture.LoadImage(fileData) ?
                         "Load success : " : "Load failed : " + filePath;

        var rgbaTexture = new Texture2D(argbTexture.width, argbTexture.height, TextureFormat.RGBA32, mipChain:false, linear:true);
            rgbaTexture.SetPixels(argbTexture.GetPixels());
            rgbaTexture.Apply();

        DestroyImmediate(argbTexture);
        LoadTexture(rgbaTexture);
    }

    private void SaveTexture(Texture2D texture2D)
    {
        var bytes    = texture2D.EncodeToPNG();
        var basePath = Application.dataPath + "/DeformMap";
        var path     = basePath + ".png";

        while (File.Exists(path))
        {
            path = basePath + "_" + System.DateTime.Now.ToString("yyMMddHHmmss") + ".png";
        }

        File.WriteAllBytes(path, bytes);
        System.Diagnostics.Process.Start(Path.GetDirectoryName(path));

        _loadFilePath = path;
    }
}}