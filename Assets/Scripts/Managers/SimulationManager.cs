using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ====================================================
// [SimulationManager.cs]
// 시뮬레이션 씬(Sim_Scene)의 시간(배속)과 반복 횟수를 지배하는 최고 관리자입니다.
// BattleManager를 부품으로 사용하여 수만 번의 전투를 자동화합니다.
// ====================================================
public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("시뮬레이션 상태")]
    public int targetSimulations = 10000;
    public int currentSimulations = 0;
    public bool isRunning = false;
    public bool isPaused = false;

    [Header("시뮬레이션 환경 설정")]
    [Tooltip("시뮬레이션 가동 시의 게임 배속 (예: 50배속)")]
    public float simulationTimeScale = 50f;

    [Header("통계 데이터")]
    public int winCount = 0;
    public int loseCount = 0;
    public int drawCount = 0;

    // 현재 진행 중인 전투가 끝났는지 체크하는 플래그 (이벤트로 제어됨)
    private bool isCurrentBattleFinished = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // [최적화] 시뮬레이션 환경에서는 프레임 제한을 풀고 수직동기화를 끕니다. (Headless 연산 최적화)
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;

        // BattleManager의 전투 종료 방송(이벤트)을 구독(Subscribe)합니다.
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleEnded += HandleBattleEnded;
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위해 씬이 파괴될 때 구독을 해제합니다.
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnBattleEnded -= HandleBattleEnded;
        }
    }

    // ========================================================================
    // [외부 제어 API (UI 버튼 등에서 호출 예정)]
    // ========================================================================

    /// <summary>
    /// 지정된 횟수만큼 자동 반복 전투를 시작합니다.
    /// </summary>
    public void StartSimulation(int count)
    {
        if (isRunning) return;

        targetSimulations = count;
        currentSimulations = 0;
        winCount = 0;
        loseCount = 0;
        drawCount = 0;

        isRunning = true;
        isPaused = false;
        Time.timeScale = simulationTimeScale; // 초고속 배속 진입

        StartCoroutine(SimulationLoop());
    }

    public void PauseSimulation()
    {
        if (!isRunning || isPaused) return;
        isPaused = true;
        Time.timeScale = 0f; // 시간 정지 (유니티 코루틴 및 물리 연산 일시정지)
    }

    public void ResumeSimulation()
    {
        if (!isRunning || !isPaused) return;
        isPaused = false;
        Time.timeScale = simulationTimeScale; // 초고속 배속 복구
    }

    public void StopSimulation()
    {
        if (!isRunning) return;

        StopAllCoroutines();
        isRunning = false;
        isPaused = false;
        Time.timeScale = 1f; // 원래 속도로 복구

        Debug.Log("<color=red>[SimulationManager] 시뮬레이션이 강제 중단되었습니다.</color>");
    }

    // ========================================================================
    // [코어 자동화 루프]
    // ========================================================================

    private IEnumerator SimulationLoop()
    {
        Debug.Log($"<color=green>[SimulationManager] {targetSimulations}회 시뮬레이션 가동 시작.</color>");

        while (currentSimulations < targetSimulations && isRunning)
        {
            isCurrentBattleFinished = false;

            // 1. 유닛 세팅 및 초기화 (추후 만들어질 SpawnManager에게 위임)
            // List<UnitControl> unitsForThisBattle = SpawnManager.Instance.SetupUnitsForSimulation();

            // [임시 방어 코드] SpawnManager가 아직 없으므로, BattleManager에 하드코딩된 리스트를 재사용합니다.
            // (실제로는 여기서 체력을 100%로 꽉 채운 건강한 8명의 유닛 리스트를 받아와야 합니다.)
            List<UnitControl> unitsForThisBattle = BattleManager.Instance.allUnits;

            // 2. 엔진 점화!
            BattleManager.Instance.StartBattle(unitsForThisBattle);

            // 3. BattleManager가 전투 종료 이벤트를 쏠 때까지 루프를 잠시 멈추고 대기
            yield return new WaitUntil(() => isCurrentBattleFinished);

            // 4. 전투 결과 집계
            RecordResult(unitsForThisBattle);

            currentSimulations++;

            // UI 업데이트 및 안전장치를 위해 1프레임 양보
            yield return null;
        }

        // 모든 반복이 정상적으로 끝났을 때
        if (currentSimulations >= targetSimulations)
        {
            Debug.Log($"<color=cyan>[SimulationManager] 시뮬레이션 완료! (승: {winCount}, 패: {loseCount}, 무: {drawCount})</color>");
            StopSimulation();
        }
    }

    // ========================================================================
    // [이벤트 수신 및 집계]
    // ========================================================================

    /// <summary>
    /// BattleManager의 OnBattleEnded 방송을 들었을 때 실행되는 콜백 함수입니다.
    /// </summary>
    private void HandleBattleEnded()
    {
        // 플래그를 true로 바꾸어 SimulationLoop 코루틴의 WaitUntil 잠금을 해제합니다.
        isCurrentBattleFinished = true;
    }

    /// <summary>
    /// 살아남은 유닛을 분석하여 아군의 승패를 기록합니다.
    /// </summary>
    private void RecordResult(List<UnitControl> units)
    {
        bool playerAlive = units.Any(u => u.isPlayer && !u.isDead);
        bool enemyAlive = units.Any(u => !u.isPlayer && !u.isDead);

        if (playerAlive && !enemyAlive) winCount++;
        else if (!playerAlive && enemyAlive) loseCount++;
        else drawCount++; // 양측 모두 전멸 등의 무승부 상황
    }
}