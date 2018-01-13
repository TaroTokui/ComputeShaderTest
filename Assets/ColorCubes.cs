using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class ColorCubes : MonoBehaviour
{
    // ==============================
    #region // Defines

    /// <summary>
    /// スレッドグループのスレッドサイズ
    /// </summary>
    const int ThreadBlockSize = 256;

    struct CubeData
    {
        public Vector3 BasePosition;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Albedo;
        public Vector2 Index;
    }

    #endregion // Defines

    // ==============================
    #region // Serialize Fields

    /// <summary>
    /// 最大オブジェクト数
    /// </summary>
    [SerializeField]
    int _instanceCountX = 100;
    [SerializeField]
    int _instanceCountY = 100;

    [SerializeField]
    float _step = 1.1f;

    /// <summary>
    /// ComputeShaderの参照
    /// </summary>
    [SerializeField]
    ComputeShader _ComputeShader;

    /// <summary>
    /// Mesh
    /// </summary>
    [SerializeField]
    Mesh _CubeMesh;

    /// <summary>
    /// Material
    /// </summary>
    [SerializeField]
    Material _CubeMaterial;

    /// <summary>
    /// meshのサイズ
    /// </summary>
    [SerializeField]
    Vector3 _CubeMeshScale = new Vector3(1f, 1f, 1f);

    /// <summary>
    /// 表示領域の中心座標
    /// </summary>
    [SerializeField]
    Vector3 _BoundCenter = Vector3.zero;

    /// <summary>
    /// 表示領域のサイズ
    /// </summary>
    [SerializeField]
    Vector3 _BoundSize = new Vector3(300f, 300f, 300f);

    /// <summary>
    /// アニメーション速度
    /// </summary>
    [Range(-Mathf.PI, Mathf.PI)]
    [SerializeField]
    float _Phi = Mathf.PI;

    /// <summary>
    /// アニメーション速度
    /// </summary>
    [Range(0.01f, 100)]
    [SerializeField]
    float _Lambda = 1;

    /// <summary>
    /// アニメーション速度
    /// </summary>
    [SerializeField]
    float _Amplitude = 1;

    #endregion // Serialize Fields

    // ==============================
    #region // Private Fields

    /// <summary>
    /// ドカベンロゴのバッファ
    /// </summary>
    ComputeBuffer _CubeDataBuffer;

    /// <summary>
    /// GPU Instancingの為の引数
    /// </summary>
    uint[] _GPUInstancingArgs = new uint[5] { 0, 0, 0, 0, 0 };

    /// <summary>
    /// GPU Instancingの為の引数バッファ
    /// </summary>
    ComputeBuffer _GPUInstancingArgsBuffer;

    int _instanceCount;

    #endregion // Private Fields


    // --------------------------------------------------
    #region // MonoBehaviour Methods

    void Awake()
    {
        Application.targetFrameRate = 90;
    }
    
    void Start()
    {
        _instanceCount = _instanceCountX * _instanceCountY;

        // バッファ生成
        this._CubeDataBuffer = new ComputeBuffer(this._instanceCount, Marshal.SizeOf(typeof(CubeData)));
        this._GPUInstancingArgsBuffer = new ComputeBuffer(1, this._GPUInstancingArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        var cubeDataArr = new CubeData[this._instanceCount];

        // set default position
        for (int j = 0; j < _instanceCountY; j++)
        {
            for (int i = 0; i < _instanceCountX; i++)
            {
                int id = j * _instanceCountX + i;
                cubeDataArr[id].BasePosition = new Vector3((i - _instanceCountX / 2) * _step, 0, (j - _instanceCountY / 2) * _step);
                cubeDataArr[id].Index = new Vector2(i, j);
            }
        }
        this._CubeDataBuffer.SetData(cubeDataArr);
        cubeDataArr = null;
    }
    
    void Update()
    {
        // ComputeShader
        int kernelId = this._ComputeShader.FindKernel("MainCS");
        this._ComputeShader.SetFloat("_Time", Time.time / 5.0f);
        this._ComputeShader.SetFloat("_Phi", _Phi);
        this._ComputeShader.SetFloat("_Lambda", _Lambda);
        this._ComputeShader.SetFloat("_Amplitude", _Amplitude);
        this._ComputeShader.SetBuffer(kernelId, "_CubeDataBuffer", this._CubeDataBuffer);
        this._ComputeShader.Dispatch(kernelId, (Mathf.CeilToInt(this._instanceCount / ThreadBlockSize) + 1), 1, 1);

        // GPU Instaicing
        this._GPUInstancingArgs[0] = (this._CubeMesh != null) ? this._CubeMesh.GetIndexCount(0) : 0;
        this._GPUInstancingArgs[1] = (uint)this._instanceCount;
        this._GPUInstancingArgsBuffer.SetData(this._GPUInstancingArgs);
        this._CubeMaterial.SetBuffer("_CubeDataBuffer", this._CubeDataBuffer);
        this._CubeMaterial.SetVector("_CubeMeshScale", this._CubeMeshScale);
        Graphics.DrawMeshInstancedIndirect(this._CubeMesh, 0, this._CubeMaterial, new Bounds(this._BoundCenter, this._BoundSize), this._GPUInstancingArgsBuffer);
    }
    
    void OnDestroy()
    {
        if (this._CubeDataBuffer != null)
        {
            this._CubeDataBuffer.Release();
            this._CubeDataBuffer = null;
        }
        if (this._GPUInstancingArgsBuffer != null)
        {
            this._GPUInstancingArgsBuffer.Release();
            this._GPUInstancingArgsBuffer = null;
        }
    }

    #endregion // MonoBehaviour Method
}
