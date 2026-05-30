using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [SequenceActionBrain.cs]
// 정해진 순서(장착된 스킬 리스트의 인덱스 순)대로 행동하는 기믹형 뇌입니다.
// ====================================================
public class SequenceActionBrain : IUnitBrain
{
    private UnitControl myUnit;
    private List<SkillData> myEquippedSkills;
    private int currentStep = 0;

    public void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        myUnit = unit;
        myEquippedSkills = equippedSkills;
        currentStep = 0; // 전투 시작 시 첫 번째 패턴으로 초기화
    }

    public ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits)
    {
        if (myEquippedSkills == null || myEquippedSkills.Count == 0 || usableSkills.Count == 0)
            return null;

        // 이번 턴에 사용해야 할 목표 패턴 스킬을 확인합니다.
        SkillData targetSkill = myEquippedSkills[currentStep];

        // 목표 패턴의 스킬을 확인한 직후, 다음 턴을 위해 스텝을 전진시킵니다.
        // 리스트의 끝에 도달하면 나머지 연산(Modulo)을 통해 다시 0번으로 순환합니다.
        currentStep = (currentStep + 1) % myEquippedSkills.Count;

        // 만약 이번 턴의 패턴 스킬이 쿨타임 등의 이유로 usableSkills에 존재하지 않는다면?
        if (!usableSkills.Contains(targetSkill))
        {
            // 패턴이 꼬이지 않게 차선책(첫 번째 사용 가능 스킬 또는 기본 공격)을 시전합니다.
            return usableSkills[0];
        }

        List<UnitControl> targets = GetRandomValidTargets(targetSkill, allUnits);

        return new ActionDecision(targetSkill, targets);
    }

    private List<UnitControl> GetRandomValidTargets(SkillData skill, List<UnitControl> allUnits)
    {
        List<UnitControl> enemies = allUnits.FindAll(u => !u.isDead && u.isPlayer != myUnit.isPlayer);
        List<UnitControl> allies = allUnits.FindAll(u => !u.isDead && u.isPlayer == myUnit.isPlayer);

        switch (skill.targetType)
        {
            case TargetType.Self: return new List<UnitControl> { myUnit };
            case TargetType.AllEnemies: return enemies;
            case TargetType.SingleEnemy:
                return enemies.Count > 0 ? new List<UnitControl> { enemies[Random.Range(0, enemies.Count)] } : new List<UnitControl>();
            default: return new List<UnitControl>();
        }
    }
}