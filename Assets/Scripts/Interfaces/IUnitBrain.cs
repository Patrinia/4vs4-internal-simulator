using System.Collections.Generic;

// ====================================================
// [IUnitBrain.cs]
// DIP(의존성 역전 원칙)를 위한 뇌 인터페이스.
// 유닛은 이 인터페이스만 바라보며, 내부가 AI인지 사람인지 알 필요가 없습니다.
// 모든 종류의 뇌는 스킬과 타겟을 결정하여 ActionDecision으로 반환해야 합니다.
// ====================================================
public interface IUnitBrain
{
    /// <summary>
    /// 전투 시작 시 뇌를 유닛에 연결하고 초기 설정(페르소나 형성 등)을 진행합니다.
    /// </summary>
    void Initialize(UnitControl unit, List<SkillData> equippedSkills);

    // (참고: 향후 AIDecisionMaker가 완성되면 아래와 같은 행동 결정 메서드가 추가될 예정입니다.)
    // SkillData SelectNextSkill(List<UnitControl> allUnits);

    /// <summary>
    /// 턴 시작 시, 쿨타임 검사를 통과한 스킬 목록을 받아 
    /// 최적의 스킬과 그 대상(Target)을 지정하여 ActionDecision 객체로 반환합니다.
    /// </summary>
    /// <param name="usableSkills">현재 쿨타임이 돌고 있지 않은 사용 가능 스킬들</param>
    /// <param name="allUnits">전장에 존재하는 모든 유닛 (피아식별 및 타겟팅 용도)</param>
    /// <returns>선택된 스킬과 타겟 유닛 리스트가 담긴 DTO (사용 가능 스킬이 없으면 null 반환)</returns>
    ActionDecision SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits);
}