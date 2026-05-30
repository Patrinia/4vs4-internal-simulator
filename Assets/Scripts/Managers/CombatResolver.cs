using System.Collections.Generic;
using UnityEngine;

// 스킬의 난수 굴림, 버프/디버프 연산, 그리고 최종 적용을 담당하는 전문가
// 전투 결과 정산 및 사망 처리 전문가
public class CombatResolver
{
    // ActionDecision을 통째로 받아 메인(100%)과 서브(배율)를 분리하여 연산합니다.
    public void ExecuteAction(UnitControl caster, ActionDecision decision)
    {
        if (decision == null || decision.SelectedSkill == null || decision.MainTarget == null) return;

        SkillData skill = decision.SelectedSkill;

        // 1. 위력 난수 굴림 (최소 ~ 최대 범위)
        // 주의: Random.Range(int, int)에서 최댓값은 포함되지 않으므로 +1을 해줍니다.
        int baseValue = Random.Range(skill.minPower, skill.maxPower + 1);

        // 2. 버프/디버프 연산 (기믹 파이프라인)
        // TODO: 향후 StatusEffectManager에서 현재 걸려있는 공격력/방어력 버프 수치를 가져와 곱합니다.
        float buffMultiplier = 1.0f;   // 예: 시전자의 공격력 증가
        float debuffMultiplier = 1.0f; // 예: 피격자의 방어력 증가

        // 기본 위력 확정
        int finalMainValue = Mathf.RoundToInt(baseValue * buffMultiplier * debuffMultiplier);

        // 3. 메인 타겟 데미지/회복 적용 (100%)
        ApplyEffectToTarget(caster, decision.MainTarget, skill, finalMainValue, "메인 타겟");

        // 4. 서브 타겟 데미지/회복 적용 (subTargetDamageRatio 배율 적용)
        if (decision.SubTargets.Count > 0 && skill.subTargetDamageRatio > 0f)
        {
            int finalSubValue = Mathf.RoundToInt(finalMainValue * skill.subTargetDamageRatio);
            foreach (var sub in decision.SubTargets)
            {
                ApplyEffectToTarget(caster, sub, skill, finalSubValue, "서브 타겟");
            }
        }
    }

    // 내부 헬퍼 함수: 단일 대상에게 데미지나 힐을 가하는 핵심 로직
    private void ApplyEffectToTarget(UnitControl caster, UnitControl target, SkillData skill, int value, string targetTypeStr)
    {
        // 스킬의 성향 태그(Tendency)에 'Heal(치유형)'이 포함되어 있다면 회복으로 처리합니다.
        if (skill.skillTendencies.Contains(TendencyType.Heal))
        {
            target.HealHP(value);
            Debug.Log($"<color=green>[{caster.unitName}]가 [{target.unitName}]({targetTypeStr})의 체력을 {value}만큼 회복시켰습니다!</color>");
        }
        else
        {
            // 그 외의 공격형/제어형 등은 데미지로 판별하여 깎습니다.
            target.TakeDamage(value);
            Debug.Log($"<color=red>[{caster.unitName}]가 [{target.unitName}]({targetTypeStr})에게 {value}의 데미지를 입혔습니다!</color>");
        }
    }

    /// <summary>
    /// 킬 캐치(Kill Catch) 판단을 위한 가상 데미지 예측기.
    /// 실제로 유닛의 체력을 깎지 않고, 예상되는 평균 위력만을 반환합니다.
    /// </summary>
    public static int PredictDamage(UnitControl attacker, UnitControl target, SkillData skill)
    {
        if (attacker == null || target == null || skill == null) return 0;

        // 1. 스킬의 순수 평균 위력 계산
        int averageBasePower = (skill.minPower + skill.maxPower) / 2;

        // (향후 이곳에 attacker의 버프 상태나 음/양 충만 상태에 따른 데미지 증폭 연산이 추가될 수 있습니다.)
        int predictedDamage = averageBasePower;

        return predictedDamage;
    }

    public void ResolveCombatResults(List<UnitControl> allUnits)
    {
        //Phase 2-5
        //대미지 계산, 반사 데미지, 사망 체크 등 기능 구현 예정
    }
}