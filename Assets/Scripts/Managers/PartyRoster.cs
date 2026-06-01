using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ====================================================
// [CurrentPartyState]
// 전투 간 인계될 유닛의 실시간 생존 및 속성 게이지 상태를 보관하는 구조체입니다.
// ====================================================
[System.Serializable]
public struct CurrentPartyState
{
    public int currentHP;
    public int yinYangValue;
    public int dreamValue;
}

// ====================================================
// [PartyRoster.cs]
// 전투에 출전할 유닛의 명단(Data)과 그들이 장착한 스킬 세팅을
// 순수하게 보관하고 제공하는 데이터 장부(Model)입니다.
// ====================================================
public class PartyRoster : MonoBehaviour
{
    public static PartyRoster Instance { get; private set; }

    [Header("출전 명단 (Data)")]
    public List<UnitData> playerParty = new List<UnitData>();
    public List<UnitData> enemyParty = new List<UnitData>();

    // 특정 유닛 데이터(Key)가 장착하기로 한 스킬 리스트(Value)를 저장하는 장부
    private Dictionary<UnitData, List<SkillData>> equippedSkillsDictionary = new Dictionary<UnitData, List<SkillData>>();

    // 세션(탐색) 동안 유지되어야 할 유닛의 현재 상태 전반을 보관하는 장부
    private Dictionary<UnitData, CurrentPartyState> rosterStates = new Dictionary<UnitData, CurrentPartyState>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ========================================================================
    // [명단 주입 API (UI 또는 씬 전환 매니저에서 호출)]
    // ========================================================================

    /// <summary>
    /// 외부(탐색 씬, 시뮬 UI)에서 플레이어 출전 명단을 주입합니다.
    /// 플레이어는 UI에서 스킬을 별도로 세팅하므로 자동 할당을 하지 않습니다.
    /// </summary>
    public void SetPlayerParty(List<UnitData> roster)
    {
        if (roster == null) return;
        playerParty = new List<UnitData>(roster);
        Debug.Log($"<color=cyan>[PartyRoster] 플레이어 명단 {playerParty.Count}명 등록 완료.</color>");
    }

    /// <summary>
    /// 외부에서 적군 출전 명단을 주입합니다. 
    /// 주입 시 기획 규칙(이동기1+필살기1+일반최대4)에 맞춰 스킬을 자동으로 장착시킵니다.
    /// </summary>
    public void SetEnemyParty(List<UnitData> roster)
    {
        if (roster == null) return;
        enemyParty = new List<UnitData>(roster);
        Debug.Log($"<color=magenta>[PartyRoster] 적군 명단 {enemyParty.Count}명 등록 완료. 스킬 자동 할당을 시작합니다.</color>");

        foreach (UnitData enemy in enemyParty)
        {
            if (enemy == null) continue;

            List<SkillData> autoSkills = new List<SkillData>();

            // [업데이트] 합의된 기획 룰 적용: 이동기 1개 + 필살기 1개 + 일반 스킬 최대 4개 추출
            if (enemy.movementSkill != null)
            {
                autoSkills.Add(enemy.movementSkill);
            }

            if (enemy.ultimateSkillPool != null && enemy.ultimateSkillPool.Count > 0)
            {
                // 고정 패턴의 적군은 첫 번째 필살기를 장착
                autoSkills.Add(enemy.ultimateSkillPool[0]);
            }

            if (enemy.normalSkillPool != null)
            {
                autoSkills.AddRange(enemy.normalSkillPool.Take(4));
            }

            // 장부에 자동 기록
            SetUnitSkills(enemy, autoSkills);
        }
    }

    // ========================================================================
    // [세션 데이터 관리 API (업데이트)]
    // ========================================================================

    /// <summary>
    /// 시뮬레이션 시작 또는 새로운 탐색(밤 정비 완료 후) 시 모든 유닛의 상태를 초기화합니다.
    /// 기본값은 MaxHP, 음양 기준점(50), 꿈 게이지 기본값(0)으로 정돈됩니다.
    /// </summary>
    public void ResetAllStates()
    {
        rosterStates.Clear();
        foreach (var unit in playerParty)
        {
            if (unit != null)
            {
                rosterStates[unit] = new CurrentPartyState
                {
                    currentHP = unit.maxHP,
                    yinYangValue = 50,
                    dreamValue = 0
                };
            }
        }
        foreach (var unit in enemyParty)
        {
            if (unit != null)
            {
                rosterStates[unit] = new CurrentPartyState
                {
                    currentHP = unit.maxHP,
                    yinYangValue = 50,
                    dreamValue = 0
                };
            }
        }
        Debug.Log("<color=green>[PartyRoster] 모든 출전 유닛의 연전 계승 상태 장부가 초기값으로 리셋되었습니다.</color>");
    }

    /// <summary>
    /// 전투 종료 시 살아남은 유닛의 누적 상태(HP 및 속성 게이지)를 장부에 저장(덮어쓰기)합니다.
    /// </summary>
    public void UpdateUnitState(UnitData unit, CurrentPartyState currentState)
    {
        if (unit != null) rosterStates[unit] = currentState;
    }

    /// <summary>
    /// SpawnManager가 육체를 스폰할 때 해당 유닛의 가변 상태 기록을 조회합니다.
    /// </summary>
    public CurrentPartyState GetUnitState(UnitData unit)
    {
        if (unit != null && rosterStates.TryGetValue(unit, out CurrentPartyState state))
        {
            return state;
        }

        // 장부에 기록이 없으면 기본 정적 데이터를 기반으로 안전하게 초기 상태를 생성하여 반환합니다.
        CurrentPartyState defaultState = new CurrentPartyState();
        if (unit != null)
        {
            defaultState.currentHP = unit.maxHP;
            defaultState.yinYangValue = 50;
            defaultState.dreamValue = 0;
        }
        return defaultState;
    }

    // ========================================================================
    // [스킬 세팅 및 조회 API]
    // ========================================================================

    /// <summary>
    /// 특정 유닛에게 명시적으로 스킬을 세팅합니다. 
    /// (플레이어 UI 스킬 세팅 또는 향후 ML 보스의 스킬 오버라이드 시 사용)
    /// </summary>
    public void SetUnitSkills(UnitData unit, List<SkillData> skills)
    {
        if (unit == null || skills == null) return;
        equippedSkillsDictionary[unit] = new List<SkillData>(skills);
    }

    /// <summary>
    /// SpawnManager가 육체를 생성할 때, 해당 유닛이 무슨 스킬을 장착해야 하는지 물어보는 함수입니다.
    /// </summary>
    public List<SkillData> GetEquippedSkills(UnitData unit)
    {
        // 장부에 명단이 있다면 해당 스킬 리스트를 반환
        if (unit != null && equippedSkillsDictionary.TryGetValue(unit, out List<SkillData> skills))
        {
            return skills;
        }

        // 장부에 없다면 빈 리스트를 반환하여 NullReference 에러를 방지합니다.
        return new List<SkillData>();
    }
}