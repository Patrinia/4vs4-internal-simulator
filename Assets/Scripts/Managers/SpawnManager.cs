using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [SpawnManager.cs]
// 유닛의 물리적 생성(Instantiate), 오브젝트 풀링(재사용), 그리고
// 매 전투 진입 시 건강한 상태(HP 100%, 쿨타임 초기화)로 
// 육체를 리셋하는 작업을 전담하는 매니저입니다.
// ====================================================
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("오브젝트 풀링 (시뮬레이션 최적화 핵심)")]
    // 매번 Instantiate/Destroy를 반복하면 유니티 메모리(GC)에 과부하가 오므로,
    // 한 번 생성한 유닛 객체를 리스트에 담아두고 데이터만 덮어씌워 재사용합니다.
    private List<UnitControl> playerPool = new List<UnitControl>();
    private List<UnitControl> enemyPool = new List<UnitControl>();

    [Header("하이어라키 정리 (선택)")]
    public Transform playerParent;
    public Transform enemyParent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// SimulationManager가 새로운 전투를 시작할 때마다 호출하여, 
    /// 건강한 상태의 8명 유닛 리스트를 받아가는 핵심 함수입니다.
    /// </summary>
    /// <param name="playerDatas">PartySettingManager가 넘겨준 아군 데이터</param>
    /// <param name="enemyDatas">PartySettingManager가 넘겨준 적군 데이터</param>
    public List<UnitControl> SetupUnitsForSimulation(List<UnitData> playerDatas, List<UnitData> enemyDatas)
    {
        List<UnitControl> readyUnits = new List<UnitControl>();

        // 1. 아군 세팅 (0~3번 슬лот)
        for (int i = 0; i < playerDatas.Count; i++)
        {
            UnitControl unit = GetOrCreateUnit(playerDatas[i], true, i);
            readyUnits.Add(unit);
        }

        // 2. 적군 세팅 (0~3번 슬лот)
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            UnitControl unit = GetOrCreateUnit(enemyDatas[i], false, i);
            readyUnits.Add(unit);
        }

        return readyUnits;
    }

    /// <summary>
    /// 풀(Pool)에서 유닛을 꺼내거나 없으면 새로 생성한 뒤, 초기값을 주입합니다.
    /// </summary>
    private UnitControl GetOrCreateUnit(UnitData data, bool isPlayer, int poolIndex)
    {
        List<UnitControl> targetPool = isPlayer ? playerPool : enemyPool;
        UnitControl unit = null;

        // [최적화 로직] 풀에 이미 만들어진 유닛 껍데기가 있다면 재사용합니다.
        if (poolIndex < targetPool.Count)
        {
            unit = targetPool[poolIndex];
        }
        else
        {
            // 풀에 없으면 최초 1회 생성 (Instantiate)
            GameObject go;
            if (data.unitPrefab != null)
            {
                go = Instantiate(data.unitPrefab, isPlayer ? playerParent : enemyParent);
            }
            else
            {
                // 프리팹이 없다면 빈 오브젝트로 임시 생성 (Headless 시뮬레이션용)
                go = new GameObject($"{data.unitName}_SimObject");
                go.transform.SetParent(isPlayer ? playerParent : enemyParent);
                go.AddComponent<UnitControl>();
            }

            unit = go.GetComponent<UnitControl>();
            targetPool.Add(unit);
        }

        // ==========================================================
        // [핵심 리셋 로직] 전투 시작 전 육체와 뇌를 완벽하게 초기화
        // ==========================================================

        // [업데이트] 장부(PartyRoster)에서 세션 상태(체력 및 속성) 전반을 조회하여 주입하고, 쿨타임 초기화 및 사망(isDead) 플래그 해제
        CurrentPartyState startingState = PartyRoster.Instance.GetUnitState(data);
        unit.Init(data, startingState, isPlayer);

        // 2. 임시 하드코딩 삭제 -> PartyRoster 장부에서 배정된 스킬을 꺼내어 장착
        unit.equippedSkills.Clear();
        List<SkillData> assignedSkills = PartyRoster.Instance.GetEquippedSkills(data);
        if (assignedSkills != null)
        {
            unit.equippedSkills.AddRange(assignedSkills);
        }

        // 3. 뇌(Brain) 새로 장착 (기존의 오염된 기억을 날리고 팩토리에서 새 뇌를 발급)
        IUnitBrain newBrain = BrainFactory.CreateBrain(data.defaultAIBrainType);
        unit.SetBrain(newBrain);

        return unit;
    }
}