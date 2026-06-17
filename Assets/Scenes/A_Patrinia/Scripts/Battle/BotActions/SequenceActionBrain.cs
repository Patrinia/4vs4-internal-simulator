using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [SequenceActionBrain.cs]
// 정해진 순서(장착된 스킬 리스트의 인덱스 순)대로 행동하는 기믹형 뇌입니다.
// ====================================================
public class SequenceActionBrain : BaseAIBrain
{
    private int currentStep = 0;

    public override void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        base.Initialize(unit, equippedSkills);
        currentStep = 0; // 전투 시작 시 첫 번째 패턴으로 초기화
    }

    public override ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits, FormationManager formationManager)
    {
        if (myEquippedSkills == null || myEquippedSkills.Count == 0 || usableSkills.Count == 0)
            return null;

        // 이번 턴에 사용해야 할 목표 패턴 스킬을 확인합니다.
        SkillData targetSkill = myEquippedSkills[currentStep];
        currentStep = (currentStep + 1) % myEquippedSkills.Count;

        if (!usableSkills.Contains(targetSkill))
        {
            targetSkill = usableSkills[0];
        }

        // 목표 스킬 하나만 담아서 부모의 사거리 헬퍼를 통과시킵니다.
        // 만약 목표 스킬의 사거리가 닿지 않으면, 부모가 억지로 허공에 쏘는 대신 '이동 스킬'을 반환해 줍니다.
        List<ActionDecision> validDecisions = GetValidDecisions(new List<SkillData> { targetSkill }, allUnits, formationManager);

        if (validDecisions.Count == 0) return null;

        return validDecisions[Random.Range(0, validDecisions.Count)];
    }
}