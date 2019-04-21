using UnityEngine;
using UnityEngine.Rendering;

public class Caustics : MonoBehaviour
{
    #region Public Variables
    static public ComputeShader computeShader;
    static public int kernelId;

    static public RenderTexture causticsTexture;
    #endregion

    // the light cookies of the caustics light
    static private Cubemap causticsCubemap;

    // the baked caustics cubemap
    public Cubemap[] cookieCubemap = new Cubemap[12];
    private Texture2DArray[] cookieTexture = new Texture2DArray[12];

    private RenderTexture[] causticsRenderTextures = new RenderTexture[12];

    //public Texture2D tmp;

    // the light sources
    public GameObject[] lights;

    // the caustics light
    private Light causticsLight;

    // the mesh of the MeshCollider
    private Mesh meshCollider;

    private RenderTexture _lightCookie;
    public Texture2D tex2d;

    private int[,] _vertices = new int[20, 3]{ { 2 , 3 , 7},
        { 2, 8 ,3},
        { 4 , 5 , 6},
        {5 , 4 , 9},
            {7 , 6 , 12},
                { 6 , 7 , 11},
                    { 10 , 11 , 3},
                        { 11 , 10 , 4},
                            {8 , 9 , 10},
                                { 9,  8 , 1},
                                    { 12 , 1 , 2},
                                        {1 , 12 , 5},
                                            {7 , 3 , 11},
                                                { 2 , 7  ,12},
                                                    { 4  ,6 , 11},
                                                        { 6 , 5,  12},
                                                            { 3 , 8 , 10},
                                                                { 8 , 2 , 1},
                                                                    { 4 , 10 , 9},
                                                                        { 5,  9,  1 }};

    #region Unity Methods

    void Start()
    {
        _lightCookie = new RenderTexture(256, 256, 16);
        _lightCookie.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        _lightCookie.hideFlags = HideFlags.HideAndDontSave;
        _lightCookie.useMipMap = false;

        causticsLight = GetComponent<Light>();

        meshCollider = GetComponent<MeshCollider>().sharedMesh;

        causticsTexture = new RenderTexture(256, 256, 16);
        causticsTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        causticsTexture.volumeDepth = 6;
        causticsTexture.format = RenderTextureFormat.Default;
        causticsTexture.hideFlags = HideFlags.HideAndDontSave;
        causticsTexture.enableRandomWrite = true;
        causticsTexture.useMipMap = false;
        causticsTexture.Create();
        causticsTexture.filterMode = FilterMode.Point;

        for (int i = 0; i < 12; ++i)
        {
            cookieTexture[i] = ToTexture2DArray(cookieCubemap[i]);

            causticsRenderTextures[i] = ToRenderTexture(cookieCubemap[i]);
        }

        for (int k = 0; k < 12; ++k)
        {
            if (k % 2 == 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    Graphics.CopyTexture(tex2d, 0, 0, causticsRenderTextures[k],
                        i,
                        0);
                }
            }
        }

        causticsLight.color = transform.parent.gameObject.GetComponent<Renderer>().material.color;

        computeShader = Resources.Load<ComputeShader>("Shaders/TextureInterpolate");

        kernelId = computeShader.FindKernel("TextureInterpolate");

        computeShader.SetTexture(kernelId, "Result", causticsTexture);

