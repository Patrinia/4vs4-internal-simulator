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