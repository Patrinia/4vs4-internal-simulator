using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ====================================================
// [AIPersonaBrain.cs]
// 유닛이 장착한 스킬들을 스캔하여 자아(Persona)를 형성하고,
// 성향에 따른 스킬 선호도(Utility Score Multiplier)를 계산하는 AI의 뇌입니다.
// ====================================================
public class AIPersonaBrain
{
    // 유닛의 성향 비율을 저장하는 딕셔너리 (예: Aggressive -> 0.5f (50%))
    private Dictionary<TendencyType, float> personaWeights = new Dictionary<TendencyType, float>();

    /// <summary>
    /// 1. 전투 시작 시 장착된 스킬 태그들을 수집하여 페르소나 가중치를 형성합니다.
    /// </summary>
    public void BuildPersona(List<SkillData> equippedSkills)
    {
        personaWeights.Clear();
        int totalTags = 0;

        // 1단계: 장착된 모든 스킬의 태그 개수를 누적합니다.
        foreach (SkillData skill in equippedSkills)
        {
            if (skill == null) continue;

            foreach (TendencyType tendency in skill.skillTendencies)
            {
                if (!personaWeights.ContainsKey(tendency))
                {
                    personaWeights[tendency] = 0f;
                }
                personaWeights[tendency] += 1f;
                totalTags++;
            }
        }

        // 2단계: 누적된 태그 개수를 전체 태그 개수로 나누어 비율(0.0 ~ 1.0)로 변환합니다.
        if (totalTags > 0)
        {
            List<TendencyType> keys = personaWeights.Keys.ToList();
            foreach (TendencyType key in keys)
            {
                personaWeights[key] /= totalTags;
            }
        }
    }

    /// <summary>
    /// 2. 특정 스킬을 평가할 때, 내 페르소나와 얼마나 일치하는지 배수(Multiplier)를 반환합니다.
    /// </summary>
    public float GetPersonaMultiplier(SkillData skill)
    {
        // 기본 배수는 1.0배 (기본적인 가치는 인정함)
        float multiplier = 1.0f;

        if (skill == null || skill.skillTendencies.Count == 0)
            return multiplier;

        // 스킬이 가진 태그들을 내 페르소나 가중치와 대조하여 가산합니다.
        foreach (TendencyType tendency in skill.skillTendencies)
        {
            if (personaWeights.ContainsKey(tendency))
            {
                // 내 성향 비율만큼 해당 스킬의 가치가 증폭됩니다.
                multiplier += personaWeights[tendency];
            }
        }

        return multiplier;
    }

    /// <summary>
    /// [디버깅용] 현재 형성된 유닛의 페르소나를 텍스트로 출력합니다.
    /// </summary>
    public string GetPersonaReport(string unitName)
    {
        if (personaWeights.Count == 0) return $"[{unitName}] сформи된 페르소나가 없습니다.";

        string report = $"[{unitName}의 페르소나 분석]\n";
        foreach (var kvp in personaWeights)
        {
            report += $"- {kvp.Key}: {(kvp.Value * 100):F1}%\n";
        }
        return report;
    }
}