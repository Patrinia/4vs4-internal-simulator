using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [StrategicActionBrain.cs]
// 성향(자아)과 현재 전황 배수를 수학적으로 합산하여 
// 최고 가치의 스킬을 찾아내는 고랭크/네임드 전용 사령관 뇌입니다.
// ====================================================
public class StrategicActionBrain : BaseAIBrain
{
    private AIPersonaBrain personaBrain;
    private ContextEvaluator contextEvaluator;

    public override void Initialize(UnitControl unit, List<SkillData> equippedSkills)
    {
        base.Initialize(unit, equippedSkills);

        // 뇌 내부에 자아와 전황 분석기 모듈을 조립합니다.
        personaBrain = new AIPersonaBrain();
        contextEvaluator = new ContextEvaluator();

        // 전투가 시작되면 장착된 스킬을 기반으로 자신의 성향(페르소나 가중치)을 구축합니다.
        personaBrain.BuildPersona(equippedSkills);
    }

    public override ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits, FormationManager formationManager)
    {
        if (usableSkills == null || usableSkills.Count == 0) return null;

        // 사거리가 안 닿는 불가능한 결정들을 부모의 필터로 사전에 컷(Cut)합니다.
        List<ActionDecision> validDecisions = GetValidDecisions(usableSkills, allUnits, formationManager);

        if (validDecisions.Count == 0) return null;

        float maxScore = -1f;
        List<ActionDecision> tiedDecisions = new List<ActionDecision>();

        foreach (ActionDecision decision in validDecisions)
        {
            float personaMult = personaBrain.GetPersonaMultiplier(decision.SelectedSkill);

            // 평가를 위해 메인 타겟과 서브 타겟을 하나의 임시 리스트로 합칩니다.
            List<UnitControl> totalTargets = new List<UnitControl> { decision.MainTarget };

            // [방어코드] 이동기일 경우 서브 타겟이 null일 수 있으므로 방어
            if (decision.SubTargets != null)
            {
                totalTargets.AddRange(decision.SubTargets);
            }

            float contextMult = contextEvaluator.GetContextMultiplier(myUnit, decision.SelectedSkill, totalTargets);
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

        if (tiedDecisions.Count == 0) return null;

        return tiedDecisions[Random.Range(0, tiedDecisions.Count)];
    }
}