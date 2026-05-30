using System.Collections.Generic;
using UnityEngine;

public static class TargetingUtility
{
    /// <summary>
    /// 해당 스킬로 지정할 수 있는 모든 타겟팅 경우의 수를 ActionDecision 리스트로 반환합니다.
    /// </summary>
    public static List<ActionDecision> GetAllPossibleDecisions(SkillData skill, UnitControl caster, List<UnitControl> allUnits)
    {
        List<ActionDecision> possibleDecisions = new List<ActionDecision>();
        List<UnitControl> validTargets = new List<UnitControl>();

        // 1. 피아식별 필터링 (새로운 TargetType 적용)
        if (skill.targetType == TargetType.Enemy)
            validTargets = allUnits.FindAll(u => !u.isDead && u.isPlayer != caster.isPlayer);
        else if (skill.targetType == TargetType.Ally)
            validTargets = allUnits.FindAll(u => !u.isDead && u.isPlayer == caster.isPlayer);
        else if (skill.targetType == TargetType.Self)
            validTargets = new List<UnitControl> { caster };

        // TODO: 향후 여기에 '도발(Taunt)' 상태인 유닛만 validTargets에 남기는 필터링 로직 추가 예정

        if (validTargets.Count == 0) return possibleDecisions;

        // 2. 광역기 최적화 로직 (타겟 수가 적 수보다 많거나 같을 때 연산 생략)
        if (skill.maxTargetCount >= validTargets.Count && skill.targetType != TargetType.Self)
        {
            UnitControl main = validTargets[0]; // 대표 메인 타겟 1명 지정
            List<UnitControl> subs = new List<UnitControl>(validTargets);
            subs.RemoveAt(0); // 메인 타겟을 제외한 나머지를 서브 타겟으로 취급

            possibleDecisions.Add(new ActionDecision(skill, main, subs));
        }
        else
        {
            // 3. 단일 및 부분 광역기: 각 유닛을 메인 타겟으로 삼는 모든 경우의 수 생성
            foreach (var target in validTargets)
            {
                List<UnitControl> subs = new List<UnitControl>();
                // TODO: FormationManager가 완성되면 skill.subTargetOffsets를 기반으로 서브 타겟 추출 연동
                possibleDecisions.Add(new ActionDecision(skill, target, subs));
            }
        }

        return possibleDecisions;
    }
}