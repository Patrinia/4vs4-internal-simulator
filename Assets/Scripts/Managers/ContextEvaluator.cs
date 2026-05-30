using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [ContextEvaluator.cs]
// 전장의 실시간 상황(체력, 음/양 변수 등)을 분석하여
// 특정 스킬이 현재 얼마나 절박하게 필요한지 '상황 배수(Context Multiplier)'를 산출합니다.
// ====================================================
public class ContextEvaluator
{
    /// <summary>
    /// 주어진 스킬이 현재 전황에서 얼마나 가치 있는지 판단하여 최종 가산 배수를 반환합니다.
    /// 기본값은 1.0f이며, 각 딜레마 상황의 절박함에 따라 가산(+)됩니다.
    /// </summary>
    public float GetContextMultiplier(UnitControl caster, SkillData skill, List<UnitControl> allUnits)
    {
        float finalMultiplier = 1.0f;

        if (caster == null || skill == null) return finalMultiplier;

        // 3가지 핵심 전황 분석 로직을 독립적으로 통과하며 가산점을 누적합니다.
        finalMultiplier += EvaluateDeathSwitch(caster, skill);
        finalMultiplier += EvaluateErosionDefense(caster, skill);
        finalMultiplier += EvaluateKillCatch(caster, skill, allUnits);

        return finalMultiplier;
    }

    // ====================================================
    // [전황 분석 모듈 1]: 데스 스위치 (생존 본능)
    // ====================================================
    private float EvaluateDeathSwitch(UnitControl caster, SkillData skill)
    {
        // 치유(Heal)나 방어(Defensive) 성향이 없는 스킬이면 배수 가산 없음
        if (!skill.skillTendencies.Contains(TendencyType.Heal) &&
            !skill.skillTendencies.Contains(TendencyType.Defensive))
        {
            return 0f;
        }

        float hpRatio = (float)caster.currentHP / caster.SourceData.maxHP;

        // 체력이 50% 이하일 때부터 생존 본능 발동
        if (hpRatio <= 0.5f)
        {
            // 공식: (1.0 - 현재 HP비율) * 1.5 
            // 체력이 0에 가까워질수록 최대 1.5의 추가 배수(총 2.5배) 발생
            return (1.0f - hpRatio) * 1.5f;
        }

        return 0f;
    }

    // ====================================================
    // [전황 분석 모듈 2]: 침식 방어 본능 (속성 안정화)
    // ====================================================
    private float EvaluateErosionDefense(UnitControl caster, SkillData skill)
    {
        // 유닛의 현재 음/양 수치를 가져옴 (0~100 단일 변수)
        if (!caster.currentAttributes.TryGetValue(AttributeType.YinYang, out int yyValue))
        {
            return 0f;
        }

        // 위험 구간 판별 (0~10, 90~100은 이미 침식 통제 불능이므로, 그 직전 구간을 위험으로 간주)
        bool isYinDanger = (yyValue >= 11 && yyValue <= 25);
        bool isYangDanger = (yyValue >= 75 && yyValue <= 89);

        // 안전 지대(26~74)에 있다면 속성 조작에 대한 절박함이 없음
        if (!isYinDanger && !isYangDanger) return 0f;

        // 스킬이 음/양 게이지를 안정화(50 방향)하는 조작을 가지고 있는지 확인
        foreach (var mod in skill.attributeModifiers)
        {
            if (mod.type == AttributeType.YinYang)
            {
                // 음기 위험(낮은 수치)일 때 양기(+)를 더해주거나, 
                // 양기 위험(높은 수치)일 때 음기(-)를 더해주면 가치 폭증
                if ((isYinDanger && mod.amount > 0) || (isYangDanger && mod.amount < 0))
                {
                    return 1.5f; // 상황 안정화 스킬에 강력한 가산점 부여
                }
            }
        }

        return 0f;
    }

    // ====================================================
    // [전황 분석 모듈 3]: 킬 캐치 (처형 본능)
    // ====================================================
    private float EvaluateKillCatch(UnitControl caster, SkillData skill, List<UnitControl> allUnits)
    {
        // 공격(Aggressive) 성향이 없는 스킬은 킬 캐치 고려 대상이 아님
        if (!skill.skillTendencies.Contains(TendencyType.Aggressive)) return 0f;

        foreach (var target in allUnits)
        {
            // 사망한 유닛이거나 아군이면 타겟팅 계산에서 제외
            if (target.isDead || target.isPlayer == caster.isPlayer) continue;

            // CombatResolver의 가상 예측 함수를 호출하여 순수 데미지 도출
            int predictedDamage = CombatResolver.PredictDamage(caster, target, skill);

            // 해당 스킬로 적 하나를 확실하게 퇴각(사망)시킬 수 있다면
            if (predictedDamage >= target.currentHP)
            {
                return 1.0f; // 기본 1.0 + 1.0 가산 = 총 2.0배 폭증 (발견 즉시 반환)
            }
        }

        return 0f;
    }
}