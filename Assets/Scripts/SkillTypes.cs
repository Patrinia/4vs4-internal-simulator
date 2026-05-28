// ====================================================
// [SkillTypes.cs]
// 오직 스킬(SkillData)과 관련된 열거형 및 구조체 모음입니다.
// ====================================================

/// <summary>
/// 스킬 발동 시 타겟을 어떻게 지정할 것인지에 대한 규칙입니다.
/// </summary>
public enum TargetType
{
    None = 0,
    SingleEnemy = 1,  // 적 1명 지정
    AllEnemies = 2,   // 적 전체
    SingleAlly = 3,   // 아군 1명 지정 (보통 자신 제외)
    AllAllies = 4,    // 아군 전체
    Self = 5          // 오직 자신에게만 시전
}

/// <summary>
/// 스킬 적중 시, 대상의 특수 속성(음/양 등)을 얼마나 깎거나 올릴지 정의합니다.
/// </summary>
[System.Serializable]
public struct AttributeModifier
{
    public AttributeType type;  // 변동시킬 속성의 종류
    public int amount;          // 변동 수치 (+면 증가, -면 감소)
}