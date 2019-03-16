using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Caustics : MonoBehaviour
{
    static public ComputeShader computeShader;
    static public int kernelId;

    static public RenderTexture causticsTexture;

    // the light cookies of the caustics light
    static private Cubemap causticsCubemap;

    // the baked caustics cubemap
    public Cubemap[] cookieCubemap = new Cubemap[12];
    private Texture2D[] cookieTexture = new Texture2D[12];


    //public Texture2D tmp;

    // the light sources
    public GameObject[] lights;

    // the caustics light
    private Light causticsLight;

    // the mesh of the MeshCollider
    private Mesh meshCollider;

    // Start is called before the first frame update
    void Start()
    {
        causticsLight = GetComponent<Light>();

        meshCollider = GetComponent<MeshCollider>().sharedMesh;

        causticsTexture = new RenderTexture(256 * 6, 256, 32);
        causticsTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        causticsTexture.enableRandomWrite = true;
        causticsTexture.useMipMap = false;
        causticsTexture.Create();
        causticsTexture.filterMode = FilterMode.Point;

        for (int i = 0; i < 12; ++i)
        {
            cookieTexture[i] = ToTexture2D(cookieCubemap[i]);
        }

        computeShader = Resources.Load<ComputeShader>("Shaders/TextureInterpolate");
        
        kernelId = computeShader.FindKernel("TexInterpolate");

        computeShader.SetTexture(kernelId, "textureOut", causticsTexture);

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

        causticsLight.color = w.x * c[0] + w.y * c[1] + w.z * c[2];

        computeShader.SetTexture(kernelId, "tex0", cookieTexture[0]);
        computeShader.SetTexture(kernelId, "tex1", cookieTexture[0]);
        computeShader.SetTexture(kernelId, "tex2", cookieTexture[0]);

        computeShader.SetVector("weight", new Vector4(w.x, w.y, w.z, 0.0f));

        computeShader.Dispatch(kernelId, 16, 16, 3);

        causticsCubemap = ToCubemap(causticsTexture);

        causticsLight.cookie = cookieCubemap[0];
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
        Texture2D tmp = new Texture2D(6 * renderTexture.height, renderTexture.height, TextureFormat.Alpha8, false);
        tmp.SetPixels(cubemapHorizontal.GetPixels());

        RenderTexture.active = null;

        Cubemap cubemap = new Cubemap(renderTexture.height, TextureFormat.Alpha8, false);

        for (int i = 0; i < 6; ++i)
        {
            cubemap.SetPixels(tmp.GetPixels(i * cubemapHorizontal.height, 0, cubemapHorizontal.height, cubemapHorizontal.height), faces[i]);
        }

        return cubemap;
    }

    private Cubemap ToCubemap(Texture2DArray array)
    {
        CubemapFace[] faces = new CubemapFace[] {
            CubemapFace.PositiveX, CubemapFace.NegativeX,
            CubemapFace.PositiveY, CubemapFace.NegativeY,
            CubemapFace.PositiveZ, CubemapFace.NegativeZ
        };

        Cubemap cubemap = new Cubemap(array.width, array.format, false);

        for (int i = 0; i < 6; ++i)
        {
            cubemap.SetPixels(array.GetPixels(i), faces[i]);
        }

        return cubemap;
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
