using System.Collections.Generic;

// ====================================================
// [IUnitBrain.cs]
// DIP(의존성 역전 원칙)를 위한 뇌 인터페이스.
// 유닛은 이 인터페이스만 바라보며, 내부가 AI인지 사람인지 알 필요가 없습니다.
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
    /// 턴 시작 시, 쿨타임과 코스트 검사를 통과한 '사용 가능한 스킬 목록'을 받아 하나를 선택합니다.
    /// </summary>
    SkillData SelectNextSkill(List<SkillData> usableSkills, List<UnitControl> allUnits);
}