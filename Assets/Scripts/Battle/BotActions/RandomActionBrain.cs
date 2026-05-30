using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [RandomActionBrain.cs]
// 전황을 읽지 않고 무작위로 행동하는 저랭크 유닛용 뇌입니다.
// ====================================================
public class RandomActionBrain : IUnitBrain
{
    // 무작위 뇌는 초기화 단계에서 특별히 기억할 정보가 없습니다.
    public void Initialize(UnitControl unit, List<SkillData> equippedSkills) { }

    public SkillData SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits)
    {
        // 안전 장치: 사용 가능한 스킬이 없다면 null을 반환하여 턴을 스킵하거나 기본 공격을 유도합니다.
        if (usableSkills == null || usableSkills.Count == 0) return null;

        // 사용 가능한 스킬 목록 중에서 무작위 인덱스를 추출하여 반환합니다.
        int randomIndex = Random.Range(0, usableSkills.Count);
        return usableSkills[randomIndex];
    }
}