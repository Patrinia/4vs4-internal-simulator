using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [RandomActionBrain.cs]
// 전황을 읽지 않고 무작위로 행동하는 저랭크 유닛용 뇌입니다.
// ====================================================
public class RandomActionBrain : IUnitBrain
{
    private UnitControl myUnit;

    public void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        myUnit = unit; // 피아식별을 위해 자신을 기억
    }

    // [업데이트] FormationManager 매개변수 추가 및 전달
    public ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits, FormationManager formationManager)
    {
        // 안전 장치: 사용 가능한 스킬이 없다면 null을 반환하여 턴을 스킵
        if (usableSkills == null || usableSkills.Count == 0) return null;

        // 스킬 무작위 선택
        SkillData selectedSkill = usableSkills[Random.Range(0, usableSkills.Count)];

        // 유틸리티를 호출하여 합법적인 모든 타겟 경우의 수를 받아옴 (진형 정보 전달)
        List<ActionDecision> possibleDecisions = TargetingUtility.GetAllPossibleDecisions(selectedSkill, myUnit, allUnits, formationManager);

        if (possibleDecisions.Count == 0) return null;

        // possibleDecisions 중에서 무작위로 하나를 골라 반환합니다.
        return possibleDecisions[Random.Range(0, possibleDecisions.Count)];
    }
}