using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Renderer))]
[ExecuteInEditMode]
public class PerlinNoise : MonoBehaviour {

    [Range(0, 8)]
    public int octaves = 4;

    [Range(0f,8f)]
    public float lacunarity = 1f;

    [Range(0f, 8f)]
    public float gain = 1f;

    [Range(1f, 20f)]
    public float timeMultiplier = 1f;

    public Gradient color;
    private Gradient oldGradient;

    // Permutation table for persistant noise calculations
    private static readonly int[] noisePermutation = { 151,160,137,91,90,15,
                    131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
                    190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
                    88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
                    77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
                    102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
                    135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
                    5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
                    223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
                    129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
                    251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
                    49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
                    138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
                    };

    // gradients for 4D noise
    private static readonly float[] g4 = {
        0, -1, -1, -1,
        0, -1, -1, 1,
        0, -1, 1, -1,
        0, -1, 1, 1,
        0, 1, -1, -1,
        0, 1, -1, 1,
        0, 1, 1, -1,
        0, 1, 1, 1,
        -1, -1, 0, -1,
        -1, 1, 0, -1,
        1, -1, 0, -1,
        1, 1, 0, -1,
        -1, -1, 0, 1,
        -1, 1, 0, 1,
        1, -1, 0, 1,
        1, 1, 0, 1,

        -1, 0, -1, -1,
        1, 0, -1, -1,
        -1, 0, -1, 1,
        1, 0, -1, 1,
        -1, 0, 1, -1,
        1, 0, 1, -1,
        -1, 0, 1, 1,
        1, 0, 1, 1,
        0, -1, -1, 0,
        0, -1, -1, 0,
        0, -1, 1, 0,
        0, -1, 1, 0,
        0, 1, -1, 0,
        0, 1, -1, 0,
        0, 1, 1, 0,
        0, 1, 1, 0,
    };

    private Renderer m_Renderer;
    private Renderer renderer_
    {
        get
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<Renderer>();
            return m_Renderer;
        }
    }

    private Shader m_OriginalShader;
    private Shader m_Shader;
    private Shader shader
    {
        get
        {
            if(m_Shader == null)
                m_Shader = Shader.Find("Noise/PerlinNoise");
            return m_Shader;
        }
    }

    private Material m_Material;
    private Material material
    {
        get
        {
            return renderer_.sharedMaterial;
        }
    }

    private Texture2D m_PermutationTexture;
    private Texture2D permutationTexture
    {
        get
        {
            if (m_PermutationTexture == null)
            {
                m_PermutationTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point,
                };
                Color[] permutedValues = new Color[noisePermutation.Length];
                for (int i = 0; i < permutedValues.Length; ++i)
                {
                    permutedValues[i] = new Color(noisePermutation[i] / 255f, 0, 0, 0);
                }
                m_PermutationTexture.SetPixels(permutedValues);
                m_PermutationTexture.Apply();
            }
            return m_PermutationTexture;
        }
    }

    private Texture2D m_Gradient4Table;
    private Texture2D gradient4Table
    {
        get
        {
            if (m_Gradient4Table == null)
            {
                m_Gradient4Table = new Texture2D(32, 1, TextureFormat.ARGB32, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point,
                };
                int entries = g4.Length / 4;
                Color[] gradientValues = new Color[entries];
                for (int i = 0; i < entries; ++i)
                {
                    int j = i * 4;
                    gradientValues[i] = new Color(g4[j], g4[j+1], g4[j+2], g4[j+3]);
                }
                m_Gradient4Table.SetPixels(gradientValues);
                m_Gradient4Table.Apply();
            }
            return m_Gradient4Table;
        }
    }

    private Texture2D m_ColorTexture;
    private Texture2D colorTexture
    {
        get
        {
            if (m_ColorTexture == null)
            {
                m_ColorTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false)
                {
                    name = "Noise color texture.",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                };
            }
            return m_ColorTexture;
        }
    }

    void OnEnable()
    {
        m_OriginalShader = material.shader;
        material.shader = shader;
        Update();
    }

    void OnDisable()
    {
        if (m_PermutationTexture != null)
            DestroyImmediate(m_PermutationTexture);

        if (m_Gradient4Table != null)
            DestroyImmediate(m_Gradient4Table);

        m_PermutationTexture = null;
        m_Gradient4Table = null;

        material.shader = m_OriginalShader;
    }

    void Update()
    {
        material.SetTexture("_ColorTexture", colorTexture);

        material.SetTexture("_PermutationTable", permutationTexture);
        material.SetTexture("_Gradient4Table", gradient4Table);

        material.SetInt("_Octaves", octaves);
        material.SetFloat("_Lacunarity", lacunarity);
        material.SetFloat("_Gain", gain);
        material.SetFloat("_TimeMultiplier", timeMultiplier);
    }

    void BakeColor()
    {
        float fWidth = colorTexture.width;
        Color[] pixels = new Color[colorTexture.width];

        for (float i = 0f; i <= 1f; i += 1f / fWidth)
        {
            Color c = color.Evaluate(i);
            pixels[(int)Mathf.Floor(i * (fWidth - 1f))] = c;
        }

        colorTexture.SetPixels(pixels);
        colorTexture.Apply();
    }

    void Start()
    {
        BakeColor();
    }

}
