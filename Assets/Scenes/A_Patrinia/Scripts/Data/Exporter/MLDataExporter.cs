using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ====================================================
// [MLDataExporter.cs]
// 파이썬 비지도 학습(Clustering) 모델의 입력 데이터(Feature Matrix)를 생성하는 전담 수집가입니다.
// 플레이어의 행동 패턴을 0.0 ~ 1.0 스케일의 연속형 변수로 정규화하여 평탄화된 CSV를 추출합니다.
// ====================================================
public class MLDataExporter : MonoBehaviour
{
    [Header("머신러닝 파일 제어")]
    // [오류 사후 보수] 개별 실행 세트 독립 분리 법칙 준수를 위해, const 대신 런타임 동적 변수 구조로 개편합니다.
    private string activeCsvPath;
    private int currentSimId = 0;

    // ==========================================
    // [런타임 피처 수집 장부 (1회차 전투용)]
    // ==========================================
    private int totalRounds = 0; // 정규화(Ratio) 계산을 위한 총 라운드 수 장부 추가

    // Operating Features (운영 지표)
    private int totalPlayerSkills = 0;
    private Dictionary<TendencyType, int> skillTendencyCounts = new Dictionary<TendencyType, int>();
    private List<float> roundEndYinYangAverages = new List<float>();

    // Tactical Features (전술 지표)
    private int playerTotalTurns = 0;
    private int playerSkippedTurns = 0;
    private int playerCorrosionReverts = 0;

    // ========================================================================
    // [1. 초기화 및 스트림 오픈 요청]
    // ========================================================================
    // 유니티 내 단 1회성 제약인 Start()를 전면 폐기하고, 거시 세션 수명주기 메서드로 로직을 완전히 이동시켰습니다.

    // ========================================================================
    // [2. 이벤트 구독 (옵저버 패턴)]
    // ========================================================================
    private void OnEnable()
    {
        //  연속 실행 시 생명주기 통제를 위한 거시 세션 이벤트 구독 추가
        BattleLogEvents.OnSimulationSessionStarted += HandleSimulationSessionStarted;
        BattleLogEvents.OnSimulationSessionEnded += HandleSimulationSessionEnded;

        BattleLogEvents.OnBattleStarted += HandleBattleStarted;
        BattleLogEvents.OnBattleEnded += HandleBattleEnded;
        BattleLogEvents.OnRoundStarted += HandleRoundStarted;
        BattleLogEvents.OnRoundEnded += HandleRoundEnded;
        BattleLogEvents.OnTurnStarted += HandleTurnStarted;
        BattleLogEvents.OnTurnSkipped += HandleTurnSkipped;
        BattleLogEvents.OnSkillCasted += HandleSkillCasted;
        BattleLogEvents.OnCorrosionReverted += HandleCorrosionReverted;
    }

    private void OnDisable()
    {
        BattleLogEvents.OnSimulationSessionStarted -= HandleSimulationSessionStarted;
        BattleLogEvents.OnSimulationSessionEnded -= HandleSimulationSessionEnded;

        BattleLogEvents.OnBattleStarted -= HandleBattleStarted;
        BattleLogEvents.OnBattleEnded -= HandleBattleEnded;
        BattleLogEvents.OnRoundStarted -= HandleRoundStarted;
        BattleLogEvents.OnRoundEnded -= HandleRoundEnded;
        BattleLogEvents.OnTurnStarted -= HandleTurnStarted;
        BattleLogEvents.OnTurnSkipped -= HandleTurnSkipped;
        BattleLogEvents.OnSkillCasted -= HandleSkillCasted;
        BattleLogEvents.OnCorrosionReverted -= HandleCorrosionReverted;
    }

