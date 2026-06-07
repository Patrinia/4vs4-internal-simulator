// ========================================================================
// [IFileLogger.cs] (DIP 준수를 위한 추상화)
// 수집가(Exporter)들이 구상 클래스에 직접 의존하지 않도록 보장하는 인터페이스입니다.
// ========================================================================
public interface IFileLogger
{
    /// <summary>스트림을 열고 파일명에 타임스탬프를 부여하며, 초기 내용(헤더 등)을 기록합니다.</summary>
    void InitializeStream(string fileNameSuffix, string initialContent);

    /// <summary>해당 스트림에 문자열을 한 줄 기록하고 즉시 파일에 플러시(Flush)합니다.</summary>
    void WriteRecord(string fileNameSuffix, string record);

    /// <summary>파일 스트림을 닫습니다. (JSON의 경우 닫기 전 괄호 등 추가 가능)</summary>
    void CloseStream(string fileNameSuffix, string closingContent = "");
}