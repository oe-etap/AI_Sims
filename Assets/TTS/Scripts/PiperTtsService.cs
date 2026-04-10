using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public sealed class PiperTtsService : MonoBehaviour
{
    [Header("Model + config")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private ESpeakTokenizer tokenizer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private BackendType backend = BackendType.CPU;

    [Header("Prosody-like pauses")]
    [SerializeField, Range(0f, 1f)] private float commaDelay = 0.10f;
    [SerializeField, Range(0f, 1f)] private float periodDelay = 0.50f;
    [SerializeField, Range(0f, 1f)] private float questionExclamationDelay = 0.60f;

    [Header("Optional")]
    [SerializeField] private bool warmupOnStart = true;
    [SerializeField] private int speakerId = 0;

    private Worker worker;
    private bool isReady;
    private bool hasSidInput;

    public bool IsReady => isReady;

    private IEnumerator Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        yield return Initialize();
    }


    public bool InitializationCompleted => initializationCompleted;

    private bool initializationCompleted;

    private void InitializeWindows()
    {
        initializationCompleted = false;

        string espeakDataPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");

        int initResult = ESpeakNG.espeak_Initialize(0, 0, espeakDataPath, 0);
        if (initResult <= 0)
        {
            Debug.LogError($"[PiperTtsService] espeak init failed: {initResult}");
            initializationCompleted = true;
            return;
        }

        int voiceResult = ESpeakNG.espeak_SetVoiceByName(tokenizer.Voice);
        if (voiceResult != 0)
        {
            Debug.LogError($"[PiperTtsService] espeak voice set failed: {voiceResult}");
            initializationCompleted = true;
            return;
        }

        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, backend);
        hasSidInput = model.inputs.Any(i => i.name == "sid");

        if (warmupOnStart)
            Warmup();

        isReady = true;
        initializationCompleted = true;
    }

    public AudioClip SynthesizeToClip(string text)
    {
        if (!isReady)
        {
            Debug.LogError("[PiperTtsService] Not ready yet.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(text))
            return null;

        return SynthesizeClip(text.Trim());
    }

    private IEnumerator Initialize()
    {
        if (modelAsset == null)
        {
            Debug.LogError("[PiperTtsService] ModelAsset nincs beállítva.");
            yield break;
        }

        if (tokenizer == null)
        {
            Debug.LogError("[PiperTtsService] ESpeakTokenizer nincs beállítva.");
            yield break;
        }

        string espeakDataPath = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        espeakDataPath = Path.Combine(Application.persistentDataPath, "espeak-ng-data");

        if (!Directory.Exists(espeakDataPath))
        {
            string zipSourcePath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data.zip");
            string zipDestPath   = Path.Combine(Application.persistentDataPath, "espeak-ng-data.zip");

            using UnityWebRequest www = UnityWebRequest.Get(zipSourcePath);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PiperTtsService] Nem sikerült betölteni az espeak-ng-data.zip fájlt: {www.error}");
                yield break;
            }

            File.WriteAllBytes(zipDestPath, www.downloadHandler.data);

            try
            {
                ZipFile.ExtractToDirectory(zipDestPath, Application.persistentDataPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PiperTtsService] Zip kicsomagolási hiba: {e.Message}");
                yield break;
            }
            finally
            {
                if (File.Exists(zipDestPath))
                    File.Delete(zipDestPath);
            }
        }
#else
        espeakDataPath = Path.Combine(Application.streamingAssetsPath, "espeak-ng-data");
        yield return null;
#endif

        int initResult = ESpeakNG.espeak_Initialize(0, 0, espeakDataPath, 0);
        if (initResult <= 0)
        {
            Debug.LogError($"[PiperTtsService] eSpeak init hiba: {initResult}");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(tokenizer.Voice))
        {
            Debug.LogError("[PiperTtsService] Tokenizer.Voice üres.");
            yield break;
        }

        int voiceResult = ESpeakNG.espeak_SetVoiceByName(tokenizer.Voice);
        if (voiceResult != 0)
        {
            Debug.LogError($"[PiperTtsService] eSpeak voice beállítási hiba: {voiceResult}");
            yield break;
        }

        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, backend);
        hasSidInput = model.inputs.Any(x => x.name == "sid");

        if (warmupOnStart)
            Warmup();

        isReady = true;
        initializationCompleted = true;
        Debug.Log($"[PiperTtsService] Ready. Model={modelAsset.name}, Voice={tokenizer.Voice}");
    }

    public void Speak(string text)
    {
        if (!isReady)
        {
            Debug.LogWarning("[PiperTtsService] Még nem áll készen.");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
            return;

        StartCoroutine(SpeakCoroutine(text));
    }

    private IEnumerator SpeakCoroutine(string text)
    {
        const string delayPunctuationPattern = @"([,.?!;:])";
        const string nonDelayPunctuationPattern = @"[^\w\s,.?!;:]";

        string[] parts = Regex.Split(text, delayPunctuationPattern);

        foreach (string part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            bool isDelayToken = Regex.IsMatch(part, "^" + delayPunctuationPattern + "$");
            if (isDelayToken)
            {
                float delay = part switch
                {
                    "," or ";" or ":" => commaDelay,
                    "." => periodDelay,
                    "?" or "!" => questionExclamationDelay,
                    _ => 0f
                };

                if (delay > 0f)
                    yield return new WaitForSeconds(delay);

                continue;
            }

            string cleaned = Regex.Replace(part, nonDelayPunctuationPattern, " ").Trim();
            if (string.IsNullOrEmpty(cleaned))
                continue;

            AudioClip clip = SynthesizeClip(cleaned);
            if (clip == null)
                continue;

            //audioSource.PlayOneShot(clip);
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    private AudioClip SynthesizeClip(string textChunk)
    {
        string phonemeString = Phonemize(textChunk);
        if (string.IsNullOrWhiteSpace(phonemeString))
            return null;

        string[] phonemes = phonemeString.Trim().Select(c => c.ToString()).ToArray();
        int[] tokenIds = tokenizer.Tokenize(phonemes);
        float[] scales = tokenizer.GetInferenceParams();

        if (tokenIds == null || tokenIds.Length == 0 || scales == null || scales.Length != 3)
        {
            Debug.LogError("[PiperTtsService] Érvénytelen tokenizer kimenet.");
            return null;
        }

        int[] inputLength = { tokenIds.Length };

        using var phonemeTensor = new Tensor<int>(new TensorShape(1, tokenIds.Length), tokenIds);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        worker.SetInput("input", phonemeTensor);
        worker.SetInput("input_lengths", lengthTensor);
        worker.SetInput("scales", scalesTensor);

        if (hasSidInput)
        {
            using var sidTensor = new Tensor<int>(new TensorShape(1), new[] { speakerId });
            worker.SetInput("sid", sidTensor);
            worker.Schedule();
        }
        else
        {
            worker.Schedule();
        }

        using var outputTensor = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
        float[] audioData = outputTensor.DownloadToArray();

        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("[PiperTtsService] Üres audio kimenet.");
            return null;
        }

        int sampleRate = tokenizer.SampleRate;
        AudioClip clip = AudioClip.Create("PiperSpeech", audioData.Length, 1, sampleRate, false);
        clip.SetData(audioData, 0);
        return clip;
    }

    private void Warmup()
    {
        string phonemeString = Phonemize("hello");
        if (string.IsNullOrWhiteSpace(phonemeString))
            return;

        string[] phonemes = phonemeString.Trim().Select(c => c.ToString()).ToArray();
        int[] tokenIds = tokenizer.Tokenize(phonemes);
        float[] scales = tokenizer.GetInferenceParams();
        int[] inputLength = { tokenIds.Length };

        using var phonemeTensor = new Tensor<int>(new TensorShape(1, tokenIds.Length), tokenIds);
        using var lengthTensor = new Tensor<int>(new TensorShape(1), inputLength);
        using var scalesTensor = new Tensor<float>(new TensorShape(3), scales);

        worker.SetInput("input", phonemeTensor);
        worker.SetInput("input_lengths", lengthTensor);
        worker.SetInput("scales", scalesTensor);

        if (hasSidInput)
        {
            using var sidTensor = new Tensor<int>(new TensorShape(1), new[] { speakerId });
            worker.SetInput("sid", sidTensor);
            worker.Schedule();
        }
        else
        {
            worker.Schedule();
        }

        using var outputTensor = (worker.PeekOutput() as Tensor<float>).ReadbackAndClone();
        Debug.Log($"[PiperTtsService] Warmup output length = {outputTensor.shape[0]}");
    }

    private string Phonemize(string text)
    {
        IntPtr textPtr = IntPtr.Zero;

        try
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text + "\0");
            textPtr = Marshal.AllocHGlobal(textBytes.Length);
            Marshal.Copy(textBytes, 0, textPtr, textBytes.Length);

            IntPtr pointerToText = textPtr;
            int textMode = 0;
            int phonemeMode = 2; // UTF-8 IPA

            IntPtr resultPtr = ESpeakNG.espeak_TextToPhonemes(ref pointerToText, textMode, phonemeMode);
            return resultPtr != IntPtr.Zero ? PtrToUtf8String(resultPtr) : null;
        }
        finally
        {
            if (textPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(textPtr);
        }
    }

    private static string PtrToUtf8String(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        using var ms = new MemoryStream();
        for (int offset = 0; ; offset++)
        {
            byte b = Marshal.ReadByte(ptr, offset);
            if (b == 0)
                break;

            ms.WriteByte(b);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private void OnDestroy()
    {
        worker?.Dispose();
        ESpeakNG.espeak_Terminate();
    }

    public void SetVoice(ModelAsset newModel, ESpeakTokenizer newTokenizer)
    {
        if (newModel == null || newTokenizer == null)
        {
            Debug.LogError("[PiperTtsService] SetVoice: null model or tokenizer!");
            return;
        }

        StopAllCoroutines();

        worker?.Dispose();
        worker = null;

        isReady = false;
        initializationCompleted = false;

        modelAsset = newModel;
        tokenizer = newTokenizer;

        Debug.Log($"[PiperTtsService] Switching voice to: {modelAsset.name}");

        StartCoroutine(Initialize());
    }
}