using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [BaseAIBrain.cs]
// 모든 AI 두뇌의 부모(추상) 클래스입니다.
// 사거리 기반 타겟 필터링과 이동기(Fallback) 탐색 등 
// 공통된 유효성 검사(Validation) 로직을 전담하여 DRY 원칙을 수호합니다.
// ====================================================
public abstract class BaseAIBrain : IUnitBrain
{
    protected UnitControl myUnit;
    protected List<SkillData> myEquippedSkills;

    public virtual void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        myUnit = unit;
        myEquippedSkills = equippedSkills;
    }

    // 자식 클래스들이 각자의 성향대로 구현해야 할 추상 메서드
    public abstract ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits, FormationManager formationManager);

    /// <summary>
    /// 쿨타임이 통과된 스킬들을 받아, 실제 사거리가 닿는 유효한(Valid) 타겟팅 경우의 수만 걸러냅니다.
    /// </summary>
    protected List<ActionDecision> GetValidDecisions(List<SkillData> usableSkills, List<UnitControl> allUnits, FormationManager formationManager)
    {
        List<ActionDecision> validDecisions = new List<ActionDecision>();

        foreach (SkillData skill in usableSkills)
        {
            // 유틸리티를 통해 피아식별이 완료된 1차 타겟 목록을 받습니다.
            List<ActionDecision> rawDecisions = TargetingUtility.GetAllPossibleDecisions(skill, myUnit, allUnits, formationManager);

            foreach (ActionDecision decision in rawDecisions)
            {
                // 2차 필터링: 사거리(Range) 유효성 검사
                // 자기 자신을 타겟으로 하거나(TargetType.Self), 사거리 데이터가 없는 스킬은 무조건 통과
                if (skill.targetType == TargetType.Self || skill.validRanges == null || skill.validRanges.Count == 0)
                {
                    validDecisions.Add(decision);
                    continue;
                }

                // 거리 계산 및 검증
                int distance = formationManager.GetDistance(myUnit, decision.MainTarget);
                if (distance != -1 && skill.validRanges.Contains(distance))
                {
                    validDecisions.Add(decision);
                }
            }
        }

        // 3. 만약 사거리가 닿는 공격/버프 스킬이 단 하나도 없다면? -> 이동기(Fallback) 탐색
        if (validDecisions.Count == 0 && myUnit.SourceData != null && myUnit.SourceData.movementSkill != null)
        {
            // 목표 스킬들의 사거리를 분석하여 전/후방 중 가장 유리한 위치를 계산하는 스마트 이동 호출
            ActionDecision moveDecision = GenerateSmartMovementDecision(myUnit.SourceData.movementSkill, usableSkills, allUnits);
            if (moveDecision != null)
            {
                validDecisions.Add(moveDecision);
            }
        }

        return validDecisions;
    }

    /// <summary>
    /// 엑셀의 ValidRange를 '방향 벡터'로 사용하여, 
    /// 목표 스킬들의 사거리 내에 가장 많은 유효 타겟이 들어오는 슬롯으로 이동 행동을 조립합니다.
    /// </summary>
    protected virtual ActionDecision GenerateSmartMovementDecision(SkillData moveSkill, List<SkillData> desiredSkills, List<UnitControl> allUnits)
    {
        if (myUnit.positionIndex == -1) return null;

        int bestIndex = -1;
        int maxScore = -1;

        // 엑셀에 정의된 ValidRange를 가져옵니다. 비어있을 경우 기본값(앞 1칸, 뒤 1칸) 세팅
        List<int> potentialOffsets = (moveSkill.validRanges != null && moveSkill.validRanges.Count > 0)
                                     ? moveSkill.validRanges
                                     : new List<int> { -1, 1 };

        foreach (int offset in potentialOffsets)
        {
            // 이동 스킬의 ValidRange 값을 '방향이 있는 변위(Vector)'로 해석하여 적용
            int nextIndex = myUnit.positionIndex + offset;

            // 바운드 체크 (진형 0~3 슬롯 내부만 유효)
            if (nextIndex >= 0 && nextIndex <= 3)
            {
                int score = EvaluatePositionScore(nextIndex, desiredSkills, allUnits);

                // 점수가 높으면 갱신 (점수가 동일할 경우 배열 순서상 먼저 탐색된 오프셋이 우선순위를 가짐)
                if (score > maxScore)
                {
                    maxScore = score;
                    bestIndex = nextIndex;
                }
            }
        }

        if (bestIndex != -1)
        {
            return new ActionDecision(moveSkill, myUnit, null, bestIndex);
        }

        return null;
    }

    /// <summary>
    /// [내부 헬퍼] 가상의 슬롯으로 이동했을 때, 목표 스킬들의 타겟이 몇 명이나 닿는지 채점합니다.
    /// </summary>
    private int EvaluatePositionScore(int virtualIndex, List<SkillData> desiredSkills, List<UnitControl> allUnits)
    {
        int score = 0;
        // 가상 슬롯에 따른 절대 좌표 환산
        int virtualAbsolutePos = myUnit.isPlayer ? (3 - virtualIndex) : (4 + virtualIndex);

        foreach (SkillData skill in desiredSkills)
        {
            if (skill.validRanges == null || skill.validRanges.Count == 0) continue;

            foreach (UnitControl target in allUnits)
            {
                if (target.isDead || target.positionIndex == -1) continue;

                // 스킬 피아식별 일치 검사
                if ((skill.targetType == TargetType.Enemy && target.isPlayer == myUnit.isPlayer) ||
                    (skill.targetType == TargetType.Ally && target.isPlayer != myUnit.isPlayer))
                {
                    continue;
                }

                int targetAbsolutePos = target.isPlayer ? (3 - target.positionIndex) : (4 + target.positionIndex);
                int distance = Mathf.Abs(virtualAbsolutePos - targetAbsolutePos);

                // 사거리에 들어온다면 점수(유리함) 가산
                if (skill.validRanges.Contains(distance))
                {
                    score++;
                }
            }
        }
        return score;
    }
}