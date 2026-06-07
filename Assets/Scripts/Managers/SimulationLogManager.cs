using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ========================================================================
// [SimulationLogManager.cs] 
// 파일 입출력(Directory, FileStream, StreamWriter)을 중앙 통제하는 전담 매니저입니다.
// ========================================================================
public class SimulationLogManager : MonoBehaviour, IFileLogger
{
    // [업데이트] 인터페이스 캐스팅 경고(CS0252) 방지 및 유니티 생명주기 캡슐화를 위한 실체 변수 선언
    private static SimulationLogManager _instance;

    // 외부에는 IFileLogger 인터페이스 형태로만 제공 (은닉화)
    public static IFileLogger Instance => _instance;

    private string rootPath;
    private string dateFolder;
    private string currentTimeStamp;

    // 활성화된 스트림을 계속 쥐고 있지 않도록, 완성된 '절대 경로'만 캐싱하는 장부
    private Dictionary<string, string> filePathCache = new Dictionary<string, string>();

    private void Awake()
    {
        // 싱글톤 패턴 초기화 (내부 실체 변수 사용)
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject); // 씬 전환 시에도 파괴되지 않고 유지

        InitializeDirectory();
    }

    /// <summary>
    /// 단 한 번, 최상위 SimulationLogs 경로 및 이번 시뮬레이션의 고유 타임스탬프를 고정합니다.
    /// </summary>
    private void InitializeDirectory()
    {
        // [업데이트] OS 쓰기 보안(Write-Protect)을 통과하는 완벽한 영구 보관소 경로로 전면 교체
        rootPath = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "SimulationLogs"));
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

            // 파일 캐시 삭제
            filePathCache.Remove(fileNameSuffix);
        }
    }

    private void OnDestroy()
    {
        filePathCache.Clear();

        // [업데이트] 매니저 파괴 시 내부 실체 변수(싱글톤 참조) 해제 (좀비 참조 방지)
        if (_instance == this) _instance = null;
    }
}