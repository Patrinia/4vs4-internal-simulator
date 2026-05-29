using UnityEngine;
using System.Collections.Generic;

public class UnitControl : MonoBehaviour
{
    [Header("원본 데이터 참조")]
    public UnitData SourceData { get; private set; }

    // RoundManager에서 unit.unitName으로 접근할 수 있도록 제공하는 프로퍼티
    public string unitName => SourceData != null ? SourceData.unitName : "Unknown";

    [Header("실시간 전투 스탯")]
    public int currentHP;        // 현재 체력
    public int currentSpeed;     // 이번 라운드의 현재 속도
    public bool isDead = false;  // 사망 여부 플래그
    public bool isPlayer;        // 아군/적군 판별 플래그

    [Header("실시간 속성 수치 관리")]
    public Dictionary<AttributeType, int> currentAttributes = new Dictionary<AttributeType, int>();

    [Header("장착된 스킬")]
    // 전투 시작 전, skillPool에서 선택된 4개의 스킬
    public List<SkillData> equippedSkills = new List<SkillData>();

    // ========================================================================
    // [1. 초기화 및 기본 로직]
    // ========================================================================

    public void Init(UnitData data, bool isPlayerSide)
    {
        SourceData = data;
        isPlayer = isPlayerSide;
        currentHP = data.maxHP;
        isDead = false;

        currentAttributes.Clear();
        if (data.baseAttributes != null)
        {
            foreach (var attr in data.baseAttributes)
            {
                if (!currentAttributes.ContainsKey(attr.type))
                {
                    currentAttributes.Add(attr.type, attr.baseValue);
                }
            }
        }
    }

    // 라운드 시작 시 속도 굴림 함수
    public void RollCurrentSpeed()
    {
        if (SourceData != null)
        {
            currentSpeed = Random.Range(SourceData.minSpeed, SourceData.maxSpeed + 1);
        }
    }

    // 속성 증감 조작 함수
    public void ModifyAttribute(AttributeType type, int amount)
    {
        if (currentAttributes.ContainsKey(type))
        {
            currentAttributes[type] += amount;
            // 게이지 한계치 고정 (0 ~ 100)
            currentAttributes[type] = Mathf.Clamp(currentAttributes[type], 0, 100);
        }
    }

    // ========================================================================
    // [2. A-1 파이프라인 연동 로직 (새로 추가된 부분)]
    // ========================================================================

    // Phase 2-3. 행동 불가 상태 확인 (음기 침식)
    public bool IsUnableToAct()
    {
        if (currentAttributes.TryGetValue(AttributeType.YinYang, out int yinYangValue))
        {
            // 음기 침식: 수치가 0~10일 경우 행동 1턴 스킵
            if (yinYangValue <= 10) return true;
        }

        // TODO: 향후 기절(Stun) 등의 상태이상 검사도 이곳에 추가됩니다.
        return false;
    }

    // Phase 2-4. 강제 무작위 행동 상태 확인 (양기 침식)
    public bool IsForcedToActRandomly()
    {
        if (currentAttributes.TryGetValue(AttributeType.YinYang, out int yinYangValue))
        {
            // 양기 침식: 수치가 90~100일 경우 제어권 상실 (무작위 행동)
            if (yinYangValue >= 90) return true;
        }
        return false;
    }

    // Phase 2-5. 결과 정산 시 CombatResolver가 호출할 데미지 적용 함수
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            // 주의: 여기서 즉시 isDead = true 처리를 하지 않습니다!
            // '불사(1턴 버티기)' 버프 기믹을 위해, 사망 판정은 CombatResolver가 일괄 수행합니다.
        }
    }

    // 체력 회복 함수 (최대 체력 제한 적용)
    public void HealHP(int amount)
    {
        if (isDead) return;
        currentHP += amount;

        // 회복량이 최대 체력을 넘지 않도록 제한
        if (currentHP > SourceData.maxHP)
        {
            currentHP = SourceData.maxHP;
        }
    }
}