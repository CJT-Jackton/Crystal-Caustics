using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent, RequireComponent(typeof(MeshCollider))]
public class Caustics : MonoBehaviour
{
    #region Public Variables
    /// <summary>
    /// The list of pre-baked caustics cubemap.
    /// </summary>
    [Tooltip("The pre-baked caustics cubemap.")]
    public Cubemap[] cookieCubemap = new Cubemap[12];

    /// <summary>
    /// The list of light sources that cast caustics.
    /// </summary>
    [Tooltip("The light sources that cast caustics.")]
    public GameObject[] lights;

    #endregion

    #region Private Variables

    private ComputeShader _computeShader;
    private int _kernelId;

    private RenderTexture _causticsTexture;

    private RenderTexture[] _causticsRenderTextures = new RenderTexture[12];

    // the light cookies of the caustics light
    private RenderTexture _lightCookie;

    // the caustics light
    private Light _causticsLight;

    private MeshCollider _meshCollider;
    private Mesh _mesh;

    private int[,] _triangleIndex;

    #endregion

    public Texture2D tex2d1;
    public Texture2D tex2d2;

    public RenderTexture _testCookie;

    #region Unity Methods

    /// <summary>
    /// Initialize the variables and computer shader.
    /// </summary>
    private void Start()
    {
        InitTextures();

        _causticsLight = GetComponent<Light>();
        _causticsLight.color = transform.parent.gameObject.GetComponent<Renderer>().material.color;

        _meshCollider = GetComponent<MeshCollider>();
        _mesh = GetComponent<MeshCollider>().sharedMesh;

        InitTriangleIndex();
        InitComputeShader();
    }

    /// <summary>
    /// Update is called once per frame.
    /// </summary>
    private void Update()
    {
        foreach (GameObject lightsource in lights)
        {
            Light light = lightsource.GetComponent<Light>();

            if (light)
            {
                RaycastHit hit;

                Vector3 origin = new Vector3();
                Vector3 direction = new Vector3();

                if (light.type == LightType.Point)
                {
                    origin = light.transform.position;
                    direction = transform.position - light.transform.position;
                }
                else if (light.type == LightType.Directional)
                {
                    origin = transform.position - light.transform.forward;
                    direction = light.transform.forward;
                }
                else if (light.type == LightType.Spot)
                {
                    origin = light.transform.position;
                    direction = transform.position - light.transform.position;
                }

                if (Physics.Raycast(origin, direction, out hit))
                {
                    if (hit.collider == _meshCollider)
                    {
                        SetCookie(hit);
                    }
                }
            }
        }
    }

    #endregion

    #region Utility Methods

    private void SetCookie(RaycastHit hit)
    {
        int[] triangles = _mesh.triangles;

        int[] vertices = {
            triangles[hit.triangleIndex * 3 + 0],
            triangles[hit.triangleIndex * 3 + 1],
            triangles[hit.triangleIndex * 3 + 2]
        };

        //Debug.Log("Triangle " + hit.triangleIndex + ": (" + vertices[0] + ", " + vertices[1] + ", " + vertices[2] + ")");

        Color[] c = { new Color(), new Color(), new Color() };

        Vector4 v = _mesh.vertices[vertices[0]];
        v = v * 0.5f + new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        c[0] = v;

        v = _mesh.vertices[vertices[1]];
        v = v * 0.5f + new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        c[1] = v;

        v = _mesh.vertices[vertices[2]];
        v = v * 0.5f + new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        c[2] = v;

        //Vector3 w = hit.barycentricCoordinate;
        //causticsLight.color = w.x * c[0] + w.y * c[1] + w.z * c[2];

        int[] index = {
            _triangleIndex[hit.triangleIndex, 0],
            _triangleIndex[hit.triangleIndex, 1],
            _triangleIndex[hit.triangleIndex, 2]
        };

        _computeShader.SetInts("index", index);
        _computeShader.SetVector("weight", hit.barycentricCoordinate);

        _computeShader.Dispatch(_kernelId, 32, 32, 1);

        _lightCookie.Release();

        ToCubemap(_causticsTexture, ref _lightCookie);
        ToCubemap(_causticsTexture, ref _testCookie);

        _causticsLight.cookie = _lightCookie;
    }

    #endregion

    #region Initialize Functions

