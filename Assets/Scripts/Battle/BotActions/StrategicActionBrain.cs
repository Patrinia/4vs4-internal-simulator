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
        myUnit = unit;

        // 뇌 내부에 자아와 전황 분석기 모듈을 조립합니다.
        personaBrain = new AIPersonaBrain();
        contextEvaluator = new ContextEvaluator();

        // 전투가 시작되면 장착된 스킬을 기반으로 자신의 성향(페르소나 가중치)을 구축합니다.
        personaBrain.BuildPersona(equippedSkills);
    }

    public SkillData SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits)
    {
        if (usableSkills == null || usableSkills.Count == 0) return null;

        float maxScore = -1f;
        List<SkillData> tiedSkills = new List<SkillData>(); // 동점 스킬 기록용 리스트

        foreach (SkillData skill in usableSkills)
        {
            // 1. 페르소나 배수 추출 (이 스킬이 내 성향에 얼마나 맞는가?)
            float personaMult = personaBrain.GetPersonaMultiplier(skill);

            // 2. 전황 배수 추출 (이 스킬이 지금 얼마나 절박하게 필요한가?)
            float contextMult = contextEvaluator.GetContextMultiplier(myUnit, skill, allUnits);

            // 3. 최종 점수(Utility Score) 연산
            float finalScore = 1.0f + (personaMult * contextMult);

            // 신기록 갱신: 기존 동점 리스트를 날려버리고 새 스킬을 등록
            if (finalScore > maxScore)
            {
                maxScore = finalScore;
                tiedSkills.Clear();
                tiedSkills.Add(skill);
            }
            // 동점 발생: 리스트에 스킬을 추가하여 후보군 보존
            else if (Mathf.Approximately(finalScore, maxScore))
            {
                tiedSkills.Add(skill);
            }
        }

        // 동점인 스킬이 여러 개라면 그 중 하나를 무작위로 선택하여 예측 불가능성 부여
        return tiedSkills[Random.Range(0, tiedSkills.Count)];
    }
}