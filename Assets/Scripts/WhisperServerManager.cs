using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WhisperServerManager : MonoBehaviour
{
    public string serverPath;
    public string serverUrl = "http://127.0.0.1:8000/docs";

    private Process serverProcess;

    void Awake()
    {
        serverPath = System.IO.Path.Combine(Application.streamingAssetsPath, "server.exe");
#if UNITY_EDITOR
    EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
    }

#if UNITY_EDITOR
void OnPlayModeChanged(PlayModeStateChange state)
{
    if (state == PlayModeStateChange.ExitingPlayMode)
    {
        StopServer();
    }
}
#endif

    IEnumerator Start()
    {
        StartServer();
        yield return StartCoroutine(WaitForServer());

        UnityEngine.Debug.Log("Whisper server is ready!");
    }

    void StartServer()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = serverPath,
                CreateNoWindow = false,
                UseShellExecute = true
            };

            serverProcess = Process.Start(startInfo);

            UnityEngine.Debug.Log("Server starting...");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log("Server already running or failed to start: " + e.Message);
        }
    }

    IEnumerator WaitForServer()
    {
        while (true)
        {
            UnityWebRequest request = UnityWebRequest.Get(serverUrl);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    void StopServer()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("server"))
            {
                proc.Kill();
            }

            UnityEngine.Debug.Log("Server stopped");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Failed to stop server: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }
}