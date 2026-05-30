using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [RandomActionBrain.cs]
// 전황을 읽지 않고 무작위로 행동하는 저랭크 유닛용 뇌입니다.
// ====================================================
public class RandomActionBrain : IUnitBrain
{
    public void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        myUnit = unit; // 피아식별을 위해 자신을 기억
    }

    public ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits)
    {
        // 안전 장치: 사용 가능한 스킬이 없다면 null을 반환하여 턴을 스킵
        if (usableSkills == null || usableSkills.Count == 0) return null;

        // 스킬 무작위 선택
        SkillData selectedSkill = usableSkills[Random.Range(0, usableSkills.Count)];

        // 타겟 무작위 지정 (TargetType 기반)
        List<UnitControl> targets = GetRandomValidTargets(selectedSkill, allUnits);

        return new ActionDecision(selectedSkill, targets);
    }

    // [피아식별 및 타겟 자동 할당 헬퍼]
    private List<UnitControl> GetRandomValidTargets(SkillData skill, List<UnitControl> allUnits)
    {
        List<UnitControl> enemies = allUnits.FindAll(u => !u.isDead && u.isPlayer != myUnit.isPlayer);
        List<UnitControl> allies = allUnits.FindAll(u => !u.isDead && u.isPlayer == myUnit.isPlayer);

        switch (skill.targetType)
        {
            case TargetType.Self: return new List<UnitControl> { myUnit };
            case TargetType.AllEnemies: return enemies; // 광역기 Bypass (전체 반환)
            case TargetType.AllAllies: return allies;
            case TargetType.SingleEnemy:
                return enemies.Count > 0 ? new List<UnitControl> { enemies[Random.Range(0, enemies.Count)] } : new List<UnitControl>();
            case TargetType.SingleAlly:
                return allies.Count > 0 ? new List<UnitControl> { allies[Random.Range(0, allies.Count)] } : new List<UnitControl>();
            default: return new List<UnitControl>();
        }
    }

}