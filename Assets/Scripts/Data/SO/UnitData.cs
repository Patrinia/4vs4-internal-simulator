using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "GameData/UnitData")]
public class UnitData : ScriptableObject
{
    [Header("식별 데이터 (엑셀 연동)")]
    public string unitID;            // 유닛 고유 ID
    public string unitName;          // 유닛 이름
    public int unitRank;             // 유닛의 등급 (0등급 ~ 5등급)

    [Header("기본 스탯 (엑셀 연동)")]
    public int maxHP;                // 최대 체력
    public int minSpeed;             // 속도 범위 최소값
    public int maxSpeed;             // 속도 범위 최대값

    [Header("속성 시스템 (엑셀 연동)")]
    // 유닛이 가질 수 있는 복수 속성과 전투 시작 시의 초기 수치
    public List<UnitAttribute> baseAttributes = new List<UnitAttribute>();

    // 엑셀에서 TRUE/FALSE로 파싱될 침식 특수 사망 면역 여부
    public bool isImmuneToCorrosion;

    [Header("스킬 시스템 풀 (엑셀 연동)")]
    // 이 캐릭터가 소유하고 배울 수 있는 스킬 목록이 3가지 종류로 세분화되었습니다.

    //플레이어가 UI에서 4개를 선택하거나, 봇이 페르소나에 맞춰 4개를 고를 전체 일반 스킬 목록
    [Tooltip("캐릭터가 소유한 일반 스킬 풀 (전투 돌입 전 4개 장착)")]
    public List<SkillData> normalSkillPool = new List<SkillData>();
    [Tooltip("이 유닛이 고정적으로 사용할 이동 스킬")]
    public SkillData movementSkill;
    [Tooltip("해금 시 선택할 수 있는 필살기 스킬 목록")]
    public List<SkillData> ultimateSkillPool = new List<SkillData>();

    [Header("AI 시스템 (엑셀 연동)")]
    // 엑셀에서 파싱될 이 유닛의 기본 뇌 타입
    public AIBrainType defaultAIBrainType;

    [Header("비주얼 에셋 (유니티 에디터 할당)")]
    public GameObject unitPrefab;    // 캐릭터 외형 프리팹
    public Sprite unitPortrait;      // 캐릭터 초상화 이미지
}