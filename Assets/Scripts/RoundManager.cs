using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("System Dependencies")]
    private ITurnSorter turnSorter;
    private CombatResolver combatResolver;
    private StatusEffectManager statusManager;

    [Header("Battle Data")]
    public List<UnitControl> allUnits = new List<UnitControl>();
    private Queue<UnitControl> turnQueue = new Queue<UnitControl>();
    private int currentRound = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 필요한 매니저들 인스턴스(객체) 생성
        turnSorter = new SpeedTurnSorter();
        combatResolver = new CombatResolver();
        statusManager = new StatusEffectManager();
    }

    private void Start()
    {
        StartCoroutine(BattleLoop());
    }

    // ========================================================================
    // [Battle Core Loop]
    // ========================================================================
    private IEnumerator BattleLoop()
    {
        yield return new WaitForSeconds(0.5f);

        // Phase 3-2: Loop until battle ends
        while (CheckBattleContinue())
        {
            currentRound++;
            Debug.Log($"<color=cyan>=== Round {currentRound} Start ===</color>");

            // [Phase 1: Round Start]
            statusManager.OnRoundStart(allUnits);                     // 1-1
            turnQueue = turnSorter.BuildTurnQueue(GetAliveUnits());   // 1-2 & 1-3

            // [Phase 2: Turn Loop]
            while (turnQueue.Count > 0)
            {
                UnitControl currentUnit = turnQueue.Dequeue(); // 2-1

                // Safety check: Skip if unit died from dot damage or reflect before their turn
                if (currentUnit == null || currentUnit.isDead) continue;

                yield return StartCoroutine(ProcessTurn(currentUnit));
            }

            // [Phase 3: Round End]
            statusManager.OnRoundEnd(allUnits); // 3-1
            Debug.Log($"<color=orange>=== Round {currentRound} End ===</color>");
            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("Battle Finished.");
    }

    private IEnumerator ProcessTurn(UnitControl unit)
    {
        Debug.Log($"[{unit.unitName}] Turn Start.");

        // 2-2. Turn Start Event
        statusManager.OnTurnStart(unit);

        // 2-3. Status Check
        if (unit.IsUnableToAct()) // Checking Yin erosion, stun, etc.
        {
            Debug.Log($"[{unit.unitName}] is unable to act. Skipping action.");
            // Skips Action (2-4), goes directly to Resolve (2-5)
        }
        else
        {
            // 2-4. Action Execution
            if (unit.IsForcedToActRandomly()) // Checking Yang erosion
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

        // 2-5. Result Resolve & Death Check
        combatResolver.ResolveCombatResults(allUnits);

        // 2-6. Turn End Event (Must execute even if action was skipped)
        statusManager.OnTurnEnd(unit);

        Debug.Log($"[{unit.unitName}] Turn End.");
        // 2-7. Turn Returned (End of coroutine)
    }

    // ========================================================================
    // [Action Execution Coroutines (To be expanded in A-2)]
    // ========================================================================
    private IEnumerator WaitForPlayerAction(UnitControl unit)
    {
        Debug.Log("Waiting for Player Input...");
        // Placeholder: Will wait for BattleUIManager / PlayerInputHandler
        yield return new WaitForSeconds(1.0f);
    }

    private IEnumerator ExecuteAIAction(UnitControl unit)
    {
        Debug.Log("AI is deciding...");
        // Placeholder: AI Utility Scoring logic will go here
        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator ExecuteRandomAction(UnitControl unit)
    {
        Debug.Log("<color=red>Unit is out of control! Acting randomly.</color>");
        // Placeholder: Force random skill on random target
        yield return new WaitForSeconds(0.6f);
    }

    // ========================================================================
    // [Utility Methods]
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