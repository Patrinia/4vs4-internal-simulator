using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Take() 메서드 사용을 위함

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

    // [업데이트] 세션(탐색) 동안 유지되어야 할 유닛의 현재 체력을 보관하는 장부 (세션 데이터 분리)
    private Dictionary<UnitData, int> rosterHP = new Dictionary<UnitData, int>();

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
    // [세션 데이터 (HP) 관리 API (신규)]
    // ========================================================================

    /// <summary>
    /// 시뮬레이션 시작 또는 새로운 탐색(밤 정비 완료 후) 시 모든 유닛의 체력을 Max로 초기화합니다.
    /// </summary>
    public void ResetAllHP()
    {
        rosterHP.Clear();
        foreach (var unit in playerParty)
        {
            if (unit != null) rosterHP[unit] = unit.maxHP;
        }
        foreach (var unit in enemyParty)
        {
            if (unit != null) rosterHP[unit] = unit.maxHP;
        }
        Debug.Log("<color=green>[PartyRoster] 모든 출전 유닛의 체력 장부가 MaxHP로 리셋되었습니다.</color>");
    }

    /// <summary>
    /// 전투 종료 시 살아남은 유닛의 체력을 장부에 저장(덮어쓰기)하여 연전 시 유지되도록 합니다.
    /// </summary>
    public void UpdateUnitHP(UnitData unit, int currentHP)
    {
        if (unit != null) rosterHP[unit] = currentHP;
    }

    /// <summary>
    /// SpawnManager가 육체를 스폰할 때 해당 유닛의 남은 체력을 조회합니다.
    /// </summary>
    public int GetUnitHP(UnitData unit)
    {
        if (unit != null && rosterHP.TryGetValue(unit, out int hp))
        {
            return hp;
        }
        // 장부에 기록이 없으면 기본적으로 해당 유닛의 최대 체력을 반환합니다.
        return unit != null ? unit.maxHP : 0;
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