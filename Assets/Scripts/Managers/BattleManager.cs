using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// ====================================================
// [BattleManager.cs] (구 RoundManager)
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

    // [업데이트] 1차원 진형 체스판을 관리하는 매니저 신규 추가
    public FormationManager FormationManager { get; private set; }

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

        // 진형 관리자 생성 (아직 유닛 배치는 안 된 상태)
        FormationManager = new FormationManager();
    }

    private void Start()
    {
        // 전투 시작 전 유닛들을 물리적 체스판(진형)에 배치합니다.
        InitializeBattlefield();
        StartCoroutine(BattleLoop());
    }

    /// <summary>
    /// [신규 추가] 전투 진입 시, 모든 유닛을 아군과 적군으로 나누어 진형 슬롯(0~3번)에 배치합니다.
    /// </summary>
    private void InitializeBattlefield()
    {
        List<UnitControl> players = allUnits.Where(u => u.isPlayer).ToList();
        List<UnitControl> enemies = allUnits.Where(u => !u.isPlayer).ToList();

        FormationManager.InitializeFormation(players, enemies);
        Debug.Log("<b>[BattleManager]</b> 유닛들이 진형 체스판에 성공적으로 배치되었습니다.");
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

                // 안전장치: 자신의 턴이 오기 전에 사망하거나, 진형에서 이탈한 경우 스킵
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

        // 2-6. 턴 종료 시점 이벤트 발동
        statusManager.OnTurnEnd(unit);

        // [업데이트] 턴이 종료될 때 해당 유닛의 모든 스킬 쿨타임을 1씩 감소시킵니다.
        unit.DecreaseCooldowns();

        Debug.Log($"[{unit.unitName}] 턴 종료.");
    }

    // ========================================================================
    // [행동 실행 코루틴 대기 구역]
    // ========================================================================
    private IEnumerator WaitForPlayerAction(UnitControl unit)
    {
        // TODO: UI에서 플레이어가 스킬을 선택할 때까지 대기하는 로직이 들어갑니다.
        Debug.Log("플레이어의 조작(입력)을 대기 중입니다...");
        yield return new WaitForSeconds(1.0f);
    }

    /// <summary>
    /// [1순위 작업 완료] 5단계 전술 파이프라인을 관장하는 AI 실행부입니다.
    /// </summary>
    private IEnumerator ExecuteAIAction(UnitControl unit)
    {
        Debug.Log($"{unit.unitName} (AI)가 전황을 분석 중입니다...");
        yield return new WaitForSeconds(0.6f); // 봇이 고민하는 듯한 시각적 딜레이

        // 1단계: 쿨타임 검사 필터링 (사용 가능한 스킬 추려내기)
        List<SkillData> usableSkills = new List<SkillData>();
        foreach (SkillData skill in unit.equippedSkills)
        {
            if (unit.GetCooldown(skill) == 0) usableSkills.Add(skill);
        }

        // 안전장치
        if (unit.Brain == null || usableSkills.Count == 0)
        {
            Debug.Log($"[{unit.unitName}] 행동 불가: 사용 가능한 스킬이 없거나 두뇌가 없습니다.");
            yield break;
        }

        // 2단계: 최신 진형 주입 및 두뇌 의사결정 요청
        ActionDecision decision = unit.Brain.SelectNextSkill(usableSkills, allUnits, FormationManager);

        // 3단계: 의사결정 검증 (Bypass 체크)
        if (decision == null || decision.SelectedSkill == null || decision.MainTarget == null)
        {
            Debug.Log($"[{unit.unitName}] 판단 결과: 현재 상황에 유리한 행동이 없어 방어 태세를 취합니다 (턴 스킵).");
            yield break; // 코루틴을 종료하고 즉시 턴 정산으로 넘어감
        }

        // 4단계: 심판관 호출 및 실집행
        Debug.Log($"<color=yellow>[{unit.unitName}] (이)가 [{decision.SelectedSkill.skillName}] 스킬을 시전!</color>");
        yield return new WaitForSeconds(0.5f); // 스킬 시전 애니메이션 딜레이 연출

        // 결재 서류를 그대로 넘겨주어 메인 타겟과 서브 타겟을 처리하게 합니다.
        combatResolver.ExecuteAction(unit, decision);

        // 5단계: 사용한 스킬에 고유 쿨타임 적용
        unit.SetCooldown(decision.SelectedSkill, decision.SelectedSkill.maxCooldown);
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