    private void InitTriangleIndex()
    {
        _triangleIndex = new int[_mesh.triangles.Length / 3, 3];

        int[] map = new int[_mesh.vertices.Length];

        for (int i = 0; i < _mesh.vertices.Length; ++i)
        {
            for (int j = 0; j < _mesh.vertices.Length; ++j)
            {
                if (_mesh.vertices[i] == _mesh.vertices[j])
                {
                    map[i] = j;
                    break;
                }
            }
        }

        for (int i = 0; i < _triangleIndex.GetLength(0); ++i)
        {
            for (int j = 0; j < _triangleIndex.GetLength(1); ++j)
            {
                _triangleIndex[i, j] = map[_mesh.triangles[i * 3 + j]];
            }
        }
    }

    private void InitTextures()
    {
        _lightCookie = new RenderTexture(256, 256, 16);
        _lightCookie.dimension = TextureDimension.Cube;
        _lightCookie.hideFlags = HideFlags.HideAndDontSave;
        _lightCookie.useMipMap = false;

        _causticsTexture = new RenderTexture(256, 256, 16);
        _causticsTexture.dimension = TextureDimension.Tex2DArray;
        _causticsTexture.volumeDepth = 6;
        _causticsTexture.format = RenderTextureFormat.Default;
        _causticsTexture.hideFlags = HideFlags.HideAndDontSave;
        _causticsTexture.enableRandomWrite = true;
        _causticsTexture.useMipMap = false;
        _causticsTexture.Create();
        _causticsTexture.filterMode = FilterMode.Point;

        for (int i = 0; i < 12; ++i)
        {
            _causticsRenderTextures[i] = ToRenderTexture(cookieCubemap[i]);
        }

        for (int k = 0; k < 12; ++k)
        {
            if (k % 3 == 1)
            {
                for (int i = 0; i < 6; ++i)
                {
                    Graphics.CopyTexture(tex2d1, 0, 0, _causticsRenderTextures[k],
                        i,
                        0);
                }
            }
            else if (k % 3 == 2)
            {
                for (int i = 0; i < 6; ++i)
                {
                    Graphics.CopyTexture(tex2d2, 0, 0, _causticsRenderTextures[k],
                        i,
                        0);
                }
            }
        }
    }

    private void InitComputeShader()
    {
        _computeShader = Resources.Load<ComputeShader>("Shaders/TextureInterpolate");
        _kernelId = _computeShader.FindKernel("TextureInterpolate");

        _computeShader.SetTexture(_kernelId, "Result", _causticsTexture);

        for (int i = 0; i < 12; ++i)
        {
            _computeShader.SetTexture(_kernelId, $"Tex[{i}]", _causticsRenderTextures[i]);
        }
    }

    #endregion

    #region Texture Manipulate Methods

    private Cubemap InterpolateTexture(Cubemap tex0, Cubemap tex1, Cubemap tex2, Vector3 weight)
    {
        Cubemap result = new Cubemap(256, TextureFormat.Alpha8, true);

        foreach (CubemapFace face in System.Enum.GetValues(typeof(CubemapFace)))
        {
            if (face == CubemapFace.Unknown)
            {
                continue;
            }

            for (int y = 0; y < 256; ++y)
            {
                for (int x = 0; x < 256; ++x)
                {
                    Color c0 = tex0.GetPixel(face, x, y);
                    Color c1 = tex1.GetPixel(face, x, y);
                    Color c2 = tex2.GetPixel(face, x, y);

                    Color color = weight.x * c0 + weight.y * c1 + weight.z * c2;

                    result.SetPixel(face, x, y, color);
                }
            }
        }

        result.Apply();
        return result;
    }

    private RenderTexture ToRenderTexture(Cubemap cubemap)
    {
        RenderTexture renderTexture = new RenderTexture(256, 256, 16);
        renderTexture.dimension = TextureDimension.Tex2DArray;
        renderTexture.volumeDepth = 6;
        renderTexture.format = RenderTextureFormat.Default;
        renderTexture.hideFlags = HideFlags.HideAndDontSave;
        renderTexture.enableRandomWrite = true;
        renderTexture.useMipMap = false;
        renderTexture.Create();
        renderTexture.filterMode = FilterMode.Point;

        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(cubemap, i, 0, renderTexture, i, 0);
        }

        return renderTexture;
    }

    private void ToCubemap(RenderTexture renderTexture, ref RenderTexture cubemapRenderTexture)
    {
        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(renderTexture, i, 0, cubemapRenderTexture, i, 0);
        }
    }

    #endregion
}
