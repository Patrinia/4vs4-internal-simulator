using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ====================================================
// [BattleManager.cs] 
// 전투의 시작, 턴 라이프사이클, 진형 관리, 그리고 최종 승패를
// 총괄하는 게임의 오케스트라 지휘자(Orchestrator) 클래스입니다.
// ====================================================
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("SYSTEM 의존성 모음 (순수 C# 전문가들)")]
    private ITurnSorter turnSorter;
    private CombatResolver combatResolver;
    private StatusEffectManager statusManager;

    // 1차원 진형 체스판을 관리하는 매니저
    public FormationManager FormationManager { get; private set; }

    [Header("전투 데이터")]
    public List<UnitControl> allUnits = new List<UnitControl>(); // 전투에 참여하는 모든 유닛 리스트
    private Queue<UnitControl> turnQueue = new Queue<UnitControl>(); // 이번 라운드의 턴 대기열
    private int currentRound = 0; // 현재 라운드 수

    // 힐/무적기 과다로 인한 무한 루프를 방지하는 라운드 제한 (외부에서 주입됨. 0 이하면 무제한)
    private int currentMaxRounds = 100;

    // 지연된 사망 처리를 위한 중앙 대기열 (중복 방지를 위해 HashSet 사용)
    private HashSet<UnitControl> deathQueue = new HashSet<UnitControl>();

    // 전투가 완전히 종료되었을 때 외부(SimulationManager 등)에 알리는 방송국(이벤트)
    public event Action OnBattleEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 순수 C# 전문가 클래스들 초기화
        turnSorter = new SpeedTurnSorter();

        // 의존성 주입을 위해 진형 매니저, 상태이상매니저를 먼저 만듦
        // CombatResolver에게 상태이상/진형 매니저를 모두 넘겨줍니다.
        statusManager = new StatusEffectManager();
        FormationManager = new FormationManager();
        combatResolver = new CombatResolver(statusManager, FormationManager);
    }

    // [업데이트] 매니저 파괴 시 싱글톤 참조 해제 및 이벤트 구독 정리 (좀비 참조 방지)
    private void OnDestroy()
    {
        CleanupEventSubscriptions();
        if (Instance == this) Instance = null;
    }

    // 기존의 Start() 내부 자동 실행 로직을 제거했습니다. 
    // 이제 BattleManager는 스스로 전투를 시작하지 않습니다. (수동 점화 대기)

    // ========================================================================
    // [외부 제어 API]
    // ========================================================================

    /// <summary>
    /// 외부 매니저(SimulationManager, GameFlowManager)가 호출하는 수동 점화 스위치입니다.
    /// maxRounds 파라미터를 통해 전투의 최대 길이를 외부에서 주입받습니다.
    /// </summary>
    /// <param name="participatingUnits">SpawnManager가 세팅을 마친 이번 전투의 참여 유닛 전체 리스트</param>
    /// <param name="maxRounds">전투 강제 종료 기준 라운드 (기본값 100, 0 이하 입력 시 무제한)</param>
    public void StartBattle(List<UnitControl> participatingUnits, int maxRounds = 100)
    {
        // 1. 전투 데이터 및 규칙 초기화
        allUnits = new List<UnitControl>(participatingUnits);
        currentRound = 0;
        currentMaxRounds = maxRounds; // 외부 규칙 주입
        turnQueue.Clear();
        deathQueue.Clear(); // 사망 대기열 초기화

        // 2. 진형 배치 및 전투 시작 전역 기믹 발동
        InitializeBattlefield();
        statusManager.OnBattleStart(allUnits); // 최초 1회 발동

        // 엔진 점화 로그 이벤트 발송 (Broadcast 사용)
        BattleLogEvents.BroadcastBattleStarted();

        // 3. 코어 루프 가동
        StartCoroutine(BattleLoop());
    }

    // SimulationManager의 Stop 버튼과 연동되어 전투 코루틴을 강제 파괴하는 방어선 API
    /// <summary>
    /// 외부에서 시뮬레이션을 강제 중단할 때 호출하여, 내부 전투 루프 코루틴을 즉시 멈춥니다.
    /// </summary>
    public void StopBattle()
    {
        StopAllCoroutines();
        CleanupEventSubscriptions(); // 강제 종료 시 메모리 누수 방지

        // 이 로그 is 전투 로그가 아닌 시스템 강제 정지 경고이므로 보존합니다.
        Debug.Log("<color=red><b>[BattleManager]</b> 연쇄 정지 명령 수신: 진행 중인 전투 루프 코루틴이 강제 종료되었습니다.</color>");
    }

    private void InitializeBattlefield()
    {
        List<UnitControl> players = allUnits.Where(u => u.isPlayer).ToList();
        List<UnitControl> enemies = allUnits.Where(u => !u.isPlayer).ToList();

        FormationManager.InitializeFormation(players, enemies);

        // 모든 유닛의 사망 이벤트를 중앙 매니저가 구독합니다.
        foreach (var unit in allUnits)
        {
            if (unit != null)
            {
                // 중복 구독 방지를 위해 뺐다가 다시 넣습니다.
                unit.OnDeathConditionMet -= HandleUnitDeathCondition;
                unit.OnDeathConditionMet += HandleUnitDeathCondition;
            }
        }

        // 개발자용 시스템 초기화 확인 로그이므로 보존합니다.
        Debug.Log("<b>[BattleManager]</b> 유닛들이 진형 체스판에 성공적으로 배치되었습니다.");
    }

    // 유닛이 죽음의 신호탄을 쏘면 큐에 등록합니다.
    private void HandleUnitDeathCondition(UnitControl unit)
    {
        if (unit != null && !deathQueue.Contains(unit))
        {
            deathQueue.Add(unit);
        }
    }

    private void CleanupEventSubscriptions()
    {
        foreach (var unit in allUnits)
        {
            if (unit != null) unit.OnDeathConditionMet -= HandleUnitDeathCondition;
        }
    }

    // ========================================================================
    // [전투 핵심 루프]
    // ========================================================================
    private IEnumerator BattleLoop()
    {
        yield return new WaitForSeconds(0.5f);

        // 전투가 계속 진행 가능한지 확인 (전멸 확인)
        while (CheckBattleContinue())
        {
            // 최대 라운드 도달 시 강제 무승부 처리 (Timeout, 0 이하일 경우 무제한 전투)
            if (currentMaxRounds > 0 && currentRound >= currentMaxRounds)
            {
                BattleLogEvents.BroadcastBattleTimeout(currentRound);
                break; // while 루프를 탈출하여 즉시 전투 종료로 직행합니다.
            }

            currentRound++;
            // 라운드 시작 이벤트 발송 (Broadcast 사용)
            BattleLogEvents.BroadcastRoundStarted(currentRound);

            // [Phase 1: 라운드 시작]
            statusManager.OnRoundStart(allUnits);
            ProcessDeathQueue(); // [Sync Point 1] 라운드 시작 시 도트딜 사망자 정리

            turnQueue = turnSorter.BuildTurnQueue(GetAliveUnits());

            // 턴 대기열이 완성된 직후, 이를 스냅샷으로 찍어 이벤트 버스로 쏘아 보냅니다.
            BattleLogEvents.BroadcastTurnOrderCalculated(turnQueue.ToList());

            // [Phase 2: 턴 반복 루프]
            while (turnQueue.Count > 0)
            {
                UnitControl currentUnit = turnQueue.Dequeue();

                // 안전장치: 자신의 턴이 오기 전에 사망하거나, 진형에서 이탈한 경우 스킵
                if (currentUnit == null || currentUnit.isDead) continue;

                yield return StartCoroutine(ProcessTurn(currentUnit));

                // 개별 턴 종료 직후 한쪽 진영이 완전히 전멸했는지 검사하여 유령 턴 행동을 차단
                if (!CheckBattleContinue())
                {
                    break;
                }
            }

            // [Phase 3: 라운드 종료]
            statusManager.OnRoundEnd(allUnits);
            ProcessDeathQueue(); // [Sync Point 5] 라운드 종료 시 도트딜/기믹 사망자 정리

            // 라운드 종료 이벤트 발송 (Broadcast 사용)
            BattleLogEvents.BroadcastRoundEnded(currentRound);

            yield return new WaitForSeconds(1.0f);
        }

        // 전투 완전히 종료 이벤트 발송 (Broadcast 사용)
        BattleLogEvents.BroadcastBattleEnded();

        // 전투 종료 시점에 플레이어측 유닛들의 실시간 생존/속성 데이터를 추출하여 PartyRoster 장부에 덮어쓰기(Save)
        if (PartyRoster.Instance != null)
        {
            foreach (UnitControl unit in allUnits)
            {
                if (unit != null && unit.isPlayer && unit.SourceData != null)
                {
                    CurrentPartyState finalState = new CurrentPartyState
                    {
                        currentHP = unit.isDead ? 0 : unit.currentHP,
                        yinYangValue = unit.currentAttributes.TryGetValue(AttributeType.YinYang, out int yy) ? yy : 50,
                        dreamValue = unit.currentAttributes.TryGetValue(AttributeType.Dream, out int dm) ? dm : 0
                    };

                    PartyRoster.Instance.UpdateUnitState(unit.SourceData, finalState);
                }
            }
        }

        CleanupEventSubscriptions(); // 메모리 누수 방지
        statusManager.OnBattleEnd(allUnits); // 전투 최종 종료 시점 이벤트 발동 (승패 정산용)
        OnBattleEnded?.Invoke(); // 상위 매니저들에게 전투가 끝났음을 알림 (옵저버 패턴)
    }

    private IEnumerator ProcessTurn(UnitControl unit)
    {
        // 턴 시작 이벤트 발송 (Broadcast 사용)
        BattleLogEvents.BroadcastTurnStarted(unit);

        // [(Track B)] 턴 시작 시 침식 과다(0 미만, 100 초과) 여부를 가장 먼저 검사합니다.
        // 팀원이 힐/게이지 조작으로 구제해주지 못했다면 이 시점에 즉사(턴 스킵)합니다.
        if (unit.CheckAndTriggerCorrosionDeath())
        {
            ProcessDeathQueue(); // 큐에 등록된 자신을 즉시 청소합니다.
            yield break;
        }

        statusManager.OnTurnStart(unit);
        ProcessDeathQueue(); // [Sync Point 2] 턴 시작 도트딜 사망자 정리

        // 만약 턴 시작 도트딜에 죽었다면 행동 불가
        if (unit.isDead) yield break;

        if (unit.IsUnableToAct())
        {
            // [업데이트] 실제로 턴을 소모하는 과정에서 행동불능 침식 기믹을 겪었으므로 플래그를 참으로 설정합니다.
            unit.CorrosionExperienced = true;

            // 턴 스킵 이벤트 발송 (Broadcast 사용)
            BattleLogEvents.BroadcastTurnSkipped(unit, "행동 불가");
        }
        else
        {
            if (unit.IsForcedToActRandomly())
            {
                // [업데이트] 실제로 턴을 소모하는 과정에서 통제상실 침식 기믹을 겪었으므로 플래그를 참으로 설정합니다.
                unit.CorrosionExperienced = true;

                yield return StartCoroutine(ExecuteRandomAction(unit));
            }
            // 소속(isPlayer)이 아닌 뇌의 종류(타입)를 검사하여 진정한 다형성을 확보합니다.
            else if (unit.Brain is BaseAIBrain)
            {
                yield return StartCoroutine(ExecuteAIAction(unit));
            }
            else
            {
                // BaseAIBrain을 상속받지 않은 외부 뇌(추후 구현될 HumanBrain 등)일 경우에만 입력을 대기합니다.
                yield return StartCoroutine(WaitForPlayerAction(unit));
            }
        }

        ProcessDeathQueue(); // [Sync Point 3] 스킬 사용(ExecuteAction) 직후 데미지/반사 사망자 정리

        statusManager.OnTurnEnd(unit);
        unit.DecreaseCooldowns();

        ProcessDeathQueue(); // [Sync Point 4] 턴 종료 도트딜/기믹 사망자 정리

        // 턴 종료 이벤트 발송 (Broadcast 사용)
        BattleLogEvents.BroadcastTurnEnded(unit);
    }

    // ========================================================================
    // [행동 실행 코루틴 대기 구역]
    // ========================================================================
    private IEnumerator WaitForPlayerAction(UnitControl unit)
    {
        // 개발자/시스템용 입력 대기 알림이므로 보존합니다.
        Debug.Log("플레이어의 조작(입력)을 대기 중입니다...");
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator ExecuteAIAction(UnitControl unit)
    {
        // 연산 딜레이 알림 시스템 로그 보존
        Debug.Log($"{unit.unitName} (AI)가 전황을 분석 중입니다...");
        yield return new WaitForSeconds(0.6f);

        List<SkillData> usableSkills = new List<SkillData>();
        foreach (SkillData skill in unit.equippedSkills)
        {
            if (unit.GetCooldown(skill) == 0) usableSkills.Add(skill);
        }

        if (unit.Brain == null || usableSkills.Count == 0)
        {
            // 지능 부재 또는 스킬 부재로 인한 턴 스킵 (Broadcast 사용)
            BattleLogEvents.BroadcastTurnSkipped(unit, "스킬 부재 혹은 두뇌 상실");
            yield break;
        }

        ActionDecision decision = unit.Brain.SelectNextSkill(usableSkills, allUnits, FormationManager);

        if (decision == null || decision.SelectedSkill == null || decision.MainTarget == null)
        {
            // 방어 태세(스킬 사용 포기)로 인한 턴 스킵 (Broadcast 사용)
            BattleLogEvents.BroadcastTurnSkipped(unit, "방어 태세(유리한 행동 없음)");
            yield break;
        }

        // 스킬 시전 알림 이벤트 발송 (Broadcast 사용)
        BattleLogEvents.BroadcastSkillCasted(unit, decision.MainTarget, decision.SelectedSkill);

        yield return new WaitForSeconds(0.5f);

        combatResolver.ExecuteAction(unit, decision);
        unit.SetCooldown(decision.SelectedSkill, decision.SelectedSkill.maxCooldown);
    }

    private IEnumerator ExecuteRandomAction(UnitControl unit)
    {
        // 제어권 상실 알림 이벤트 발송 (Broadcast 사용)
        BattleLogEvents.BroadcastRandomActionForced(unit);
        yield return new WaitForSeconds(0.6f);
    }

    // ========================================================================
    // [유틸리티 및 시스템 함수]
    // ========================================================================

    // 대기열에 모인 사망자들을 일괄적으로 논리/물리적 처리하는 중앙 청소기
    private void ProcessDeathQueue()
    {
        if (deathQueue.Count == 0) return;

        foreach (UnitControl deadUnit in deathQueue)
        {
            if (deadUnit == null || deadUnit.isDead) continue;

            deadUnit.isDead = true;
            // 사망 알림 이벤트 발송 (Broadcast 사용)
            BattleLogEvents.BroadcastUnitDied(deadUnit);

            // 1. 논리적 진형(체스판)에서 이탈시켜 빈칸 확보 (Summon 기믹 대비)
            FormationManager.RemoveUnit(deadUnit);

            // 2. 물리적 비활성화 (오브젝트 풀링 반환)
            // (추후 C단계 게임씬 적용 시, IUnitLifecycleHandler를 주입받아 화려한 사망 연출로 분기할 수 있습니다.)
            deadUnit.gameObject.SetActive(false);
        }

        deathQueue.Clear();
    }

    private List<UnitControl> GetAliveUnits()
    {
        return allUnits.Where(u => u != null && !u.isDead).ToList();
    }

    private bool CheckBattleContinue()
    {
        bool hasPlayer = allUnits.Any(u => u.isPlayer && !u.isDead);
        bool hasEnemy = allUnits.Any(u => !u.isPlayer && !u.isDead);
        return hasPlayer && hasEnemy;
    }
}