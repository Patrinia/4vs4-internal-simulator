using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ========================================================================
// [SimulationLogManager.cs] 
// 파일 입출력(Directory, FileStream, StreamWriter)을 중앙 통제하는 전담 매니저입니다.
// 내부 개발 및 파이썬 분석 편의성을 위해 프로젝트(Assets) 최상단에 로그를 저장합니다.
// ========================================================================
public class SimulationLogManager : MonoBehaviour, IFileLogger
{
    private static SimulationLogManager _instance;

    public static IFileLogger Instance => _instance;

    private string rootPath;
    private string dateFolder;
    private string currentTimeStamp;

    private Dictionary<string, string> filePathCache = new Dictionary<string, string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);

        InitializeDirectory();
    }

    private void InitializeDirectory()
    { 
        // 파이썬 분석과 기획팀의 데이터 접근 편의성을 최우선으로 하여, 
        // 유니티 프로젝트 폴더(Assets)와 나란히 위치한 'SimulationLogs' 폴더를 사용합니다.
        rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../SimulationLogs"));

        dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
        currentTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    // ========================================================================
    // [IFileLogger 구현부]
    // ========================================================================

    public void InitializeStream(string fileNameSuffix, string initialContent)
    {
        if (filePathCache.ContainsKey(fileNameSuffix)) return;

        string subDir = Path.GetDirectoryName(fileNameSuffix);
        string fileName = Path.GetFileName(fileNameSuffix);

        string extension = Path.GetExtension(fileName);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        string finalFileName = $"{nameWithoutExt}_{currentTimeStamp}{extension}";

        string targetFolderPath = Path.Combine(rootPath, subDir, dateFolder);
        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);
        }

        string fullPath = Path.Combine(targetFolderPath, finalFileName);
        filePathCache.Add(fileNameSuffix, fullPath);

        using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        using (StreamWriter writer = new StreamWriter(fs, System.Text.Encoding.UTF8))
        {
            if (!string.IsNullOrEmpty(initialContent))
            {
                writer.WriteLine(initialContent);
            }
        }
    }

    public void WriteRecord(string fileNameSuffix, string record)
    {
        if (filePathCache.TryGetValue(fileNameSuffix, out string fullPath))
        {
            using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter writer = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(record);
            }
        }
        else
        {
            Debug.LogWarning($"[SimulationLogManager] {fileNameSuffix} 스트림이 초기화되지 않았습니다.");
        }
    }

    public void CloseStream(string fileNameSuffix, string Content = "")
    {
        if (filePathCache.TryGetValue(fileNameSuffix, out string fullPath))
        {
            if (!string.IsNullOrEmpty(Content))
            {
                using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter writer = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(Content);
                }
            }
            filePathCache.Remove(fileNameSuffix);
        }
    }

    private void OnDestroy()
    {
        filePathCache.Clear();
        if (_instance == this) _instance = null;
    }
}