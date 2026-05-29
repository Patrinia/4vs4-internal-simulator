using UnityEngine;

// ==========================================
// [개방-폐쇄 원칙 (OCP) 준수]
// 복잡한 스킬의 특수 효과를 모듈화하기 위한 추상 클래스입니다.
// 새로운 상태이상이 필요하면 이를 상속받는 SO를 새로 만들면 됩니다.
// ==========================================
public abstract class SkillEffect : ScriptableObject
{
    [Header("효과 식별 데이터")]
    public string effectID;      // 효과 고유 ID
    public string effectName;    // 효과 이름

    // 실제 전투 정산 시 CombatResolver 등에 의해 실행될 추상 메서드
    public abstract void ApplyEffect(UnitControl caster, UnitControl target);
}