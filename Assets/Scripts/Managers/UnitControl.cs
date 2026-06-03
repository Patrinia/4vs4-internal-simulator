using UnityEngine;
using System.Collections.Generic;
using System;

// ====================================================
// [UnitControl.cs]
// 전장에 스폰된 유닛 객체의 실시간 상태와 체력/속성 변수를 관리합니다.
// ====================================================
public class UnitControl : MonoBehaviour
{
    [Header("원본 데이터 참조")]
    public UnitData SourceData { get; private set; }

    // BattleManager에서 unit.unitName으로 안전하게 접근할 수 있도록 제공하는 프로퍼티
    public string unitName => SourceData != null ? SourceData.unitName : "Unknown";

    [Header("실시간 전투 스탯")]
    public int currentHP;        // 현재 체력
    public int currentSpeed;     // 이번 라운드의 현재 속도
    public bool isDead = false;  // 사망 여부 플래그
    public bool isPlayer;        // 아군/적군 판별 플래그

    // 침식 특수 사망에 면역인지 여부 (기본값 false)
    public bool corrosionImmune = false;

    // 체력 고갈, 즉사 기믹, 침식 초과 시 BattleManager에게 알리는 이벤트
    public event Action<UnitControl> OnDeathConditionMet;

    // 진형 내 자신의 인덱스를 캐싱하는 변수 (오직 FormationManager에 의해서만 갱신됨)
    public int positionIndex = -1;

    // 게이지 임계치 초과로 인한 침식 즉사 상태 플래그
    public bool isCorrosioned = false;

    [Header("실시간 속성 수치 관리")]
    public Dictionary<AttributeType, int> currentAttributes = new Dictionary<AttributeType, int>();

    [Header("장착된 스킬")]
    // 기획 룰 적용: 고정 이동기(1) + 필살기(최대 1) + 일반 스킬(최대 4) = 최대 6개 장착
    public List<SkillData> equippedSkills = new List<SkillData>();

    // 의존성 역전을 위한 추상화된 뇌 (BattleManager가 접근할 수 있도록 프로퍼티로 변경)
    public IUnitBrain Brain { get; private set; }

    // 실시간 쿨타임 추적 데이터베이스
    private Dictionary<SkillData, int> skillCooldowns = new Dictionary<SkillData, int>();

    // 런타임에 유닛에게 부착된 살아있는 상태이상(버프/디버프) 객체들을 관리하는 리스트입니다.
    public List<StatusEffectBase> activeEffects = new List<StatusEffectBase>();

    // ========================================================================
    // [1. 초기화 및 기본 로직]
    // ========================================================================

    // 세션 데이터 분리 원칙에 따라 시작 상태(CurrentPartyState)를 구조체 형태로 주입받습니다.
    public void Init(UnitData data, CurrentPartyState startingState, bool isPlayerSide)
    {
        SourceData = data;
        isPlayer = isPlayerSide;
        currentHP = startingState.currentHP;
        isDead = false;

        currentAttributes.Clear();
        skillCooldowns.Clear(); // 쿨타임 초기화
        activeEffects.Clear();  // 초기화 시 상태이상 리스트도 비웁니다.

        // 1. 정적 데이터(Excel)에 선언된 속성 종류를 화이트리스트로 먼저 등록합니다 (속성이 아예 없는 무공 개체 방어)
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

        // 2. 등록된 속성 보유 여부를 검사하여, 유효한 속성에만 장부의 세션 값을 주입합니다 (데이터 오염 방지)
        if (currentAttributes.ContainsKey(AttributeType.YinYang))
        {
            currentAttributes[AttributeType.YinYang] = startingState.yinYangValue;
        }
        if (currentAttributes.ContainsKey(AttributeType.Dream))
        {
            currentAttributes[AttributeType.Dream] = startingState.dreamValue;
        }
    }

    /// <summary>
    /// 유닛의 뇌를 장착합니다. (AI, Human 등 다양한 뇌 교체 가능)
    /// </summary>
    public void SetBrain(IUnitBrain newBrain)
    {
        Brain = newBrain;
        // 장착과 동시에 뇌(Brain)에게 이 유닛의 신체 정보와 스킬 정보를 넘겨줍니다.
        Brain.Initialize(this, equippedSkills);
    }

