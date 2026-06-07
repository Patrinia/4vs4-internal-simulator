using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// ====================================================
// [BalanceDataExporter.cs]
// BattleLogEvents를 구독하여 전투 데이터를 수집하고, 
// 메모리 과부하(OOM) 방지를 위해 실시간으로 CSV와 JSON 파일로 밀어내는 스트리밍 수집가입니다.
// ====================================================
public class BalanceDataExporter : MonoBehaviour
{
    [Header("스트리밍 파일 제어")]
    // [업데이트] Manager가 동적으로 폴더를 생성하도록 파일명 앞에 카테고리 경로("Balance/")를 추가했습니다.
    private const string CSV_FILE_NAME = "Balance/Balance_CombatMetrics.csv";
    private const string JSON_FILE_NAME = "Balance/Balance_CombatFlow.json";
    private bool isFirstJsonRecord = true;

    [Header("시뮬레이션 누적 식별자")]
    private int currentSimId = 0;

    // ==========================================
    // [런타임 데이터 누적 장부 (1회차용)]
    // ==========================================
    private int p_aliveCount, e_aliveCount;
    private int p_corrosionDeaths, e_corrosionDeaths;

    // 데미지 및 치유 분리
    private int p_directDmg, p_statusDmg;
    private int e_directDmg, e_statusDmg;
    private int p_healDone, e_healDone;

    // 딕셔너리 트래커
    private Dictionary<string, int> p_tendencyDict = new Dictionary<string, int>();
    private Dictionary<string, int> e_tendencyDict = new Dictionary<string, int>();
    private Dictionary<string, int> p_statusImpactDict = new Dictionary<string, int>();
    private Dictionary<string, int> e_statusImpactDict = new Dictionary<string, int>();
    private Dictionary<string, int> p_skillCountDict = new Dictionary<string, int>();
    private Dictionary<string, int> e_skillCountDict = new Dictionary<string, int>();

    // JSON 타임라인 트래커
    private BattleRecord currentBattleRecord;
    private RoundRecord currentRoundRecord;
    private TurnRecord currentTurnRecord;

    // ========================================================================
    // [1. 초기화 및 파일 스트림 오픈]
    // ========================================================================
    private void Awake()
    {
        OpenStreams();
    }

    private void OpenStreams()
    {
        // 1. CSV 헤더 초기화 및 스트림 오픈 요청 (SimulationLogManager 위임)
        string csvHeader = "Sim_ID,Result,Total_Rounds,P_Alive_Count,E_Alive_Count,P_Remain_HP_Ratio,E_Remain_HP_Ratio," +
                           "P_Corrosion_Deaths,E_Corrosion_Deaths,P_Tendency_Distribution,E_Tendency_Distribution," +
                           "P_Dmg_Split,E_Dmg_Split,P_Heal_Done,E_Heal_Done,P_Status_Impact,E_Status_Impact," +
                           "P_Skill_Counters,E_Skill_Counters";

        if (SimulationLogManager.Instance != null)
        {
            SimulationLogManager.Instance.InitializeStream(CSV_FILE_NAME, csvHeader);

            // 2. JSON 배열 열기 (초기 괄호 기입)
            SimulationLogManager.Instance.InitializeStream(JSON_FILE_NAME, "[");
        }
        else
        {
            Debug.LogError("[BalanceDataExporter] SimulationLogManager 인스턴스를 찾을 수 없습니다!");
        }

        isFirstJsonRecord = true;
    }

    private void OnDestroy()
    {
        // 게임/시뮬레이션 종료 시 JSON 배열 닫기 및 스트림 해제 요청
        if (SimulationLogManager.Instance != null)
        {
            SimulationLogManager.Instance.CloseStream(JSON_FILE_NAME, "\n]");
            SimulationLogManager.Instance.CloseStream(CSV_FILE_NAME);
        }
    }

