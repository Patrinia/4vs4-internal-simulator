using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [StrategicActionBrain.cs]
// 성향(자아)과 현재 전황 배수를 수학적으로 합산하여 
// 최고 가치의 스킬을 찾아내는 고랭크/네임드 전용 사령관 뇌입니다.
// ====================================================
public class StrategicActionBrain : IUnitBrain
{
    private AIPersonaBrain personaBrain;
    private ContextEvaluator contextEvaluator;
    private UnitControl myUnit;

    public void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        myUnit = unit; // 피아식별을 위해 자신을 기억

        // 뇌 내부에 자아와 전황 분석기 모듈을 조립합니다.
        personaBrain = new AIPersonaBrain();
        contextEvaluator = new ContextEvaluator();

        // 전투가 시작되면 장착된 스킬을 기반으로 자신의 성향(페르소나 가중치)을 구축합니다.
        personaBrain.BuildPersona(equippedSkills);
    }

    public ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits)
    {
        if (usableSkills == null || usableSkills.Count == 0) return null;

        float maxScore = -1f;
        List<SkillData> tiedSkills = new List<SkillData>(); // 동점 스킬 기록용 리스트

        foreach (SkillData skill in usableSkills)
        {
            // 페르소나 배수 추출 (이 스킬이 내 성향에 얼마나 맞는가?)
            float personaMult = personaBrain.GetPersonaMultiplier(skill);

            // 해당 스킬로 공격 가능한 '모든 경우의 수(타겟 그룹)'를 가져옵니다.
            List<List<UnitControl>> possibleTargetGroups = GetPossibleTargetGroups(skill, allUnits);

            // 각각의 경우의 수를 순회하며 가치를 평가합니다.
            foreach (List<UnitControl> targets in possibleTargetGroups)
            {
                if (targets.Count == 0) continue; // 유효한 타겟이 없으면 스킵

                float contextMult = contextEvaluator.GetContextMultiplier(myUnit, skill, targets);
                float finalScore = 1.0f + (personaMult * contextMult);

                ActionDecision newDecision = new ActionDecision(skill, targets);

                if (finalScore > maxScore)
                {
                    maxScore = finalScore;
                    tiedDecisions.Clear();
                    tiedDecisions.Add(newDecision);
                }
                else if (Mathf.Approximately(finalScore, maxScore))
                {
                    tiedDecisions.Add(newDecision);
                }
            }
        }

        if (tiedDecisions.Count == 0) return null;

        // 동점인 스킬이 여러 개라면 그 중 하나를 무작위로 선택하여 예측 불가능성 부여
        return tiedSkills[Random.Range(0, tiedSkills.Count)];
    }

    // [최적화 및 시나리오 헬퍼]
    private List<List<UnitControl>> GetPossibleTargetGroups(SkillData skill, List<UnitControl> allUnits)
    {
        List<List<UnitControl>> groups = new List<List<UnitControl>>();
        List<UnitControl> enemies = allUnits.FindAll(u => !u.isDead && u.isPlayer != myUnit.isPlayer);
        List<UnitControl> allies = allUnits.FindAll(u => !u.isDead && u.isPlayer == myUnit.isPlayer);

        // TODO: 도발 기믹이 추가되면 여기서 enemies 리스트를 도발 유닛만 남도록 필터링합니다.

        switch (skill.targetType)
        {
            case TargetType.Self:
                groups.Add(new List<UnitControl> { myUnit });
                break;
            case TargetType.AllEnemies:
                // 광역기 Bypass: 경우의 수를 적 1명씩 나누지 않고 '전체 적'이라는 1개의 경우의 수만 생성
                groups.Add(enemies);
                break;
            case TargetType.AllAllies:
                groups.Add(allies);
                break;
            case TargetType.SingleEnemy:
                // 단일기: 적의 수만큼 경우의 수를 쪼개서 각각 테스트
                foreach (var e in enemies) groups.Add(new List<UnitControl> { e });
                break;
            case TargetType.SingleAlly:
                foreach (var a in allies) groups.Add(new List<UnitControl> { a });
                break;
        }
        return groups;
    }
}