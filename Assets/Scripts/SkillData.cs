using UnityEngine;

// ==========================================
// [개방-폐쇄 원칙 (OCP) 준수]
// 새로운 속성(예: 제3의 속성)이 기획되면 
// 이 열거형에 단어만 추가하면 됩니다. 
// 다른 코드는 일절 수정할 필요가 없습니다.
// ==========================================
public enum AttributeType
{
    None = 0,
    YinYang = 1,   // 음/양 속성
    Dream = 2      // 꿈 속성
}

// 엑셀에서 읽어올 [속성 종류 + 시작 수치] 세트 구조체
[System.Serializable]
public struct UnitAttribute
{
    public AttributeType type; // 속성 종류
    public int baseValue;      // 전투 시작 시 초기값
}

// 에러 방지용 스킬 데이터 뼈대 (A-2 단계에서 구체화 예정)
public class SkillData : ScriptableObject
{
    public string skillID;
    public string skillName;
    // 향후 쿨타임, 데미지, 성향 태그 등이 추가될 예정입니다.
}