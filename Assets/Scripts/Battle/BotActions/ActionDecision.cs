using System.Collections.Generic;

// ====================================================
// [ActionDecision.cs]
// 뇌(Brain)가 의사결정을 마친 후, 선택된 스킬과 타겟 정보를 
// 하나의 패키지로 묶어 시스템(RoundManager)에 전달하는 DTO(데이터 전송 객체)입니다.
// ====================================================
public class ActionDecision
{
    // 프로퍼티를 통해 외부에서는 읽기(get)만 가능하고, 
    // 생성할 때만 값(set)을 넣을 수 있도록 캡슐화(불변성 보장)합니다.
    public SkillData SelectedSkill { get; private set; }
    public List<UnitControl> Targets { get; private set; }

    /// <summary>
    /// ActionDecision 생성자
    /// </summary>
    /// <param name="skill">뇌가 최종 선택한 스킬</param>
    /// <param name="targets">해당 스킬을 적용할 대상 유닛 리스트</param>
    public ActionDecision(SkillData skill, List<UnitControl> targets)
    {
        SelectedSkill = skill;
        Targets = targets;
    }
}