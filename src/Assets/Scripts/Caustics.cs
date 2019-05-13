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

    /// <summary>
    /// The texture interpolation compute shader.
    /// </summary>
    private ComputeShader _computeShader;

    /// <summary>
    /// The kernel id of the texture interpolation.
    /// </summary>
    private int _kernelId;

    /// <summary>
    /// The caustics map render texture in texture2d array form.
    /// </summary>
    private RenderTexture _causticsTexture;

    /// <summary>
    /// The caustics map render texture. 
    /// </summary>
    private RenderTexture[] _causticsRenderTextures = new RenderTexture[12];

    // the light cookies of the caustics light
    private RenderTexture _lightCookie;

    /// <summary>
    /// The point light that used to cast caustics.
    /// </summary>
    private Light _causticsLight;

    private MeshCollider _meshCollider;
    private Mesh _mesh;

    private int[] _sampleIndex;

    private Vector3 _sampleWeight;

    /// <summary>
    /// The sample correction matrix for each caustics map.
    /// </summary>
    private Matrix4x4[] _sampleMatrix;

    /// <summary>
    /// The vertex index of triangle got hit.
    /// </summary>
    private int[,] _triangleIndex;

    /// <summary>
    /// The layer mask use for ray cast collision test.
    /// </summary>
    private int _layerMask;

    #endregion

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

        _layerMask = 1 << gameObject.layer;

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

                if (Physics.Raycast(origin, direction, out hit, 2, _layerMask))
                {
                    if (hit.collider == _meshCollider)
                    {
                        Vector3 dirLocal =
                            transform.InverseTransformDirection(direction);

                        SetComputeShaderParameter(hit, dirLocal);
                        RunComputeShader();
                    }
                }

                _lightCookie.Release();

                ToCubemap(_causticsTexture, ref _lightCookie);
                //ToCubemap(_causticsTexture, ref _testCookie);

                _causticsLight.cookie = _lightCookie;

                Color lightColor = light.color;
                lightColor *= transform.parent.gameObject.GetComponent<Renderer>().material.color;
                float absorbDistance = transform.parent.gameObject
                    .GetComponent<Renderer>().material
                    .GetFloat("_ATDistance");
                lightColor *= transform.parent.gameObject
                    .GetComponent<Renderer>().material
                    .GetColor("_TransmittanceColor");
                //_causticsLight.color = lightColor;
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Set up the compute shader variables.
    /// </summary>
    /// <param name="hit">The hit info.</param>
    /// <param name="lightDirection">The light direction.</param>
    private void SetComputeShaderParameter(RaycastHit hit, Vector3 lightDirection)
    {
        _sampleIndex[0] = _triangleIndex[hit.triangleIndex, 0];
        _sampleIndex[1] = _triangleIndex[hit.triangleIndex, 1];
        _sampleIndex[2] = _triangleIndex[hit.triangleIndex, 2];

        _sampleWeight = hit.barycentricCoordinate;

        _sampleMatrix[_sampleIndex[0]] = GetRotationMatrix(lightDirection, _sampleIndex[0]);
        _sampleMatrix[_sampleIndex[1]] = GetRotationMatrix(lightDirection, _sampleIndex[1]);
        _sampleMatrix[_sampleIndex[2]] = GetRotationMatrix(lightDirection, _sampleIndex[2]);
    }

    /// <summary>
    /// Run the compute shader.
    /// </summary>
    private void RunComputeShader()
    {
        _computeShader.SetInts("sampleIndex", _sampleIndex);
        _computeShader.SetVector("sampleWeight", _sampleWeight);
        _computeShader.SetMatrixArray("sampleMatrix", _sampleMatrix);

        _computeShader.Dispatch(_kernelId, 32, 32, 1);
    }

    /// <summary>
    /// The rotation matrix align vector A to vector B.
    /// https://math.stackexchange.com/questions/180418/calculate-rotation-matrix-to-align-vector-a-to-vector-b-in-3d
    /// </summary>
    /// <param name="lightDirection"></param>
    /// <param name="vertexIndex"></param>
    /// <returns></returns>
    private Matrix4x4 GetRotationMatrix(Vector3 lightDirection, int vertexIndex)
    {
        //Vector3 B = -_mesh.vertices[vertexIndex].normalized;
        //B = new Vector3(B.x, B.y, -B.z);
        Vector3 B = -_mesh.vertices[vertexIndex].normalized;

        float sin = Vector3.Cross(lightDirection, B).magnitude;
        float cos = Vector3.Dot(lightDirection, B);

        Matrix4x4 G = new Matrix4x4();
        G[0, 0] = cos;
        G[0, 1] = -sin;
        G[1, 0] = sin;
        G[1, 1] = cos;
        G[2, 2] = 1;

        Vector3 u = lightDirection;
        Vector3 v = (B - cos * lightDirection).normalized;
        Vector3 w = Vector3.Cross(B, lightDirection);

        Matrix4x4 F = new Matrix4x4();
        F.SetRow(0, u);
        F.SetRow(1, v);
        F.SetRow(2, w);
        F[3, 3] = 1;

        Matrix4x4 U = F.inverse * G * F;

        return U;
    }

    #endregion

    #region Initialize Functions

    /// <summary>
    /// Calculate the vertex index mapping of the mesh of the collider.
    /// </summary>
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

        for (int i = 0; i < 12; ++i)
        {
            Debug.Log($"{i}: ({_mesh.vertices[i].x / 2}, {-_mesh.vertices[i].z / 2}, {_mesh.vertices[i].y / 2})");
        }
    }

    /// <summary>
    /// Initialize the cookie textures and render textures.
    /// </summary>
    private void InitTextures()
    {
        // initialize the caustics light cookie
        _lightCookie = new RenderTexture(256, 256, 16);
        _lightCookie.dimension = TextureDimension.Cube;
        _lightCookie.hideFlags = HideFlags.HideAndDontSave;
        _lightCookie.useMipMap = true;
        _lightCookie.filterMode = FilterMode.Bilinear;

        // initialize the caustics light cookie texture2d array
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
            // convert the cubemap to texture2d array render texture
            _causticsRenderTextures[i] = ToRenderTexture(cookieCubemap[i]);
        }

        for (int i = 0; i < 6; ++i)
        {
            //Graphics.CopyTexture(tex2d2, 0, 0, _causticsRenderTextures[1], i, 0);
        }
    }

    /// <summary>
    /// Set up the compute shader parameters.
    /// </summary>
    private void InitComputeShader()
    {
        _computeShader = Instantiate(Resources.Load<ComputeShader>("Shaders/TextureInterpolate"));
        _kernelId = _computeShader.FindKernel("TextureInterpolate");

        _sampleIndex = new int[3];
        _sampleWeight = new Vector3();
        _sampleMatrix = new Matrix4x4[12];

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
        if (cubemapRenderTexture != null)
        {
            for (int i = 0; i < 6; ++i)
            {
                Graphics.CopyTexture(renderTexture, i, 0, cubemapRenderTexture,
                    i, 0);
            }
        }
    }

    #endregion
}
