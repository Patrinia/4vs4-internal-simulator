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
    // 외부에서는 인터페이스(IFileLogger) 타입으로만 접근하도록 제한하여 캡슐화 달성
    public static IFileLogger Instance { get; private set; }

    private string baseFolderPath;
    private string currentTimeStamp;

    // 활성화된 스트림을 계속 쥐고 있지 않도록, 완성된 '절대 경로'만 캐싱하는 장부
    private Dictionary<string, string> filePathCache = new Dictionary<string, string>();

    private void Awake()
    {
        // 싱글톤 패턴 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // 씬 전환 시에도 파괴되지 않고 유지

        InitializeDirectory();
    }

    /// <summary>
    /// 단 한 번, 오늘 날짜의 폴더를 생성하고 이번 시뮬레이션의 고유 타임스탬프를 고정합니다.
    /// </summary>
    private void InitializeDirectory()
    {
        string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../SimulationLogs"));
        string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
        baseFolderPath = Path.Combine(rootPath, dateFolder);

        if (!Directory.Exists(baseFolderPath))
        {
            Directory.CreateDirectory(baseFolderPath);
        }

        // 이번 시뮬레이션 세션 동안 모든 파일이 공유할 초 단위 고유 시간
        currentTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    // ========================================================================
    // [IFileLogger 구현부]
    // ========================================================================

    public void InitializeStream(string fileNameSuffix, string initialContent)
    {
        if (filePathCache.ContainsKey(fileNameSuffix)) return;

        string extension = Path.GetExtension(fileNameSuffix);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileNameSuffix);
        string finalFileName = $"{nameWithoutExt}_{currentTimeStamp}{extension}";
        string fullPath = Path.Combine(baseFolderPath, finalFileName);

        filePathCache.Add(fileNameSuffix, fullPath);

        // using 블록을 사용하여 파일을 열고(Create) 기록한 뒤, 즉시 스트림을 닫아 권한을 반환합니다.
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
            // 파일을 이어쓰기(Append) 모드로 열어 단 1줄(1회 전투 분량)을 쓰고 즉각 권한을 반환합니다.
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

    public void CloseStream(string fileNameSuffix, string closingContent = "")
    {
        if (filePathCache.TryGetValue(fileNameSuffix, out string fullPath))
        {
            if (!string.IsNullOrEmpty(closingContent))
            {
                using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter writer = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine(closingContent);
                }
            }

            // 파일 캐시 삭제
            filePathCache.Remove(fileNameSuffix);
        }
    }

    private void OnDestroy()
    {
        // 유니티 종료 시 열려있는 스트림이 없으므로, 메모리(캐시) 장부만 비워줍니다.
        filePathCache.Clear();
    }
}