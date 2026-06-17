using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ====================================================
// [SimulationManager.cs]
// 시뮬레이션 씬(Simulation)의 시간(배속)과 반복 횟수를 지배하는 최고 관리자입니다.
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

    // 100라운드를 초과하여 강제 무승부 처리된 타임아웃 횟수 추적
    public int timeoutCount = 0;

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

        // [업데이트] 매니저 파괴 시 싱글톤 참조 해제 (좀비 참조 방지)
        if (Instance == this) Instance = null;
    }

    // ========================================================================
    // [외부 제어 API (UI 버튼 등에서 호출 예정)]
    // ========================================================================

    /// <summary>
    /// 지정된 횟수만큼 자동 반복 전투를 시작합니다.
    /// </summary>
    public void StartSimulation(int count)
    {
        // [업데이트] 수집가들이 예외 크래시로 멈췄을 때 시스템 데드락 상태(isRunning이 true로 고착됨)를 방지하는 Fail-safe 처리
        if (isRunning)
        {
            StopSimulation();
        }

        targetSimulations = count;
        currentSimulations = 0;
        winCount = 0;
        loseCount = 0;
        drawCount = 0;
        timeoutCount = 0; // 타임아웃 횟수 초기화

        isRunning = true;
        isPaused = false;
        Time.timeScale = simulationTimeScale; // 초고속 배속 진입

        // [업데이트] 실행 세트 독립화 법칙에 의거, 새로운 세션의 완전한 시작을 알림
        BattleLogEvents.BroadcastSimulationSessionStarted();

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

    // UI의 단일 일시정지/재개 버튼과 통신하기 위한 토글(Toggle) 수신 API입니다.
    public void PauseResumeSimulation()
    {
        if (isPaused)
        {
            ResumeSimulation();
        }
        else
        {
            PauseSimulation();
        }
    }

    public void StopSimulation()
    {
        if (!isRunning) return;

        // [업데이트] 임의 중단 시에도 열려 있는 파일 스트림을 Graceful하게 닫기 위해 세션 종료 방송 선행 호출
        BattleLogEvents.BroadcastSimulationSessionEnded();

        StopAllCoroutines();
        isRunning = false;
        isPaused = false;
        Time.timeScale = 1f; // 원래 속도로 복구

        // 전투 매니저(하청업체)에게도 강제 종료 명령을 하달하여 고아 코루틴(Orphan Coroutine) 방지
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StopBattle();
        }

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

            // 1. 유닛 세팅 및 초기화 (임시 하드코딩 제거 및 매니저 체인 연동)
            // 매 시뮬레이션 루프마다 장부의 체력과 속성값을 초기값으로 갱신하여 독립된 평행 우주를 만듭니다.
            PartyRoster.Instance.ResetAllStates();

            // SpawnManager에게 아군/적군 명단을 넘겨주고, 스킬과 뇌가 장착된 최대 8명의 육체를 받아옵니다.
            List<UnitControl> unitsForThisBattle = SpawnManager.Instance.SetupUnitsForSimulation(
                PartyRoster.Instance.playerParty,
                PartyRoster.Instance.enemyParty
            );

            // 2. 엔진 점화!
            // 시뮬레이션 환경에 맞춰 최대 100라운드의 제한 규칙을 명시적으로 주입합니다. (OCP 준수)
            BattleManager.Instance.StartBattle(unitsForThisBattle, 100);

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
            // 타임아웃 횟수를 시뮬레이션 최종 완료 로그에 포함합니다.
            Debug.Log($"<color=cyan>[SimulationManager] 시뮬레이션 완료! (승: {winCount}, 패: {loseCount}, 무: {drawCount}, 타임아웃: {timeoutCount})</color>");
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
        else
        {
            drawCount++; // 양측 모두 전멸 등의 무승부 상황

            // 무승부이면서 양측 모두 살아있다면 100라운드 초과(Timeout)로 간주하고 기록합니다.
            if (playerAlive && enemyAlive) timeoutCount++;
        }
    }
}