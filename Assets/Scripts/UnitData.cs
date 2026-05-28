using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "GameData/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("식별 데이터 (엑셀 연동)")]
    public string unitID;            // 유닛 고유 ID
    public string unitName;          // 유닛 이름

    [Header("기본 스탯 (엑셀 연동)")]
    public int maxHP;                // 최대 체력
    public int minSpeed;             // 속도 범위 최소값
    public int maxSpeed;             // 속도 범위 최대값

    [Header("속성 시스템 (엑셀 연동)")]
    // 유닛이 가질 수 있는 복수 속성과 전투 시작 시의 초기 수치
    public List<UnitAttribute> baseAttributes = new List<UnitAttribute>();

    [Header("스킬 시스템 풀 (엑셀 연동)")]
    // [업데이트] 이 캐릭터가 소유하고 배울 수 있는 전체 스킬 목록입니다.
    // 가상 봇은 이 목록 중에서 페르소나 점수가 높은 4개를 골라 UnitControl에 장착합니다.
    public List<SkillData> skillPool = new List<SkillData>();

    [Header("비주얼 에셋 (유니티 에디터 할당)")]
    public GameObject unitPrefab;    // 캐릭터 외형 프리팹
    public Sprite unitPortrait;      // 캐릭터 초상화 이미지
}