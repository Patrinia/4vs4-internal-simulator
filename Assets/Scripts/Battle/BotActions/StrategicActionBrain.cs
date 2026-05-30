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

        // [오류 수정] SkillData가 아니라 ActionDecision을 담아야 하며, 이름도 tiedDecisions로 맞춥니다.
        List<ActionDecision> tiedDecisions = new List<ActionDecision>(); // 동점 스킬 기록용 리스트

        foreach (SkillData skill in usableSkills)
        {
            // 페르소나 배수 추출 (이 스킬이 내 성향에 얼마나 맞는가?)
            float personaMult = personaBrain.GetPersonaMultiplier(skill);

            // 해당 스킬로 공격 가능한 '모든 경우의 수(타겟 그룹)'를 가져옵니다.
            //List<List<UnitControl>> possibleTargetGroups = GetPossibleTargetGroups(skill, allUnits);

            // 타겟팅 유틸리티를 통해 발생 가능한 모든 행동 시나리오를 가져옵니다.
            List<ActionDecision> possibleDecisions = TargetingUtility.GetAllPossibleDecisions(skill, myUnit, allUnits);

            foreach (ActionDecision decision in possibleDecisions)
            {
                // 평가를 위해 메인 타겟과 서브 타겟을 하나의 임시 리스트로 합칩니다.
                List<UnitControl> totalTargets = new List<UnitControl> { decision.MainTarget };
                totalTargets.AddRange(decision.SubTargets);

                float contextMult = contextEvaluator.GetContextMultiplier(myUnit, skill, totalTargets);
                float finalScore = 1.0f + (personaMult * contextMult);

                if (finalScore > maxScore)
                {
                    maxScore = finalScore;
                    tiedDecisions.Clear();
                    tiedDecisions.Add(decision);
                }
                else if (Mathf.Approximately(finalScore, maxScore))
                {
                    tiedDecisions.Add(decision);
                }
            }
        }

        if (tiedDecisions.Count == 0) return null;

        // 동점인 스킬이 여러 개라면 그 중 하나를 무작위로 선택하여 예측 불가능성 부여
        return tiedDecisions[Random.Range(0, tiedDecisions.Count)];
    }

}