    // ========================================================================
    // [2. 이벤트 구독 (Method B: 옵저버 패턴)]
    // ========================================================================
    private void OnEnable()
    {
        BattleLogEvents.OnBattleStarted += HandleBattleStarted;
        BattleLogEvents.OnBattleEnded += HandleBattleEnded;
        BattleLogEvents.OnRoundStarted += HandleRoundStarted;
        BattleLogEvents.OnRoundEnded += HandleRoundEnded;
        BattleLogEvents.OnTurnStarted += HandleTurnStarted;
        BattleLogEvents.OnTurnEnded += HandleTurnEnded;

        BattleLogEvents.OnTurnOrderCalculated += HandleTurnOrderCalculated;
        BattleLogEvents.OnSkillCasted += HandleSkillCasted;
        BattleLogEvents.OnDamageDealt += HandleDamageDealt;
        BattleLogEvents.OnHealed += HandleHealed;
        BattleLogEvents.OnCorrosionDeath += HandleCorrosionDeath;
        BattleLogEvents.OnTurnSkipped += HandleTurnSkipped;

        BattleLogEvents.OnAttributeModified += HandleAttributeModified;
        BattleLogEvents.OnCorrosionReverted += HandleCorrosionReverted;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지용 구독 해제
        BattleLogEvents.OnBattleStarted -= HandleBattleStarted;
        BattleLogEvents.OnBattleEnded -= HandleBattleEnded;
        BattleLogEvents.OnRoundStarted -= HandleRoundStarted;
        BattleLogEvents.OnRoundEnded -= HandleRoundEnded;
        BattleLogEvents.OnTurnStarted -= HandleTurnStarted;
        BattleLogEvents.OnTurnEnded -= HandleTurnEnded;

        BattleLogEvents.OnTurnOrderCalculated -= HandleTurnOrderCalculated;
        BattleLogEvents.OnSkillCasted -= HandleSkillCasted;
        BattleLogEvents.OnDamageDealt -= HandleDamageDealt;
        BattleLogEvents.OnHealed -= HandleHealed;
        BattleLogEvents.OnCorrosionDeath -= HandleCorrosionDeath;
        BattleLogEvents.OnTurnSkipped -= HandleTurnSkipped;

        BattleLogEvents.OnAttributeModified -= HandleAttributeModified;
        BattleLogEvents.OnCorrosionReverted -= HandleCorrosionReverted;
    }

    // ========================================================================
    // [3. 런타임 데이터 축적부]
    // ========================================================================

    private void HandleBattleStarted()
    {
        currentSimId++;

        // CSV 집계 장부 초기화
        p_corrosionDeaths = e_corrosionDeaths = 0;
        p_directDmg = p_statusDmg = e_directDmg = e_statusDmg = 0;
        p_healDone = e_healDone = 0;

        p_tendencyDict.Clear(); e_tendencyDict.Clear();
        p_statusImpactDict.Clear(); e_statusImpactDict.Clear();
        p_skillCountDict.Clear(); e_skillCountDict.Clear();

        // JSON 루트 객체 생성 (초기화)
        currentBattleRecord = new BattleRecord { Sim_ID = currentSimId };

        // 초기 전장 세팅 정보 파싱 및 JSON 주입 (읽기 전용)
        if (BattleManager.Instance != null)
        {
            foreach (var unit in BattleManager.Instance.allUnits)
            {
                // 1. CSV 용 텐던시 추출
                var targetDict = unit.isPlayer ? p_tendencyDict : e_tendencyDict;

                // 2. JSON 초기 세팅 정보 객체 생성
                InitialUnitSetup setupInfo = new InitialUnitSetup
                {
                    UnitName = unit.unitName,
                    IsPlayer = unit.isPlayer,
                    PositionIndex = unit.positionIndex
                };

                foreach (var skill in unit.equippedSkills)
                {
                    // CSV 용 태그 카운팅
                    foreach (var tag in skill.skillTendencies)
                    {
                        string tagStr = tag.ToString();
                        if (targetDict.ContainsKey(tagStr)) targetDict[tagStr]++;
                        else targetDict[tagStr] = 1;
                    }

                    // JSON 스킬 이름 목록 수집
                    setupInfo.EquippedSkills.Add(skill.skillID); // 파이썬 식별을 위해 ID 기록
                }

                // JSON 최상단 헤더(InitialSetup) 리스트에 추가
                currentBattleRecord.InitialSetup.Add(setupInfo);
            }
        }
    }

    private void HandleRoundStarted(int round)
    {
        currentRoundRecord = new RoundRecord { RoundNumber = round };
        currentBattleRecord.Rounds.Add(currentRoundRecord);
        currentRoundRecord.Phase_RoundStart.Add($"라운드 {round} 시작");
    }

    private void HandleRoundEnded(int round)
    {
        if (currentRoundRecord != null)
        {
            currentRoundRecord.Phase_RoundEnd.Add($"라운드 {round} 종료");
        }
    }

    private void HandleTurnOrderCalculated(List<UnitControl> turnOrder)
    {
        if (currentRoundRecord != null)
        {
            currentRoundRecord.TurnOrder = turnOrder.Select(u => u.unitName).ToList();
        }
    }

    private void HandleTurnStarted(UnitControl unit)
    {
        currentTurnRecord = new TurnRecord { Caster = unit.unitName };
        if (currentRoundRecord != null) currentRoundRecord.Turns.Add(currentTurnRecord);
        currentTurnRecord.Phase_TurnStart.Add($"[{unit.unitName}] 턴 시작");
    }

