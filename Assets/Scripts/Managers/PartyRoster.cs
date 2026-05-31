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
    /// 주입 시 기획자가 엑셀에 세팅해 둔 NormalSkillPool에서 최대 4개를 자동으로 장착시킵니다.
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

            // 엑셀(CSV)에서 파싱되어 들어온 normalSkillPool 리스트에서 맨 앞부터 최대 4개를 안전하게 가져옵니다.
            if (enemy.normalSkillPool != null)
            {
                autoSkills = enemy.normalSkillPool.Take(4).ToList();
            }

            // 장부에 자동 기록
            SetUnitSkills(enemy, autoSkills);
        }
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