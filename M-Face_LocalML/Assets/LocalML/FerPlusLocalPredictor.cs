using System;
using System.IO;
using System.Threading.Tasks;
using NatML;
using NatML.Features;
using NatML.Types;
using UnityEngine;

/// <summary>
/// FER+ ONNX/TFLite 로컬 추론 (NatML MLEdgeModel).
/// Android: .tflite, Windows/Editor: .onnx
/// </summary>
public sealed class FerPlusLocalPredictor : IDisposable
{
    readonly MLEdgeModel model;
    readonly MLArrayType inputArrayType;

    FerPlusLocalPredictor(MLEdgeModel model)
    {
        this.model = model;
        inputArrayType = model.inputs[0] as MLArrayType;
        if (inputArrayType == null)
            throw new InvalidOperationException("FER+ model expects array input");
    }

    public static async Task<FerPlusLocalPredictor> CreateFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new FileNotFoundException("FER+ model not found at " + path);
        var edge = await MLEdgeModel.Create(path);
        return new FerPlusLocalPredictor(edge);
    }

    public static async Task<FerPlusLocalPredictor> CreateFromResources()
    {
        return await CreateFromPath(ResolveModelPath());
    }

    public static string ResolveModelPath()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return MFaceModelBootstrap.CachedModelPath;
#else
        return Path.Combine(Application.dataPath, "MLModels", "emotion-ferplus-8.onnx");
#endif
    }

    /// <summary>64x64 grayscale pixels, values 0~255.</summary>
    public float[] Predict(float[] gray64x64)
    {
        if (gray64x64 == null || gray64x64.Length != 64 * 64)
            throw new ArgumentException("FER+ requires 64x64 grayscale input");

        var input = new MLArrayFeature<float>(gray64x64, inputArrayType.shape);
        using var edgeInput = ((IMLEdgeFeature)input).Create(inputArrayType);
        using var outputs = model.Predict(edgeInput);
        var logits = new MLArrayFeature<float>(outputs[0]);
        int count = Mathf.Min(8, logits.elementCount);
        var result = new float[count];
        for (int i = 0; i < count; i++)
            result[i] = logits[i];
        return result;
    }

    public void Dispose() => model.Dispose();
}
