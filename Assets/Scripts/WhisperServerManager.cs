using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

public class WhisperServerManager : MonoBehaviour
{
    public string serverPath;
    public string serverUrl = "http://127.0.0.1:8000/docs";

    private Process serverProcess;

    void Awake()
    {
        serverPath = System.IO.Path.Combine(Application.streamingAssetsPath, "server.exe");
    }

    IEnumerator Start()
    {
        StartServer();
        yield return StartCoroutine(WaitForServer());

        UnityEngine.Debug.Log("Whisper server is ready!");
    }

    void StartServer()
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

            UnityEngine.Debug.Log("Waiting for server...");
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnApplicationQuit()
    {
        if (serverProcess != null && !serverProcess.HasExited)
        {
            serverProcess.Kill();
            UnityEngine.Debug.Log("Server stopped");
        }
    }
}