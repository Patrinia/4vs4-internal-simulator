using System.Collections.Generic;

// ====================================================
// [ActionDecision.cs]
// 뇌(Brain)가 의사결정을 마친 후, 선택된 스킬과 타겟 정보를 
// 하나의 패키지로 묶어 시스템(BattleManager)에 전달하는 DTO(데이터 전송 객체)입니다.
// ====================================================
public class ActionDecision
{
    // 프로퍼티를 통해 외부에서는 읽기(get)만 가능하고, 
    // 생성할 때만 값(set)을 넣을 수 있도록 캡슐화(불변성 보장)합니다.
    public SkillData SelectedSkill { get; private set; }
    public UnitControl MainTarget { get; private set; }
    public List<UnitControl> SubTargets { get; private set; }

    // 빈칸 이동 등 특정 좌표(슬롯)를 타겟팅하기 위한 인덱스 추가 (기본값 -1)
    public int TargetSlotIndex { get; private set; }

    /// <summary>
    /// ActionDecision 생성자 (TargetSlotIndex 선택적 매개변수 추가)
    /// </summary>
    public ActionDecision(SkillData skill, UnitControl mainTarget, List<UnitControl> subTargets, int targetSlotIndex = -1)
    {
        SelectedSkill = skill;
        MainTarget = mainTarget;
        // 서브 타겟이 null로 들어오면 빈 리스트로 안전하게 초기화합니다.
        SubTargets = subTargets ?? new List<UnitControl>();
        TargetSlotIndex = targetSlotIndex;
    }
}