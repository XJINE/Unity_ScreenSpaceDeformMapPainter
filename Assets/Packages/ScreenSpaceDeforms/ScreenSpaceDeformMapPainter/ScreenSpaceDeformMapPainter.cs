using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ScreenSpaceDeforms
{
    public partial class ScreenSpaceDeformMapPainter : MonoBehaviour
    {
        #region Field

        [SerializeField] private GameObject canvas;

        public Vector2Int initSize  = new (512, 512);
        public Color      initColor = new (0.5f, 0.5f, 0, 1);

        [Header("Paint Settings")]
        public float   paintPower  = 0.05f;
        public float   paintSigma  = 10f;
        public Vector2 paintClamp  = new (0, 1);
        public Color   paintColorL = new (1, 0, 0, 1);
        public Color   paintColorR = new (0, 1, 0, 1);

        public enum PaintMode { ScaleX, ScaleY, }
        [SerializeField]
        private PaintMode currentPaintMode = PaintMode.ScaleX;

        [Header("Sample Object")]
        [SerializeField]
        private GameObject       sampleObject;
        public  Vector2Int       sampleObjectCount = new (3, 3);
        public  float            sampleObjectScale = 1;
        private List<GameObject> sampleObjects = new ();

        // NOTE:
        // pixelData.Length shows texture.width * texture.height.
        // Ex. 512 * 512 = 262144.
        private Texture2D            _texture2D;
        private NativeArray<Color32> _pixelData;

        #endregion Field

        #region Method

        private void Awake()
        {
            InitializeTexture();
            InitializeSampleObjects();
            _guiWindow.IsVisible = false;
        }

        private void Update()
        {
            if (_guiWindow.IsVisible)
            {
                return;
            }

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                var screenCoord = Input.mousePosition;

                if(screenCoord.x < 0 || Screen.width  < screenCoord.x
                || screenCoord.y < 0 || Screen.height < screenCoord.y)
                {
                    return;
                }

                var uvCoord = new Vector2(screenCoord.x / Screen.width,
                                          screenCoord.y / Screen.height);

                var pixelCoord = new Vector2Int((int)(uvCoord.x * _texture2D.width),
                                                (int)(uvCoord.y * _texture2D.height));

                var color = currentPaintMode == PaintMode.ScaleX ? paintColorL : paintColorR;

                MetaTextureUtil.ApplyGaussianDistribution
                (_pixelData, _texture2D.width, _texture2D.height, pixelCoord.x, pixelCoord.y,
                  paintSigma,   paintPower * (Input.GetMouseButton(1) ? -1 : 1),
                  paintClamp.x, paintClamp.y, currentPaintMode == PaintMode.ScaleX ? 0 : 1,
                  color);

                _texture2D.Apply();
            }

            Shader.SetGlobalTexture("_GlobalScreenSpaceDeformTex", _texture2D);
            UpdateCanvasScale();
        }

        private void LoadTexture(Texture2D texture)
        {
            _texture2D = texture;
            _pixelData = _texture2D.GetPixelData<Color32>(0);
            canvas.GetComponent<Renderer>().material.mainTexture = _texture2D;
        }

        private void InitializeTexture()
        {
            (_texture2D, _pixelData) = MetaTextureUtil.GenerateNewTexture(initSize.x, initSize.y, initColor);
            canvas.GetComponent<Renderer>().material.mainTexture = _texture2D;
        }

        private void UpdateCanvasScale()
        {
            var camera       = Camera.main;
            var cameraHeight = 2f * camera.orthographicSize;
            var cameraWidth  = cameraHeight * camera.aspect;

            canvas.transform.localScale = new Vector3(cameraWidth, cameraHeight, 1f);
        }

        public void InitializeSampleObjects()
        {
            foreach (var sampleObject in sampleObjects)
            {
                Destroy(sampleObject);
            }
            sampleObjects.Clear();

            var bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
            var topRight   = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

            var xSpace = (topRight.x - bottomLeft.x) / (sampleObjectCount.x - 1);
            var ySpace = (topRight.y - bottomLeft.y) / (sampleObjectCount.y - 1);

            for (var x = 0; x < sampleObjectCount.x; x++)
            {
                for (var y = 0; y < sampleObjectCount.y; y++)
                {
                    var instance = Instantiate(sampleObject);

                    instance.transform.position = bottomLeft + new Vector3(x * xSpace, y * ySpace, 0);
                    instance.transform.localScale *= sampleObjectScale;

                    sampleObjects.Add(instance);
                }
            }
        }

        public bool ToggleSampleObjectsVisibility()
        {
            var visibility = !sampleObjects[0].activeSelf;

            foreach (var sampleObject in sampleObjects)
            {
                sampleObject.SetActive(visibility);
            }

            return visibility;
        }

        public void SetSampleObjectsVisibility(bool visibility)
        {
            foreach (var sampleObject in sampleObjects)
            {
                sampleObject.SetActive(visibility);
            }
        }

        #endregion Method
    }
}