    // ========================================================================
    // [신규 업데이트: 세션 제어 및 실행 세트 파일 분리 시스템]
    // ========================================================================
    private void HandleSimulationSessionStarted()
    {
        // 1. 기획 규칙 반영: 새로운 시뮬레이션 세트 실행 시 고유 식별자(Sim_ID)를 다시 1번부터 카운팅하도록 초기화
        currentSimId = 0;

        // 2. 파일 분리 격리 법칙: 씬을 끄지 않고 연속 가동하더라도 중복 파일 충돌(Sharing violation)을 차단하도록 고유 시분초 식별값 결합
        string uniqueSessionTimeStamp = DateTime.Now.ToString("HHmmss");
        activeCsvPath = $"MLCluster/ML_Feature_Dataset_{uniqueSessionTimeStamp}.csv";

        // 스피드/늪지대형 페르소나 분별을 위해 Total_Rounds 피처를 CSV 헤더에 명시적으로 추가
        string csvHeader = "Sim_ID,Total_Rounds,Alive_Ratio,Remaining_HP_Ratio,YinYang_Deviation," +
                           "Skill_Aggressive,Skill_Heal,Skill_Utility,Skill_Defensive," +
                           "Turn_Skip_Ratio,Corrosion_Revert_Ratio";

        if (SimulationLogManager.Instance != null)
        {
            SimulationLogManager.Instance.InitializeStream(activeCsvPath, csvHeader);
        }
        else
        {
            Debug.LogError("[MLDataExporter] SimulationLogManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    private void HandleSimulationSessionEnded()
    {
        if (SimulationLogManager.Instance != null && !string.IsNullOrEmpty(activeCsvPath))
        {
            SimulationLogManager.Instance.CloseStream(activeCsvPath);
        }
    }

    // ========================================================================
    // [3. 런타임 데이터 축적부]
    // ========================================================================
    private void HandleBattleStarted()
    {
        currentSimId++;

        // 피처 장부 초기화
        totalRounds = 0; // [업데이트] 매 전투 시작마다 총 라운드 수 초기화
        totalPlayerSkills = 0;
        playerTotalTurns = 0;
        playerSkippedTurns = 0;
        playerCorrosionReverts = 0;
        skillTendencyCounts.Clear();
        roundEndYinYangAverages.Clear();
    }

    // [업데이트] 라운드가 시작될 때마다 총 라운드 수를 1씩 증가시키는 헬퍼 이벤트 수신부 추가
    private void HandleRoundStarted(int round)
    {
        totalRounds++;
    }

    private void HandleSkillCasted(UnitControl caster, UnitControl target, SkillData skill)
    {
        // 머신러닝의 분석 대상은 '플레이어'이므로 적군의 행동은 집계하지 않습니다.
        if (!caster.isPlayer) return;

        totalPlayerSkills++;

        foreach (var tag in skill.skillTendencies)
        {
            if (!skillTendencyCounts.ContainsKey(tag)) skillTendencyCounts[tag] = 0;
            skillTendencyCounts[tag]++;
        }
    }

    private void HandleRoundEnded(int round)
    {
        if (BattleManager.Instance == null) return;

        var alivePlayers = BattleManager.Instance.allUnits.Where(u => u.isPlayer && !u.isDead).ToList();
        if (alivePlayers.Count == 0) return;

        // 라운드 종료 시점의 생존 아군 음양 수치 평균을 구하여, 50(중립)으로부터의 편차를 기록합니다.
        float avgYinYang = 0f;
        foreach (var player in alivePlayers)
        {
            if (player.currentAttributes.TryGetValue(AttributeType.YinYang, out int yy))
            {
                avgYinYang += yy;
            }
            else
            {
                avgYinYang += 50f;
            }
        }
        avgYinYang /= alivePlayers.Count;

        // 50으로부터 얼마나 벗어났는지(절댓값)를 기록 (Gauge_Stability 지표)
        roundEndYinYangAverages.Add(Mathf.Abs(avgYinYang - 50f));
    }

    private void HandleTurnStarted(UnitControl unit)
    {
        if (unit.isPlayer) playerTotalTurns++;
    }

    private void HandleTurnSkipped(UnitControl unit, string reason)
    {
        if (unit.isPlayer) playerSkippedTurns++;
    }

    private void HandleCorrosionReverted(UnitControl unit, string stateName)
    {
        if (unit.isPlayer) playerCorrosionReverts++;
    }

    // ========================================================================
    // [4. 피처 평탄화(Flattening) 및 파일 스트리밍]
    // ========================================================================
    private void HandleBattleEnded()
    {
        // 1. Result Features (생존 성과) 산출
        CalculateResultFeatures(out float aliveRatio, out float hpRatio);

        // 2. Operating Features (운영 지표) 산출
        CalculateOperatingFeatures(out float yyDeviation, out float aggRatio, out float healRatio, out float utilRatio, out float defRatio);

        // 3. Tactical Features (전술 지표) 산출
        CalculateTacticalFeatures(out float skipRatio, out float corrosionRevertRatio);

        // CSV 포맷 조립 (스파게티 코드 방지를 위해 소수점 통일)
        // CSV 데이터 로우에 totalRounds 값을 두 번째 칼럼으로 삽입하여 파이썬 측에서 활용할 수 있게 합니다.
        string row = $"{currentSimId},{totalRounds},{aliveRatio:F3},{hpRatio:F3},{yyDeviation:F1}," +
                     $"{aggRatio:F3},{healRatio:F3},{utilRatio:F3},{defRatio:F3}," +
                     $"{skipRatio:F3},{corrosionRevertRatio:F3}";

        // I/O 매니저에게 쓰기 위임 (동적 고유 파일 경로 격리 적용)
        if (SimulationLogManager.Instance != null && !string.IsNullOrEmpty(activeCsvPath))
        {
            SimulationLogManager.Instance.WriteRecord(activeCsvPath, row);
        }
    }

    // ========================================================================
    // [내부 헬퍼 메서드 - OCP 및 SRP 준수]
    // ========================================================================
    private void CalculateResultFeatures(out float aliveRatio, out float hpRatio)
    {
        aliveRatio = 0f;
        hpRatio = 0f;

        if (BattleManager.Instance == null) return;

        var players = BattleManager.Instance.allUnits.Where(u => u.isPlayer).ToList();
        if (players.Count == 0) return;

        int aliveCount = players.Count(u => !u.isDead);
        aliveRatio = (float)aliveCount / players.Count;

        float maxHpSum = players.Sum(u => u.SourceData != null ? u.SourceData.maxHP : 1);
        float curHpSum = players.Where(u => !u.isDead).Sum(u => u.currentHP);
        hpRatio = maxHpSum > 0 ? curHpSum / maxHpSum : 0f;
    }

    private void CalculateOperatingFeatures(out float yyDeviation, out float aggRatio, out float healRatio, out float utilRatio, out float defRatio)
    {
        yyDeviation = roundEndYinYangAverages.Count > 0 ? roundEndYinYangAverages.Average() : 0f;

        aggRatio = GetTendencyRatio(TendencyType.Aggressive);
        healRatio = GetTendencyRatio(TendencyType.Heal);
        utilRatio = GetTendencyRatio(TendencyType.Utility);
        defRatio = GetTendencyRatio(TendencyType.Defensive);
    }

    private void CalculateTacticalFeatures(out float skipRatio, out float corrosionRevertRatio)
    {
        // 턴 스킵 비율 (스턴, 행동불능 침식 등에 얼마나 노출되었는가를 0.0~1.0으로 표현)
        skipRatio = playerTotalTurns > 0 ? (float)playerSkippedTurns / playerTotalTurns : 0f;

        // 절대 횟수에서 총 라운드 수 대비 비율로 정규화 (0.0~1.0 스케일)
        corrosionRevertRatio = totalRounds > 0 ? (float)playerCorrosionReverts / totalRounds : 0f;
    }

    private float GetTendencyRatio(TendencyType type)
    {
        if (totalPlayerSkills == 0) return 0f;
        return skillTendencyCounts.TryGetValue(type, out int count) ? (float)count / totalPlayerSkills : 0f;
    }
}