    private void HandleSkillCasted(UnitControl caster, UnitControl target, SkillData skill)
    {
        // 1. CSV 스킬 카운터 분류
        var dict = caster.isPlayer ? p_skillCountDict : e_skillCountDict;
        string typeKey = "Norm";
        if (skill.skillTendencies.Contains(TendencyType.SelfMove)) typeKey = "Move";
        // 임시: 궁극기는 기획상 쿨타임이 매우 길거나 특정 태그로 판별된다고 가정
        else if (skill.maxCooldown >= 5) typeKey = "Ult";

        if (dict.ContainsKey(typeKey)) dict[typeKey]++;
        else dict[typeKey] = 1;

        // 2. JSON 기록
        if (currentTurnRecord != null)
        {
            currentTurnRecord.MainAction = new ActionRecord
            {
                ActionType = typeKey + "Skill",
                SkillName = skill.skillName,
                Target = target.unitName,
                Result = "시전 완료"
            };
        }
    }

    private void HandleDamageDealt(UnitControl caster, UnitControl target, int damage, string targetTypeStr)
    {
        if (caster.isPlayer) p_directDmg += damage;
        else e_directDmg += damage;

        if (currentTurnRecord != null && currentTurnRecord.MainAction != null)
        {
            currentTurnRecord.MainAction.Result = $"{damage} 대미지 적용";
        }
    }

    private void HandleHealed(UnitControl caster, UnitControl target, int amount, string targetTypeStr)
    {
        if (caster.isPlayer) p_healDone += amount;
        else e_healDone += amount;
    }

    private void HandleCorrosionDeath(UnitControl unit, int value)
    {
        if (unit.isPlayer) p_corrosionDeaths++;
        else e_corrosionDeaths++;

        if (currentTurnRecord != null)
        {
            currentTurnRecord.Phase_TurnStart.Add($"[침식 붕괴] 즉사 발동 (수치:{value})");
        }
    }

    private void HandleTurnSkipped(UnitControl unit, string reason)
    {
        if (currentTurnRecord != null)
        {
            currentTurnRecord.MainAction = new ActionRecord { ActionType = "TurnSkipped", Result = reason };
        }

        // 상태이상(기절)으로 인한 턴 스킵 기여도 수집 (Method B 추론)
        if (reason.Contains("기절"))
        {
            // 타겟(기절당한 자)이 아군이면, 기절을 건 적군의 기여도가 올라감
            var impactDict = unit.isPlayer ? e_statusImpactDict : p_statusImpactDict;
            if (impactDict.ContainsKey("StunTurns")) impactDict["StunTurns"]++;
            else impactDict["StunTurns"] = 1;
        }
    }

    private void HandleAttributeModified(UnitControl target, AttributeType type, int amount)
    {
        if (currentTurnRecord != null)
        {
            currentTurnRecord.Phase_TurnEnd.Add($"[기믹] {type} {amount} 변화");
        }
    }

    private void HandleCorrosionReverted(UnitControl unit, string stateName)
    {
        if (currentRoundRecord != null)
        {
            currentRoundRecord.Phase_RoundEnd.Add($"[{unit.unitName}] {stateName} 구제 (50 리셋)");
        }
    }

    private void HandleTurnEnded(UnitControl unit)
    {
        if (currentTurnRecord != null)
        {
            currentTurnRecord.Phase_TurnEnd.Add($"[{unit.unitName}] 턴 종료");
        }
    }

    // ========================================================================
    // [Method B: 상태이상 대미지 명시적 수신부 (Stage C 확장용)]
    // ========================================================================
    public void HandleStatusImpactDealt(UnitControl caster, EffectType type, int amount)
    {
        // 1. 상태이상 데미지 합계 분리
        if (caster.isPlayer) p_statusDmg += amount;
        else e_statusDmg += amount;

        // 2. 디테일 기여도 딕셔너리 기록 (Burn:450 등)
        var impactDict = caster.isPlayer ? p_statusImpactDict : e_statusImpactDict;
        string impactKey = type.ToString() + "Dmg";

        if (impactDict.ContainsKey(impactKey)) impactDict[impactKey] += amount;
        else impactDict[impactKey] = amount;
    }

