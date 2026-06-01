using System.Collections.Generic;
using UnityEngine;

// 스킬의 난수 굴림, 버프/디버프 연산, 그리고 최종 적용을 담당하는 전문가
// 전투 결과 정산 및 사망 처리 전문가
public class CombatResolver
{
    // StatusEffectManager에 대한 의존성 주입 (팩토리 기능 사용)
    private StatusEffectManager effectManager;


    // ActionDecision을 통째로 받아 메인(100%)과 서브(배율)를 분리하여 연산합니다.
    public void ExecuteAction(UnitControl caster, ActionDecision decision)
    {
        if (decision == null || decision.SelectedSkill == null || decision.MainTarget == null) return;

        SkillData skill = decision.SelectedSkill;

        // [업데이트] 1. 데미지 또는 힐량 판별 및 위력 굴림
        bool isHealSkill = skill.skillTendencies.Contains(TendencyType.Heal);
        int baseValue = 0;

        if (isHealSkill)
            baseValue = Random.Range(skill.minHeal, skill.maxHeal + 1);
        else
            baseValue = Random.Range(skill.minDamage, skill.maxDamage + 1);

        // 2. 버프/디버프 연산 (기믹 파이프라인)
        // [업데이트] StatusEffectManager를 통해 실제 버프 수치를 가져옵니다.
        float buffMultiplier = effectManager.GetAttackMultiplier(caster);
        float debuffMultiplier = effectManager.GetDefenseMultiplier(decision.MainTarget);

        // 힐 스킬일 경우 디버프(방어력) 계산을 무시하고, 데미지일 경우만 적용
        int finalMainValue = isHealSkill ?
                             Mathf.RoundToInt(baseValue * buffMultiplier) :
                             Mathf.RoundToInt(baseValue * buffMultiplier * debuffMultiplier);

        // 3. 메인 타겟 데미지/회복 적용 (100%)
        ApplyEffectToTarget(caster, decision.MainTarget, skill, finalMainValue, "메인 타겟");

        // 4. 서브 타겟 적용 (개별 방어력 연산 적용)
        if (decision.SubTargets.Count > 0 && skill.subTargetDamageRatio > 0f)
        {
            foreach (var sub in decision.SubTargets)
            {
                // 서브 타겟 본인만의 방어력/디버프 배율을 새로 가져옵니다.
                float subDebuffMultiplier = effectManager.GetDefenseMultiplier(sub);

                // 순수 위력(baseValue) * 시전자 공업 * 서브타겟 방업 * 서브타겟 배율(0.5 등)
                int finalSubValue = isHealSkill ?
                                    Mathf.RoundToInt(baseValue * buffMultiplier * skill.subTargetDamageRatio) :
                                    Mathf.RoundToInt(baseValue * buffMultiplier * subDebuffMultiplier * skill.subTargetDamageRatio);

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

        // 5. 데미지/힐 적용 후, 스킬에 담긴 부가 효과(상태이상, 게이지 변동) 처리
        if (effectManager != null)
        {
            effectManager.ApplySkillEffects(caster, target, skill);
        }
    }

    /// <summary>
    /// 킬 캐치(Kill Catch) 판단을 위한 가상 데미지 예측기.
    /// 실제로 유닛의 체력을 깎지 않고, 예상되는 평균 위력만을 반환합니다.
    /// </summary>
    public static int PredictDamage(UnitControl attacker, UnitControl target, SkillData skill)
    {
        if (attacker == null || target == null || skill == null) return 0;

        // 데미지 스킬인지 힐 스킬인지 판단하여 예측
        // 데미지 예측이므로 힐은 0 반환
        if (skill.skillTendencies.Contains(TendencyType.Heal)) return 0;

        // 1. 스킬의 순수 평균 위력 계산
        int averageBasePower = (skill.minDamage + skill.maxDamage) / 2;

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