    // 라운드 시작 시 속도 굴림 함수
    public void RollCurrentSpeed()
    {
        if (SourceData != null)
        {
            currentSpeed = UnityEngine.Random.Range(SourceData.minSpeed, SourceData.maxSpeed + 1);
        }
    }

    /// <summary>
    /// 속성 증감 조작 및 침식 즉사 판정
    /// </summary>
    public void ModifyAttribute(AttributeType type, int amount)
    {
        if (currentAttributes.ContainsKey(type))
        {
            int nextValue = currentAttributes[type] + amount;

            // 즉사 판정 및 강제 고정(Clamp) 로직을 완전히 제거하여, 
            // 턴 시작 전까지 초과/미달 수치를 그대로 유지(세이브 플레이 허용)하도록 해방합니다.
            currentAttributes[type] = nextValue;
        }
    }

    // Phase 2-3. 행동 불가 상태 확인 (음기 침식)
    public bool IsUnableToAct()
    {
        if (currentAttributes.TryGetValue(AttributeType.YinYang, out int yinYangValue))
        {
            // 음기 침식: 수치가 0~10일 경우 행동 1턴 스킵
            if (yinYangValue <= 10) return true;
        }

        // 기절(Stun) 상태이상 체크 로직 추가
        foreach (var effect in activeEffects)
        {
            if (effect.type == EffectType.Stun && !effect.isExpired)
                return true;
        }

        return false;
    }

    // Phase 2-4. 강제 무작위 행동 상태 확인 (양기 침식)
    public bool IsForcedToActRandomly()
    {
        if (currentAttributes.TryGetValue(AttributeType.YinYang, out int yinYangValue))
        {
            // 양기 침식: 수치가 90~100일 경우 제어권 상失 (무작위 행동)
            if (yinYangValue >= 90) return true;
        }
        return false;
    }

    // 스킬 피격 및 상태이상(도트딜) 발동 시 호출되는 통합 데미지 적용 함수
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            // 주의: 여기서 즉시 isDead = true 처리를 하지 않으며 맵에서 지우지도 않습니다!
            // 불사 기믹 및 동시 사망 처리를 위해, 실제 사망 판정 및 청소는 BattleManager(사망 대기열)가 일괄 수행합니다.

            // 체력이 0이 되면 중앙 통제소(BattleManager)의 사망 대기열에 자신을 등록해달라고 요청합니다.
            OnDeathConditionMet?.Invoke(this);
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

    // ========================================================================
    // [3. 전투 자원 및 쿨타임 관리 로직]
    // ========================================================================

    public int GetCooldown(SkillData skill)
    {
        if (skillCooldowns.TryGetValue(skill, out int cooldown)) return cooldown;
        return 0; // 사전에 등록되지 않은 스킬이면 쿨타임 0으로 취급
    }

    public void SetCooldown(SkillData skill, int turns)
    {
        skillCooldowns[skill] = turns;
    }

    public void DecreaseCooldowns()
    {
        List<SkillData> keys = new List<SkillData>(skillCooldowns.Keys);
        foreach (var key in keys)
        {
            if (skillCooldowns[key] > 0) skillCooldowns[key]--;
        }
    }

    // ========================================================================
    // [특수 사망 및 강제 처형 헬퍼]
    // ========================================================================

    /// <summary>
    /// 기믹/스크립트에 의해 대상이 즉사(처형)당할 때 호출됩니다. (유형 4)
    /// </summary>
    public void ForceKill()
    {
        if (isDead) return;
        currentHP = 0;
        OnDeathConditionMet?.Invoke(this);
    }

    /// <summary>
    /// 턴 시작 시 호출되어, 침식 한계 돌파(0 미만, 100 초과)로 인한 특수 사망(유형 3)을 판별합니다.
    /// 만약 조건이 맞다면 이벤트를 발송하고 true를 반환합니다.
    /// </summary>
    public bool CheckAndTriggerErosionDeath()
    {
        // 이미 사망했거나 면역이면 무시합니다.
        if (isDead || corrosionImmune) return false;

        if (currentAttributes.TryGetValue(AttributeType.YinYang, out int yinYangValue))
        {
            if (yinYangValue < 0 || yinYangValue > 100)
            {
                // [업데이트] 침식 특수 사망 이벤트를 송출합니다. (Broadcast 사용)
                BattleLogEvents.BroadcastErosionDeath(this, yinYangValue);
                OnDeathConditionMet?.Invoke(this);
                return true;
            }
        }
        return false;
    }
}