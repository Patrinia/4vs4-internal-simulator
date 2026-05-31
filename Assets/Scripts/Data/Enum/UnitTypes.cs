// ====================================================
// [UnitTypes.cs]
// 오직 유닛(UnitData, UnitControl)과 관련된 구조체 모음입니다.
// ====================================================

/// <summary>
/// 유닛이 태어날 때 가지는 [특수 속성]과 그 [시작 수치]의 묶음입니다.
/// 엑셀에서 UnitData를 파싱할 때 사용됩니다.
/// </summary>
[System.Serializable]
public struct UnitAttribute
{
    public AttributeType type; // 어떤 속성인지 (예: YinYang)
    public int baseValue;      // 전투 시작 시 초기화될 수치 (예: 50)
}

/// <summary>
/// 기획자가 엑셀에서 지정할 유닛의 두뇌(지능) 타입입니다.
/// </summary>
public enum AIBrainType
{
    None = 0,
    Player = 1,      // 플레이어 조작 (HumanBrain - UI 입력 대기)
    Random = 2,      // 무작위 행동 (RandomActionBrain)
    Sequence = 3,    // 순차적 행동 (SequenceActionBrain)
    Strategic = 4,   // 전황 분석 행동 (StrategicActionBrain - 보스급 지능)
    MLAgent = 5      // E단계에서 적용될 머신러닝 추론 전용 뇌 예약 슬롯
}