        computeShader.SetVector("weight", new Vector4());
    }

    // Update is called once per frame
    void Update()
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
                    if (hit.collider == GetComponent<MeshCollider>())
                    {
                        SetCookie(hit);
                    }
                }
            }
        }
    }

    #endregion


    private void SetCookie(RaycastHit hit)
    {
        int[] triangles = meshCollider.triangles;

        int[] vertices = {
            triangles[hit.triangleIndex * 3 + 0],
            triangles[hit.triangleIndex * 3 + 1],
            triangles[hit.triangleIndex * 3 + 2]
        };

        //Debug.Log("Triangle " + hit.triangleIndex + ": (" + vertices[0] + ", " + vertices[1] + ", " + vertices[2] + ")");

        Color[] c = { new Color(), new Color(), new Color() };

        Vector4 v = meshCollider.vertices[vertices[0]];
        v = v * 0.5f + new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        c[0] = v;

        v = meshCollider.vertices[vertices[1]];
        v = v * 0.5f + new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        c[1] = v;

        v = meshCollider.vertices[vertices[2]];
        v = v * 0.5f + new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        c[2] = v;

        Vector3 w = hit.barycentricCoordinate;

        //causticsLight.color = w.x * c[0] + w.y * c[1] + w.z * c[2];

        int[] index = {
            _vertices[hit.triangleIndex, 0] - 1,
            _vertices[hit.triangleIndex, 1] - 1,
            _vertices[hit.triangleIndex, 2] - 1
        };

        Debug.Log(index[0] + "," + index[1] + "," + index[2]);

        computeShader.SetTexture(kernelId, "Tex0", causticsRenderTextures[index[0]]);
        computeShader.SetTexture(kernelId, "Tex1", causticsRenderTextures[index[1]]);
        computeShader.SetTexture(kernelId, "Tex2", causticsRenderTextures[index[2]]);

        computeShader.SetVector("weight", new Vector4(w.x, w.y, w.z, 0.0f));

        computeShader.Dispatch(kernelId, 32, 32, 1);

        ToCubemap(causticsTexture, ref _lightCookie);

        causticsLight.cookie = _lightCookie;
    }

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
        CubemapFace[] faces = new CubemapFace[] {
            CubemapFace.PositiveX, CubemapFace.NegativeX,
            CubemapFace.PositiveY, CubemapFace.NegativeY,
            CubemapFace.PositiveZ, CubemapFace.NegativeZ
        };

        RenderTexture renderTexture = new RenderTexture(256, 256, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        renderTexture.volumeDepth = 6;
        renderTexture.format = RenderTextureFormat.Default;
        renderTexture.useMipMap = false;
        renderTexture.Create();
        renderTexture.filterMode = FilterMode.Point;

        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(cubemap, i, 0, renderTexture, i, 0);
        }

        return renderTexture;
    }

    private Cubemap ToCubemap(RenderTexture renderTexture)
    {
        CubemapFace[] faces = new CubemapFace[] {
            CubemapFace.PositiveX, CubemapFace.NegativeX,
            CubemapFace.PositiveY, CubemapFace.NegativeY,
            CubemapFace.PositiveZ, CubemapFace.NegativeZ
        };

        RenderTexture.active = renderTexture;

        Texture2D cubemapHorizontal = new Texture2D(6 * renderTexture.height, renderTexture.height, TextureFormat.RGBA32, false);
        cubemapHorizontal.ReadPixels(new Rect(0, 0, renderTexture.height, renderTexture.height), 0, 0);
        Texture2D tmp = new Texture2D(6 * renderTexture.height, renderTexture.height, TextureFormat.RGB24, false);
        tmp.SetPixels(cubemapHorizontal.GetPixels());

        RenderTexture.active = null;

        Cubemap cubemap = new Cubemap(renderTexture.height, TextureFormat.RGB24, false);

        for (int i = 0; i < 6; ++i)
        {
            cubemap.SetPixels(tmp.GetPixels(i * cubemapHorizontal.height, 0, cubemapHorizontal.height, cubemapHorizontal.height), faces[i]);
        }

        return cubemap;
    }

    private void ToCubemap(RenderTexture renderTexture, ref RenderTexture cubemapRenderTexture)
    {
        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(renderTexture, i, 0, cubemapRenderTexture, i, 0);
        }
    }

    private void ToCubemap(RenderTexture[] renderTextures, ref Cubemap cubemap)
    {
        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(renderTextures[i], 0, 0, cubemap, i, 0);
        }
    }

    private void ToRenderTextureArray(Cubemap cubemap, ref RenderTexture[] renderTextures)
    {
        for (int i = 0; i < 6; ++i)
        {
            Graphics.CopyTexture(cubemap, i, 0, renderTextures[i], 0, 0);
        }
    }

    /// <summary>
    /// Convert the Texture2DArray into Cubemap.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="cubemap"></param>
    private void ToCubemap(Texture2DArray array, ref Cubemap cubemap)
    {
        CubemapFace[] faces = new CubemapFace[] {
            CubemapFace.PositiveX, CubemapFace.NegativeX,
            CubemapFace.PositiveY, CubemapFace.NegativeY,
            CubemapFace.PositiveZ, CubemapFace.NegativeZ
        };

        for (int i = 0; i < 6; ++i)
        {
            cubemap.SetPixels(array.GetPixels(i), faces[i]);
        }
    }

    private Texture2DArray ToTexture2DArray(Cubemap cubemap)
    {
        CubemapFace[] faces = new CubemapFace[] {
            CubemapFace.PositiveX, CubemapFace.NegativeX,
            CubemapFace.PositiveY, CubemapFace.NegativeY,
            CubemapFace.PositiveZ, CubemapFace.NegativeZ
        };

        Texture2DArray array = new Texture2DArray(cubemap.width, cubemap.width, 6, cubemap.format, false);

        for (int i = 0; i < 6; ++i)
        {
            array.SetPixels(cubemap.GetPixels(faces[i]), i, 0);
        }

        return array;
    }

    private Texture2D ToTexture2D(Cubemap cubemap)
    {
        CubemapFace[] faces = new CubemapFace[] {
            CubemapFace.PositiveX, CubemapFace.NegativeX,
            CubemapFace.PositiveY, CubemapFace.NegativeY,
            CubemapFace.PositiveZ, CubemapFace.NegativeZ
        };

        Texture2D texture = new Texture2D(6 * cubemap.width, cubemap.width, cubemap.format, false);

        for (int i = 0; i < 6; ++i)
        {
            texture.SetPixels(i * cubemap.width, 0, cubemap.width, cubemap.width, cubemap.GetPixels(faces[i]));
        }

        return texture;
    }
}
