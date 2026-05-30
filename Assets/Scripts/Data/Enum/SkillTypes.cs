// ====================================================
// [SkillTypes.cs]
// 오직 스킬(SkillData)과 관련된 열거형 및 구조체 모음입니다.
// ====================================================

/// <summary>
/// 스킬 발동 시 대상의 진영을 어떻게 지정할 것인지에 대한 규칙입니다.
/// </summary>
public enum TargetType
{
    None = 0,
    Enemy = 1,   // 적군 진영 타겟
    Ally = 2,    // 아군 진영 타겟
    Self = 3     // 오직 자신에게만 시전
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

//스킬의 종류
//이후 패시브 스킬도 추가 가능
public enum SkillCategory
{
    Normal = 0,    // 일반 스킬
    Ultimate = 1   // 필살기 (장착 1개 제한)
}