    // ========================================================================
    // [4. 전투 종료 정산 및 스트리밍 (파일 쓰기)]
    // ========================================================================
    private void HandleBattleEnded()
    {
        if (BattleManager.Instance == null) return;

        var players = BattleManager.Instance.allUnits.Where(u => u.isPlayer).ToList();
        var enemies = BattleManager.Instance.allUnits.Where(u => !u.isPlayer).ToList();

        // 결과 산출
        p_aliveCount = players.Count(u => !u.isDead);
        e_aliveCount = enemies.Count(u => !u.isDead);

        float p_maxHp = players.Sum(u => u.SourceData != null ? u.SourceData.maxHP : 1);
        float e_maxHp = enemies.Sum(u => u.SourceData != null ? u.SourceData.maxHP : 1);
        float p_remHpRatio = (p_maxHp > 0) ? ((float)players.Where(u => !u.isDead).Sum(u => u.currentHP) / p_maxHp) * 100f : 0f;
        float e_remHpRatio = (e_maxHp > 0) ? ((float)enemies.Where(u => !u.isDead).Sum(u => u.currentHP) / e_maxHp) * 100f : 0f;

        string result = "Draw/Timeout";
        if (p_aliveCount > 0 && e_aliveCount == 0) result = "PlayerWin";
        else if (p_aliveCount == 0 && e_aliveCount > 0) result = "EnemyWin";

        // JSON 마감 처리
        currentBattleRecord.Battle_Result = result;

        // 스트리밍 쓰기 수행
        WriteCsvRecord(result, p_remHpRatio, e_remHpRatio);
        WriteJsonRecord();
    }

    private void WriteCsvRecord(string result, float p_remHpRatio, float e_remHpRatio)
    {
        // 딕셔너리 직렬화 헬퍼 함수 활용
        string p_tendency = SerializeDict(p_tendencyDict);
        string e_tendency = SerializeDict(e_tendencyDict);
        string p_dmgSplit = $"Direct:{p_directDmg}|Status:{p_statusDmg}";
        string e_dmgSplit = $"Direct:{e_directDmg}|Status:{e_statusDmg}";
        string p_impact = SerializeDict(p_statusImpactDict);
        string e_impact = SerializeDict(e_statusImpactDict);
        string p_skills = SerializeDict(p_skillCountDict);
        string e_skills = SerializeDict(e_skillCountDict);

        // 19개 칼럼 규격화
        string row = $"{currentSimId},{result},{currentBattleRecord.Rounds.Count}," +
                     $"{p_aliveCount},{e_aliveCount},{p_remHpRatio:F1},{e_remHpRatio:F1}," +
                     $"{p_corrosionDeaths},{e_corrosionDeaths},{p_tendency},{e_tendency}," +
                     $"{p_dmgSplit},{e_dmgSplit},{p_healDone},{e_healDone},{p_impact},{e_impact}," +
                     $"{p_skills},{e_skills}";

        // I/O 매니저에게 쓰기 위임
        if (SimulationLogManager.Instance != null)
        {
            SimulationLogManager.Instance.WriteRecord(CSV_FILE_NAME, row);
        }
    }

    private void WriteJsonRecord()
    {
        string jsonStr = JsonUtility.ToJson(currentBattleRecord, true);

        // I/O 매니저에게 쓰기 위임 (첫 레코드가 아니면 콤마를 앞에 붙임)
        if (!isFirstJsonRecord) jsonStr = ",\n" + jsonStr;

        if (SimulationLogManager.Instance != null)
        {
            SimulationLogManager.Instance.WriteRecord(JSON_FILE_NAME, jsonStr);
        }

        isFirstJsonRecord = false;
        currentBattleRecord = null; // 메모리에서 JSON 객체 파기
    }

    private string SerializeDict(Dictionary<string, int> dict)
    {
        if (dict.Count == 0) return "None";
        return string.Join("|", dict.Select(kv => $"{kv.Key}:{kv.Value}"));
    }
}

// ========================================================================
// [JSON 직렬화용 보조 클래스 (Tree 구조)]
// ========================================================================
[Serializable]
public class InitialUnitSetup
{
    public string UnitName;
    public bool IsPlayer;
    public int PositionIndex; // 0번이 최전방, 3번이 최후방
    public List<string> EquippedSkills = new List<string>();
}

[Serializable]
public class BattleRecord
{
    public int Sim_ID;
    public string Battle_Result;
    public List<InitialUnitSetup> InitialSetup = new List<InitialUnitSetup>();
    public List<RoundRecord> Rounds = new List<RoundRecord>();
}

[Serializable]
public class RoundRecord
{
    public int RoundNumber;
    public List<string> Phase_RoundStart = new List<string>();
    public List<string> TurnOrder = new List<string>();
    public List<TurnRecord> Turns = new List<TurnRecord>();
    public List<string> Phase_RoundEnd = new List<string>();
}

[Serializable]
public class TurnRecord
{
    public string Caster;
    public List<string> Phase_TurnStart = new List<string>();
    public ActionRecord MainAction;
    public List<string> Phase_TurnEnd = new List<string>();
}

[Serializable]
public class ActionRecord
{
    public string ActionType;
    public string SkillName;
    public string Target;
    public string Result;
}