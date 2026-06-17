using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// ====================================================
// [SimulationUIManager.cs]
// 시뮬레이션 씬(A-N)의 사용자 입력을 받고 화면(View)을 갱신하는 UI 전담 매니저입니다.
// 단일 책임 원칙(SRP)에 따라 전투 로직은 전혀 처리하지 않으며,
// 오직 데이터를 파싱하여 화면에 뿌려주고, 입력값을 백엔드로 넘기는 중개자 역할만 수행합니다.
// ====================================================
public class SimulationUIManager : MonoBehaviour
{
    [Header("백엔드 매니저 참조 (의존성 주입)")]
    [SerializeField] private SimulationManager simulationManager;
    [SerializeField] private PartyRoster partyRoster;

    [Header("런타임 제어 패널")]
    [SerializeField] private TMP_InputField inputSimCount;
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnPauseResume;
    [SerializeField] private Button btnStop;

    [Header("엔트리 세팅 프리팹 및 부모")]
    [SerializeField] private Transform playerPanelContainer;
    [SerializeField] private Transform enemyPanelContainer;
    [SerializeField] private GameObject playerPanelPrefab;
    [SerializeField] private GameObject enemyPanelPrefab;

    // 메모리 캐싱 데이터 (어드레서블에서 로드한 전체 원본 리스트)
    private List<UnitData> loadedUnitDataList = new List<UnitData>();
    private List<SkillData> loadedSkillDataList = new List<SkillData>();

    // 런타임에 동적으로 생성된 패널 스크립트들을 보관하는 리스트
    private List<PlayerUnitPanel> activePlayerPanels = new List<PlayerUnitPanel>();
    private List<EnemyUnitPanel> activeEnemyPanels = new List<EnemyUnitPanel>();

    private void Start()
    {
        // 1. 버튼 이벤트 바인딩
        btnStart.onClick.AddListener(OnStartButtonClicked);
        btnPauseResume.onClick.AddListener(OnPauseResumeButtonClicked);
        btnStop.onClick.AddListener(OnStopButtonClicked);

        // 2. 어드레서블 데이터 비동기 로드 시작
        InitializeData();
    }

