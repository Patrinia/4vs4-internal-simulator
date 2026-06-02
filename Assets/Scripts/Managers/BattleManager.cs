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

    [Header("시스템 의존성 모음 (순수 C# 전문가들)")]
    private ITurnSorter turnSorter;
    private CombatResolver combatResolver;
    private StatusEffectManager statusManager;

    // 1차원 진형 체스판을 관리하는 매니저
    public FormationManager FormationManager { get; private set; }

    [Header("전투 데이터")]
    public List<UnitControl> allUnits = new List<UnitControl>(); // 전투에 참여하는 모든 유닛 리스트
    private Queue<UnitControl> turnQueue = new Queue<UnitControl>(); // 이번 라운드의 턴 대기열
    private int currentRound = 0; // 현재 라운드 수

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

    // 기존의 Start() 내부 자동 실행 로직을 제거했습니다. 
    // 이제 BattleManager는 스스로 전투를 시작하지 않습니다. (수동 점화 대기)

    // ========================================================================
    // [외부 제어 API]
    // ========================================================================

    /// <summary>
    /// 외부 매니저(SimulationManager, GameFlowManager)가 호출하는 수동 점화 스위치입니다.
    /// </summary>
    /// <param name="participatingUnits">SpawnManager가 세팅을 마친 이번 전투의 참여 유닛 전체 리스트</param>
    public void StartBattle(List<UnitControl> participatingUnits)
    {
        // 1. 전투 데이터 초기화
        allUnits = new List<UnitControl>(participatingUnits);
        currentRound = 0;
        turnQueue.Clear();
        deathQueue.Clear(); // 사망 대기열 초기화

        // 2. 진형 배치 및 전투 시작 전역 기믹 발동
        InitializeBattlefield();
        statusManager.OnBattleStart(allUnits); // 최초 1회 발동

        Debug.Log("<color=green><b>[BattleManager]</b> 외부 명령으로 엔진 점화! 전투를 시작합니다.</color>");

        // 3. 코어 루프 가동
        StartCoroutine(BattleLoop());
    }

    // [신규 업데이트] SimulationManager의 Stop 버튼과 연동되어 전투 코루틴을 강제 파괴하는 방어선 API
    /// <summary>
    /// 외부에서 시뮬레이션을 강제 중단할 때 호출하여, 내부 전투 루프 코루틴을 즉시 멈춥니다.
    /// </summary>
    public void StopBattle()
    {
        StopAllCoroutines();
        CleanupEventSubscriptions(); // 강제 종료 시 메모리 누수 방지
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
            currentRound++;
            Debug.Log($"<color=cyan>=== 라운드 {currentRound} 시작 ===</color>");

            // [Phase 1: 라운드 시작]
            statusManager.OnRoundStart(allUnits);
            ProcessDeathQueue(); // [Sync Point 1] 라운드 시작 시 도트딜 사망자 정리

            turnQueue = turnSorter.BuildTurnQueue(GetAliveUnits());

            // [Phase 2: 턴 반복 루프]
            while (turnQueue.Count > 0)
            {
                UnitControl currentUnit = turnQueue.Dequeue();

                // 안전장치: 자신의 턴이 오기 전에 사망하거나, 진형에서 이탈한 경우 스킵
                if (currentUnit == null || currentUnit.isDead) continue;

                yield return StartCoroutine(ProcessTurn(currentUnit));
            }

            // [Phase 3: 라운드 종료]
            statusManager.OnRoundEnd(allUnits);
            ProcessDeathQueue(); // [Sync Point 5] 라운드 종료 시 도트딜/기믹 사망자 정리

            Debug.Log($"<color=orange>=== 라운드 {currentRound} 종료 ===</color>");
            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("<color=red><b>[BattleManager] 전투가 완전히 종료되었습니다. 결과를 집계하고 방송을 송출합니다.</b></color>");

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
        Debug.Log($"[{unit.unitName}] 턴 시작.");

        // [(Track B)] 턴 시작 시 침식 과다(0 미만, 100 초과) 여부를 가장 먼저 검사합니다.
        // 팀원이 힐/게이지 조작으로 구제해주지 못했다면 이 시점에 즉사(턴 스킵)합니다.
        if (unit.CheckAndTriggerErosionDeath())
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
            Debug.Log($"[{unit.unitName}] 행동 불가 상태입니다. 턴을 건너뜁니다.");
        }
        else
        {
            if (unit.IsForcedToActRandomly())
            {
                yield return StartCoroutine(ExecuteRandomAction(unit));
            }
            else if (unit.isPlayer)
            {
                // 시뮬레이션에서는 유닛이 BotBrain을 가지므로 이 분기로 들어오지 않습니다.
                // 본 게임에서만 PlayerBrain을 가진 유닛이 이 코드를 타게 됩니다.
                yield return StartCoroutine(WaitForPlayerAction(unit));
            }
            else
            {
                yield return StartCoroutine(ExecuteAIAction(unit));
            }
        }

        ProcessDeathQueue(); // [Sync Point 3] 스킬 사용(ExecuteAction) 직후 데미지/반사 사망자 정리

        statusManager.OnTurnEnd(unit);
        unit.DecreaseCooldowns();

        ProcessDeathQueue(); // [Sync Point 4] 턴 종료 도트딜/기믹 사망자 정리

        Debug.Log($"[{unit.unitName}] 턴 종료.");
    }

    // ========================================================================
    // [행동 실행 코루틴 대기 구역]
    // ========================================================================
    private IEnumerator WaitForPlayerAction(UnitControl unit)
    {
        Debug.Log("플레이어의 조작(입력)을 대기 중입니다...");
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator ExecuteAIAction(UnitControl unit)
    {
        Debug.Log($"{unit.unitName} (AI)가 전황을 분석 중입니다...");
        yield return new WaitForSeconds(0.6f);

        List<SkillData> usableSkills = new List<SkillData>();
        foreach (SkillData skill in unit.equippedSkills)
        {
            if (unit.GetCooldown(skill) == 0) usableSkills.Add(skill);
        }

        if (unit.Brain == null || usableSkills.Count == 0)
        {
            Debug.Log($"[{unit.unitName}] 행동 불가: 사용 가능한 스킬이 없거나 두뇌가 없습니다.");
            yield break;
        }

        ActionDecision decision = unit.Brain.SelectNextSkill(usableSkills, allUnits, FormationManager);

        if (decision == null || decision.SelectedSkill == null || decision.MainTarget == null)
        {
            Debug.Log($"[{unit.unitName}] 판단 결과: 현재 상황에 유리한 행동이 없어 방어 태세를 취합니다 (턴 스킵).");
            yield break;
        }

        Debug.Log($"<color=yellow>[{unit.unitName}] (이)가 [{decision.SelectedSkill.skillName}] 스킬을 시전!</color>");
        yield return new WaitForSeconds(0.5f);

        combatResolver.ExecuteAction(unit, decision);
        unit.SetCooldown(decision.SelectedSkill, decision.SelectedSkill.maxCooldown);
    }

    private IEnumerator ExecuteRandomAction(UnitControl unit)
    {
        Debug.Log("<color=red>유닛이 제어권을 상실했습니다! 무작위로 행동합니다.</color>");
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
            Debug.Log($"<color=black><b>[사망]</b> {deadUnit.unitName}이(가) 쓰러졌습니다!</color>");

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