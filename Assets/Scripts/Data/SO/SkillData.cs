using UnityEngine;
using System.Collections.Generic;

// ====================================================
// [SkillData.cs]
// 기획 엑셀 파일과 1:1로 매칭되는 스킬 데이터의 설계도(ScriptableObject)입니다.
// 이 파일은 절대 실시간 전투 중에 값이 변하지 않는 '정적 데이터'입니다.
// ====================================================
[CreateAssetMenu(fileName = "NewSkillData", menuName = "GameData/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("1. 식별 및 텍스트 데이터 (엑셀 연동)")]
    [Tooltip("엑셀의 식별자와 매칭되는 고유 ID (예: Skill_001)")]
    public string skillID;
    [Tooltip("인게임 UI에 표시될 스킬의 실제 이름")]
    public string skillName;
    public SkillCategory category;    //스킬 카테고리
    [Tooltip("인게임 UI 툴팁에 표시될 스킬의 상세 설명")]
    [TextArea(2, 5)]
    public string description;

    [Header("2. 전투 위력 (엑셀 연동: 데미지/힐량 분리)")]
    [Tooltip("이 스킬을 한 번 사용한 후, 다시 사용하기 위해 기다려야 하는 턴 수")]
    public int maxCooldown;

    [Tooltip("스킬의 최소 데미지 (공격형이 아닐 경우 0)")]
    public int minDamage;
    [Tooltip("스킬의 최대 데미지 (공격형이 아닐 경우 0)")]
    public int maxDamage;

    [Tooltip("스킬의 최소 치유량 (회복형이 아닐 경우 0)")]
    public int minHeal;
    [Tooltip("스킬의 최대 치유량 (회복형이 아닐 경우 0)")]
    public int maxHeal;

    [Header("3. AI 의사결정 및 시스템 분류 (엑셀 연동)")]
    [Tooltip("이 스킬이 누구를 타겟으로 하는지 정의 (예: Enemy, Ally)")]
    public TargetType targetType;
    [Tooltip("스킬이 타격할 수 있는 최대 타겟 수")]
    public int maxTargetCount;
    [Tooltip("메인 타겟 인덱스 기준 서브 타겟의 상대적 위치 (예: -1, 1)")]
    public List<int> subTargetOffsets = new List<int>();
    [Tooltip("서브 타겟에게 적용될 위력의 배율 (예: 0.5)")]
    public float subTargetDamageRatio;
    [Tooltip("가상 플레이어(Bot)가 자신의 페르소나에 맞춰 스킬의 점수를 매길 때 참고하는 복합 성향 태그")]
    public List<TendencyType> skillTendencies = new List<TendencyType>();

    [Header("4. 특수 기믹 및 상태이상 (엑셀 연동)")]
    [Tooltip("스킬 적중 시 대상의 음/양, 꿈 게이지 등을 얼마나 변화시킬지 정의하는 리스트")]
    public List<AttributeModifier> attributeModifiers = new List<AttributeModifier>();
    [Tooltip("스킬 적중 시 대상에게 부여할 하이브리드 상태이상 데이터 목록")]
    public List<SkillEffectData> statusEffects = new List<SkillEffectData>();

    [Header("5. 시각/청각 에셋 (유니티 에디터 수동 할당)")]
    [Tooltip("스킬 버튼에 표시될 아이콘 이미지")]
    public Sprite skillIcon;
    [Tooltip("스킬 사용 시 씬에 생성될 화려한 파티클/이펙트 프리팹")]
    public GameObject vfxPrefab;
    [Tooltip("스킬 발동 시 재생될 사운드 클립")]
    public AudioClip sfxClip;
}