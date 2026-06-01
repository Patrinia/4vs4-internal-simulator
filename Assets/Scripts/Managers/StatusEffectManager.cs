using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [StatusEffectManager.cs]
// 전투의 6단계 라이프사이클 타이밍에 맞춰 상태 이상, 쿨타임, 
// 그리고 전역 기믹의 효과 발동을 관리하는 전담 관리자입니다.
// ====================================================
public class StatusEffectManager
{
    // ========================================================================
    // [팩토리 및 데이터 적용부]
    // ========================================================================

    /// <summary>
    /// 스킬 적중 시 CombatResolver가 호출합니다. 속성 변동과 상태이상을 타겟에게 부여합니다.
    /// </summary>
    public void ApplySkillEffects(UnitControl caster, UnitControl target, SkillData skill)
    {
        // 1. 속성 조작 (게이지 증감)
        foreach (var mod in skill.attributeModifiers)
        {
            target.ModifyAttribute(mod.type, mod.amount);
            Debug.Log($"<color=cyan>[기믹] {target.unitName}의 {mod.type} 속성이 {mod.amount}만큼 변화했습니다.</color>");
        }

        // 2. 상태이상 팩토리 (정적 데이터를 동적 객체로)
        foreach (var effectData in skill.statusEffects)
        {
            // 동일한 상태이상이 이미 유닛에게 존재하고 유효한지 검사 (중첩 병합 처리)
            StatusEffectBase existingEffect = target.activeEffects.Find(e => e.type == effectData.type && !e.isExpired);

            if (existingEffect != null)
            {
                // 타입별 사칙연산 병합 룰 적용
                if (existingEffect is DurationEffect)
                {
                    existingEffect.value += effectData.value;        // 기간제: 위력(개수) 합산
                    existingEffect.duration += effectData.duration;  // 기간제: 지속시간 합산
                }
                else if (existingEffect is StackEffect)
                {
                    existingEffect.value += effectData.value;        // 스택제: 오직 위력(개수)만 합산
                }
                Debug.Log($"<color=magenta>[상태이상 중첩] {target.unitName}의 {effectData.type} 중첩 병합! (총 위력:{existingEffect.value}, 지속:{existingEffect.duration})</color>");
            }
            else
            {
                // 기존에 없는 상태이상이라면 새로 생성하여 주머니에 추가
                StatusEffectBase newEffect = CreateEffectInstance(effectData.type);
                if (newEffect != null)
                {
                    newEffect.Init(caster, target, effectData);
                    target.activeEffects.Add(newEffect);
                    Debug.Log($"<color=magenta>[상태이상] {target.unitName}에게 {effectData.type}(위력:{effectData.value}) 부여됨!</color>");
                }
            }
        }
    }

    /// <summary>
    /// EffectType에 따라 알맞은 자식 클래스 인스턴스를 생성하는 팩토리 메서드
    /// </summary>
    private StatusEffectBase CreateEffectInstance(EffectType type)
    {
        switch (type)
        {
            // (추후 구체적인 클래스들이 작성되면 이곳에 case가 추가됩니다.)
            case EffectType.AtkUp: return new DurationEffectTemplate();
            case EffectType.DefDown: return new DurationEffectTemplate();
            case EffectType.Stun: return new DurationEffectTemplate();
            case EffectType.Burn: return new StackEffectTemplate();
            case EffectType.Bleed: return new StackEffectTemplate();
            default: return null;
        }
    }

    // ========================================================================
    // [전투 연산 헬퍼]
    // ========================================================================

    public float GetAttackMultiplier(UnitControl unit)
    {
        float multiplier = 1.0f;
        foreach (var effect in unit.activeEffects)
        {
            // AtkUp은 위력(value)을 효과의 '개수'로 인지하며, 개당 10%(* 0.1f)씩 공격력을 복리 증가가 아닌 단리로 증가시킵니다.
            if (effect.type == EffectType.AtkUp && !effect.isExpired)
                multiplier += (effect.value * 0.1f);
        }
        return multiplier;
    }

