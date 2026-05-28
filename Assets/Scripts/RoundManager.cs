using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("시스템 의존성 모음")]
    private ITurnSorter turnSorter;
    private CombatResolver combatResolver;
    private StatusEffectManager statusManager;

    [Header("전투 데이터")]
    public List<UnitControl> allUnits = new List<UnitControl>(); // 전투에 참여하는 모든 유닛 리스트
    private Queue<UnitControl> turnQueue = new Queue<UnitControl>(); // 이번 라운드의 턴 대기열
    private int currentRound = 0; // 현재 라운드 수

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 순수 C# 전문가 클래스들 초기화
        turnSorter = new SpeedTurnSorter();
        combatResolver = new CombatResolver();
        statusManager = new StatusEffectManager();
    }

    private void Start()
    {
        StartCoroutine(BattleLoop());
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
            turnQueue = turnSorter.BuildTurnQueue(GetAliveUnits());

            // [Phase 2: 턴 반복 루프]
            while (turnQueue.Count > 0)
            {
                UnitControl currentUnit = turnQueue.Dequeue();

                // 안전장치: 자신의 턴이 오기 전에 도트 데미지나 반사 데미지로 사망한 경우 스킵
                if (currentUnit == null || currentUnit.isDead) continue;

                yield return StartCoroutine(ProcessTurn(currentUnit));
            }

            // [Phase 3: 라운드 종료]
            statusManager.OnRoundEnd(allUnits);
            Debug.Log($"<color=orange>=== 라운드 {currentRound} 종료 ===</color>");
            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("전투가 완전히 종료되었습니다.");
    }

    private IEnumerator ProcessTurn(UnitControl unit)
    {
        Debug.Log($"[{unit.unitName}] 턴 시작.");

        // 2-2. 턴 시작 시점 이벤트 발동
        statusManager.OnTurnStart(unit);

        // 2-3. 행동 가능 상태 검사
        if (unit.IsUnableToAct())
        {
            Debug.Log($"[{unit.unitName}] 행동 불가 상태입니다. 턴을 건너뜁니다.");
            // 행동을 건너뛰고 곧바로 정산 단계로 이동
        }
        else
        {
            // 2-4. 행동 실행
            if (unit.IsForcedToActRandomly())
            {
                yield return StartCoroutine(ExecuteRandomAction(unit));
            }
            else if (unit.isPlayer)
            {
                yield return StartCoroutine(WaitForPlayerAction(unit));
            }
            else
            {
                yield return StartCoroutine(ExecuteAIAction(unit));
            }
        }

        // 2-5. 행동 결과 정산 및 사망자 체크
        combatResolver.ResolveCombatResults(allUnits);

        // 2-6. 턴 종료 시점 이벤트 발동 (행동을 스킵했더라도 쿨타임은 감소해야 함)
        statusManager.OnTurnEnd(unit);

        Debug.Log($"[{unit.unitName}] 턴 종료.");
    }

    // ========================================================================
    // [행동 실행 코루틴 대기 구역 (A-2 단계 등에서 구체화 예정)]
    // ========================================================================
    private IEnumerator WaitForPlayerAction(UnitControl unit)
    {
        Debug.Log("플레이어의 조작(입력)을 대기 중입니다...");
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator ExecuteAIAction(UnitControl unit)
    {
        Debug.Log("AI가 행동을 결정 중입니다...");
        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator ExecuteRandomAction(UnitControl unit)
    {
        Debug.Log("<color=red>유닛이 제어권을 상실했습니다! 무작위로 행동합니다.</color>");
        yield return new WaitForSeconds(0.6f);
    }

    // ========================================================================
    // [유틸리티 함수]
    // ========================================================================
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