    /// <summary>
    /// 어드레서블 라벨을 이용하여 SO 데이터들을 비동기 로드합니다.
    /// </summary>
    private void InitializeData()
    {
        // UnitData 로드
        Addressables.LoadAssetsAsync<UnitData>("UnitData", null).Completed += (AsyncOperationHandle<IList<UnitData>> handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedUnitDataList.AddRange(handle.Result);
                // SkillData 로드 (유닛이 완료된 후 스킬 로드)
                LoadSkillData();
            }
            else
            {
                Debug.LogError("<color=red>[UI 매니저] UnitData 어드레서블 로드에 실패했습니다.</color>");
            }
        };
    }

    private void LoadSkillData()
    {
        Addressables.LoadAssetsAsync<SkillData>("SkillData", null).Completed += (AsyncOperationHandle<IList<SkillData>> handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedSkillDataList.AddRange(handle.Result);

                // 데이터가 모두 준비되면 UI 프리팹들을 동적으로 생성합니다.
                SpawnSettingPanels();
            }
            else
            {
                Debug.LogError("<color=red>[UI 매니저] SkillData 어드레서블 로드에 실패했습니다.</color>");
            }
        };
    }

    /// <summary>
    /// 아군 4명, 적군 4명의 세팅 패널을 생성하고 초기화합니다.
    /// 기획 기준(UnitID의 PC, NPC 포함 여부)에 따라 아군과 적군 목록을 분리하여 드롭다운에 전달합니다.
    /// </summary>
    private void SpawnSettingPanels()
    {
        // 1. 유닛 데이터를 아군(PC, NPC)과 적군(나머지)으로 분리
        List<UnitData> playerSideUnits = new List<UnitData>();
        List<UnitData> enemySideUnits = new List<UnitData>();

        foreach (var unit in loadedUnitDataList)
        {
            // UnitData에 선언된 ID 변수명(예: unitID)에 맞게 필터링합니다.
            // 만약 변수명이 다르면 수정이 필요할 수 있습니다.
            if (unit.unitID.Contains("PC") || unit.unitID.Contains("NPC"))
            {
                playerSideUnits.Add(unit);
            }
            else
            {
                enemySideUnits.Add(unit);
            }
        }

        // 2. 아군 패널 4개 생성 (아군 전용 리스트 주입)
        for (int i = 0; i < 4; i++)
        {
            GameObject obj = Instantiate(playerPanelPrefab, playerPanelContainer);
            PlayerUnitPanel panel = obj.GetComponent<PlayerUnitPanel>();
            if (panel != null)
            {
                // 매니저가 중재자(Mediator) 역할을 수행하여 중복 배치를 검증하는 함수를 주입합니다.
                panel.OnValidateUnit = (unit) =>
                {
                    foreach (var p in activePlayerPanels)
                        if (p != panel && p.GetSelectedUnit() == unit) return false;
                    return true;
                };

                panel.Initialize(playerSideUnits); // 분리된 아군 리스트 전달
                activePlayerPanels.Add(panel);
            }
        }

        // 3. 적군 패널 4개 생성 (적군 전용 리스트 주입)
        for (int i = 0; i < 4; i++)
        {
            GameObject obj = Instantiate(enemyPanelPrefab, enemyPanelContainer);
            EnemyUnitPanel panel = obj.GetComponent<EnemyUnitPanel>();
            if (panel != null)
            {
                //시뮬레이션에서 잡몹 중복 선택을 위해 주석처리 했음.
                //// 적군도 동일하게 중복 검증 로직 주입
                //panel.OnValidateUnit = (unit) =>
                //{
                //    foreach (var p in activeEnemyPanels)
                //        if (p != panel && p.GetSelectedUnit() == unit) return false;
                //    return true;
                //};

                panel.Initialize(enemySideUnits); // 분리된 적군 리스트 전달
                activeEnemyPanels.Add(panel);
            }
        }

        Debug.Log($"<color=green>[UI 매니저] 시뮬레이션 UI 패널 세팅 완료! (아군 풀: {playerSideUnits.Count}종, 적군 풀: {enemySideUnits.Count}종)</color>");
    }

    // ========================================================================
    // [UI 상호작용 - 버튼 이벤트]
    // ========================================================================

    private void OnStartButtonClicked()
    {
        if (simulationManager == null || partyRoster == null)
        {
            Debug.LogError("<color=red>[UI 매니저] 매니저 참조가 누락되었습니다.</color>");
            return;
        }

        // 1. 반복 횟수 검증
        if (!int.TryParse(inputSimCount.text, out int simCount) || simCount <= 0)
        {
            Debug.LogWarning("[UI 매니저] 올바른 시뮬레이션 횟수를 입력해 주십시오.");
            return;
        }

        // 2. PartyRoster 세팅 데이터 추출 및 주입
        // (기획자님이 말씀하신 SetPlayerParty, SetEnemyParty, SetUnitSkills 호출 규격 준수)
        List<UnitData> playerList = new List<UnitData>();
        foreach (var p in activePlayerPanels)
        {
            UnitData u = p.GetSelectedUnit();
            if (u != null) playerList.Add(u); // "--- 선택 안함 ---" 필터링
        }

        List<UnitData> enemyList = new List<UnitData>();
        foreach (var e in activeEnemyPanels)
        {
            UnitData u = e.GetSelectedUnit();
            if (u != null) enemyList.Add(u); // "--- 선택 안함 ---" 필터링
        }

        // 양 진영 중 한 곳이라도 아무도 안 골랐다면 전투 거부
        if (playerList.Count == 0 || enemyList.Count == 0)
        {
            Debug.LogWarning("<color=orange>[UI 매니저] 아군과 적군 진형에 각각 최소 1명 이상의 유닛을 배치해야 합니다.</color>");
            return;
        }

        partyRoster.SetPlayerParty(playerList);
        partyRoster.SetEnemyParty(enemyList);

        // 스킬 세팅 주입
        foreach (var p in activePlayerPanels)
        {
            UnitData targetUnit = p.GetSelectedUnit();
            if (targetUnit != null)
            {
                partyRoster.SetUnitSkills(targetUnit, p.GetSelectedMoveSkill(), p.GetSelectedUltSkill(), p.GetSelectedNormalSkills());
            }
        }

        // 3. 전투 엔진 점화
        Debug.Log($"<color=cyan>[UI 매니저] 시뮬레이션 {simCount}회 가동을 시작합니다.</color>");
        simulationManager.StartSimulation(simCount);
    }

    private void OnPauseResumeButtonClicked()
    {
        if (simulationManager != null)
        {
            simulationManager.PauseResumeSimulation();
        }
    }

    private void OnStopButtonClicked()
    {
        if (simulationManager != null)
        {
            simulationManager.StopSimulation();
        }
    }
}