    public float GetDefenseMultiplier(UnitControl unit)
    {
        float multiplier = 1.0f;
        foreach (var effect in unit.activeEffects)
        {
            // DefDown은 위력(value)을 효과의 '개수'로 인지하며, 개당 10%(* 0.1f)씩 대상이 받는 데미지 배율을 증가시킵니다.
            if (effect.type == EffectType.DefDown && !effect.isExpired)
                multiplier += (effect.value * 0.1f);
        }
        return multiplier;
    }

    /// <summary>
    /// 1단계: 전투 진입 시 최초 1회 발동
    /// </summary>
    public void OnBattleStart(List<UnitControl> allUnits)
    {
        /* 전역 기믹 초기화, 영구 패시브 효과 적용 및 유닛별 초기 세팅 */
    }

    /// <summary>
    /// 2단계: 매 라운드 시작 시 발동 (Phase 1-1)
    /// </summary>
    public void OnRoundStart(List<UnitControl> allUnits)
    {
        /* 라운드 시작 시 지속 효과 정산 및 스킬 쿨타임 감소 등 */
    }

    /// <summary>
    /// 3단계: 개별 유닛의 턴 시작 시 발동 (Phase 2-2)
    /// </summary>
    public void OnTurnStart(UnitControl unit)
    {
        /* 턴 시작 시 도트 데미지 정산, 속성 변화 디버프 처리 등 */
    }

    /// <summary>
    /// 4단계: 개별 유닛의 행동이 끝난 후 턴 종료 시 발동 (Phase 2-6)
    /// </summary>
    public void OnTurnEnd(UnitControl unit)
    {
        /* 턴 종료 시 1턴짜리 버프 수명 감소 및 상태 갱신 */
    }

    /// <summary>
    /// 5단계: 모든 유닛이 행동을 마친 후 라운드 종료 시 발동 (Phase 3-1)
    /// 침식 상태 복귀 기믹 작성 되어 있음
    /// </summary>
    public void OnRoundEnd(List<UnitControl> allUnits)
    {
        // 1. 살아있는 모든 유닛을 검사하여 침식 상태 해제 (기획 A)
        foreach (UnitControl unit in allUnits)
        {
            if (unit.isDead) continue; // 사망한 유닛은 연산에서 제외

            if (unit.currentAttributes.TryGetValue(AttributeType.YinYang, out int yyValue))
            {
                // 음기 침식(0~10) 또는 양기 침식(90~100) 상태인지 확인
                if ((yyValue >= 0 && yyValue <= 10) || (yyValue >= 90 && yyValue <= 100))
                {
                    string stateName = yyValue <= 10 ? "음기 침식" : "양기 과다";
                    Debug.Log($"<color=cyan><b>[{unit.unitName}]</b>의 {stateName} 상태가 해제되어 수치가 기준점(50)으로 복귀합니다.</color>");

                    // 수치를 완벽한 조화(50)로 강제 리셋
                    unit.currentAttributes[AttributeType.YinYang] = 50;
                }
            }
        }

        /* 기타 라운드 종료 시 전장 환경 변화 정산 등 추가 가능 */
    }

    /// <summary>
    /// 6단계: 승패가 결정되어 전투가 최종 종료될 때 발동
    /// </summary>
    public void OnBattleEnd(List<UnitControl> allUnits)
    {
        /* 전투 결과 데이터 저장 트리거 및 승리/패배 상태 정산 */
    }

    /// <summary>
    /// 리스트 순회 중 아이템 소멸(Remove) 에러를 방지하기 위해 역순으로 검사하고, 
    /// 액션(콜백)을 실행한 후 만료된 객체를 청소하는 안전한 헬퍼 함수
    /// </summary>
    private void ProcessEffects(UnitControl unit, System.Action<StatusEffectBase> action)
    {
        for (int i = unit.activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = unit.activeEffects[i];
            action?.Invoke(effect); // 생명주기 함수 실행

            if (effect.isExpired)
            {
                unit.activeEffects.RemoveAt(i);
                Debug.Log($"<color=grey>[해제] {unit.unitName}의 {effect.type} 상태가 해제되었습니다.</color>");
            }
        }
    }

}

// ========================================================================
// [상태이상 객체 템플릿]
// (임시로 생성된 클래스들이며, 추후 각자 독립된 로직을 가질 수 있습니다)
// ========================================================================
public class DurationEffectTemplate : DurationEffect { }
public class StackEffectTemplate : StackEffect { }