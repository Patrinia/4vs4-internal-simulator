using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [RandomActionBrain.cs]
// 전황을 읽지 않고 무작위로 행동하는 저랭크 유닛용 뇌입니다.
// ====================================================
public class RandomActionBrain : BaseAIBrain
{
    public override ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits, FormationManager formationManager)
    {
        if (usableSkills == null || usableSkills.Count == 0) return null;

        // 부모(BaseAIBrain)의 사거리 필터링 로직에 위임하여 완벽한 후보군만 받아옵니다.
        List<ActionDecision> validDecisions = GetValidDecisions(usableSkills, allUnits, formationManager);

        if (validDecisions.Count == 0) return null;

        // [업데이트] 무작위 난사 방지를 위한 룰렛 휠(Roulette Wheel) 가중치 분배 시스템 적용
        int totalWeight = 0;
        List<KeyValuePair<ActionDecision, int>> decisionWeights = new List<KeyValuePair<ActionDecision, int>>();

        foreach (ActionDecision decision in validDecisions)
        {
            int weight = 100; // 일반적인 스킬의 기본 가중치

            string categoryName = decision.SelectedSkill.category.ToString();

            if (categoryName.Contains("Ultimate"))
            {
                weight = 50;  // 필살기는 다소 신중하게
            }
            // [업데이트] 하드코딩된 문자열 검사를 제거하고, 명확한 객체지향 성향(Tag) 검사로 교체
            else if (decision.SelectedSkill.skillTendencies.Contains(TendencyType.SelfMove))
            {
                weight = 10;  // 공격 가능한 스킬이 섞여 있을 때 이동기의 선택 확률을 극도로 낮춤
            }
            else
            {
                weight = 100; // Normal(일반기) 등은 가장 높은 확률(100)로 선택됨
            }

            decisionWeights.Add(new KeyValuePair<ActionDecision, int>(decision, weight));
            totalWeight += weight;
        }

        // 총 가중치 합산 값 내에서 다트(난수)를 던집니다.
        int randomRoll = Random.Range(0, totalWeight);
        int currentSum = 0;

        foreach (var kvp in decisionWeights)
        {
            currentSum += kvp.Value;
            if (randomRoll < currentSum)
            {
                return kvp.Key;
            }
        }

        // 수학적 오차를 대비한 최종 안전장치
        return validDecisions[0];
    }
}