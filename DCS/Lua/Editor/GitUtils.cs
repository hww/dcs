using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;
#endif

/// <summary>
/// Локальная утилита для автоматического запекания и получения Git Tag и хэша коммита.
/// </summary>
public class GitUtils
#if UNITY_EDITOR
    : IPreprocessBuildWithReport
#endif
{
    private const string ResourceFileName = "git_hash";
    private const string FallbackHash = "Local-Editor-Build";

#if UNITY_EDITOR
    public int callbackOrder => 0;

    /// <summary>
    /// Автоматический вызов перед сборкой билда.
    /// </summary>
    public void OnPreprocessBuild(BuildReport report)
    {
        UpdateBakedGitHash();
    }

    /// <summary>
    /// Обновление вручную: Assets -> Git -> Update Git Tag and Hash
    /// </summary>
    [MenuItem("Assets/Git/Update Git Tag and Hash")]
    public static void UpdateBakedGitHash()
    {
        string gitInfo = GetLocalGitInfo();
        SaveHashToResources(gitInfo);
    }

    // Вызывает консоль и запрашивает Tag + Hash
    private static string GetLocalGitInfo()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                // Опция --tags находит ближайший тег. 
                // Опция --always гарантирует, что если тегов нет, вернется хотя бы хэш.
                Arguments = "describe --tags --always --long",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(result) && !result.Contains("fatal"))
                    {
                        return result; // Вернет что-то вроде: v1.0.2-0-g1a2b3c4
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GitUtils] Не удалось получить Git данные: {ex.Message}");
        }

        return "No-Git-Data";
    }

    private static void SaveHashToResources(string data)
    {
        string resourcesDirPath = Path.Combine(Application.dataPath, "Resources");
        string filePath = Path.Combine(resourcesDirPath, ResourceFileName + ".txt");

        if (!Directory.Exists(resourcesDirPath))
        {
            Directory.CreateDirectory(resourcesDirPath);
        }

        File.WriteAllText(filePath, data);
        AssetDatabase.Refresh();
        Debug.Log($"[GitUtils] Git данные '{data}' успешно сохранены в Resources.");
    }
#endif

    /// <summary>
    /// Вызовите этот метод в любом месте вашей игры, чтобы получить Tag и хэш.
    /// </summary>
    public static string GetCommitHash()
    {
        TextAsset hashAsset = Resources.Load<TextAsset>(ResourceFileName);

        if (hashAsset != null && !string.IsNullOrEmpty(hashAsset.text))
        {
            return hashAsset.text.Trim();
        }

        return FallbackHash;
    }
}
