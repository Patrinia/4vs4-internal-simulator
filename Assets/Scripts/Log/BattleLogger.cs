using UnityEngine;

// ====================================================
// [BattleLogger.cs]
// BattleLogEvents의 신호를 받아 실제 콘솔이나 UI에 텍스트를 출력하는 전담 아나운서입니다.
// 시뮬레이션 연산 시 성능을 보존하기 위해 로깅을 켜고 끌 수 있는 마스터 스위치를 제공합니다.
// ====================================================
public class BattleLogger : MonoBehaviour
{
    [Header("마스터 스위치")]
    [Tooltip("시뮬레이션을 수만 번 돌릴 때는 이 값을 false로 하여 성능을 극대화합니다.")]
    public static bool IsLoggingEnabled = true;

    private void OnEnable()
    {
        // 이벤트 구독 (Subscribe)
        BattleLogEvents.OnBattleStarted += HandleBattleStarted;
        BattleLogEvents.OnBattleEnded += HandleBattleEnded;
        BattleLogEvents.OnRoundStarted += HandleRoundStarted;
        BattleLogEvents.OnRoundEnded += HandleRoundEnded;
        BattleLogEvents.OnTurnStarted += HandleTurnStarted;
        BattleLogEvents.OnTurnEnded += HandleTurnEnded;

        BattleLogEvents.OnTurnSkipped += HandleTurnSkipped;
        BattleLogEvents.OnRandomActionForced += HandleRandomActionForced;

        BattleLogEvents.OnSkillCasted += HandleSkillCasted;
        BattleLogEvents.OnUtilityCasted += HandleUtilityCasted;
        BattleLogEvents.OnDamageDealt += HandleDamageDealt;
        BattleLogEvents.OnHealed += HandleHealed;

        BattleLogEvents.OnAttributeModified += HandleAttributeModified;
        BattleLogEvents.OnStatusEffectMerged += HandleStatusEffectMerged;
        BattleLogEvents.OnStatusEffectApplied += HandleStatusEffectApplied;
        BattleLogEvents.OnStatusEffectExpired += HandleStatusEffectExpired;
        BattleLogEvents.OnErosionReverted += HandleErosionReverted;

        BattleLogEvents.OnUnitMoved += HandleUnitMoved;
        BattleLogEvents.OnUnitSwapped += HandleUnitSwapped;
        BattleLogEvents.OnUnitDied += HandleUnitDied;
        BattleLogEvents.OnErosionDeath += HandleErosionDeath;
    }

    private void OnDisable()
    {
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        BattleLogEvents.OnBattleStarted -= HandleBattleStarted;
        BattleLogEvents.OnBattleEnded -= HandleBattleEnded;
        BattleLogEvents.OnRoundStarted -= HandleRoundStarted;
        BattleLogEvents.OnRoundEnded -= HandleRoundEnded;
        BattleLogEvents.OnTurnStarted -= HandleTurnStarted;
        BattleLogEvents.OnTurnEnded -= HandleTurnEnded;

        BattleLogEvents.OnTurnSkipped -= HandleTurnSkipped;
        BattleLogEvents.OnRandomActionForced -= HandleRandomActionForced;

        BattleLogEvents.OnSkillCasted -= HandleSkillCasted;
        BattleLogEvents.OnUtilityCasted -= HandleUtilityCasted;
        BattleLogEvents.OnDamageDealt -= HandleDamageDealt;
        BattleLogEvents.OnHealed -= HandleHealed;

        BattleLogEvents.OnAttributeModified -= HandleAttributeModified;
        BattleLogEvents.OnStatusEffectMerged -= HandleStatusEffectMerged;
        BattleLogEvents.OnStatusEffectApplied -= HandleStatusEffectApplied;
        BattleLogEvents.OnStatusEffectExpired -= HandleStatusEffectExpired;
        BattleLogEvents.OnErosionReverted -= HandleErosionReverted;

        BattleLogEvents.OnUnitMoved -= HandleUnitMoved;
        BattleLogEvents.OnUnitSwapped -= HandleUnitSwapped;
        BattleLogEvents.OnUnitDied -= HandleUnitDied;
        BattleLogEvents.OnErosionDeath -= HandleErosionDeath;
    }

    // ========================================================================
    // [로그 텍스트 가공 및 출력부]
    // IsLoggingEnabled가 false일 경우 즉시 return하여 문자열 연산 부하(GC)를 차단합니다.
    // ========================================================================

    private void HandleBattleStarted()
    {
        if (!IsLoggingEnabled) return;
        Debug.Log("<color=green><b>[엔진 점화]</b> 전투를 시작합니다.</color>");
    }

    private void HandleBattleEnded()
    {
        if (!IsLoggingEnabled) return;
        Debug.Log("<color=red><b>[전투 종료]</b> 전투가 완전히 종료되었습니다.</color>");
    }

    private void HandleRoundStarted(int round)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=cyan>=== 라운드 {round} 시작 ===</color>");
    }

    private void HandleRoundEnded(int round)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=orange>=== 라운드 {round} 종료 ===</color>");
    }

    private void HandleTurnStarted(UnitControl unit)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"[{unit.unitName}] 턴 시작.");
    }

    private void HandleTurnEnded(UnitControl unit)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"[{unit.unitName}] 턴 종료.");
    }

    private void HandleTurnSkipped(UnitControl unit, string reason)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"[{unit.unitName}] {reason} 상태입니다. 턴을 건너뜁니다.");
    }

    private void HandleRandomActionForced(UnitControl unit)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=red>[{unit.unitName}] 제어권을 상실했습니다! 무작위로 행동합니다.</color>");
    }

    private void HandleSkillCasted(UnitControl caster, UnitControl target, SkillData skill)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=yellow>[{caster.unitName}] (이)가 [{skill.skillName}] 스킬을 시전!</color>");
    }

    private void HandleUtilityCasted(UnitControl caster, UnitControl target, SkillData skill)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=yellow>[{caster.unitName}]가 [{target.unitName}]에게 기믹/유틸리티({skill.skillName})을 시전했습니다.</color>");
    }

    private void HandleDamageDealt(UnitControl caster, UnitControl target, int damage, string targetTypeStr)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=red>[{caster.unitName}]가 [{target.unitName}]({targetTypeStr})에게 {damage}의 데미지를 입혔습니다!</color>");
    }

    private void HandleHealed(UnitControl caster, UnitControl target, int amount, string targetTypeStr)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=green>[{caster.unitName}]가 [{target.unitName}]({targetTypeStr})의 체력을 {amount}만큼 회복시켰습니다!</color>");
    }

    private void HandleAttributeModified(UnitControl target, AttributeType type, int amount)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=cyan>[기믹] {target.unitName}의 {type} 속성이 {amount}만큼 변화했습니다.</color>");
    }

    private void HandleStatusEffectMerged(UnitControl target, EffectType type, int value, int duration)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=magenta>[상태이상 중첩] {target.unitName}의 {type} 중첩 병합! (총 위력:{value}, 지속:{duration})</color>");
    }

    private void HandleStatusEffectApplied(UnitControl target, EffectType type, int value)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=magenta>[상태이상] {target.unitName}에게 {type}(위력:{value}) 부여됨!</color>");
    }

    private void HandleStatusEffectExpired(UnitControl target, EffectType type)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=grey>[해제] {target.unitName}의 {type} 상태가 해제되었습니다.</color>");
    }

    private void HandleErosionReverted(UnitControl unit, string stateName)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=cyan><b>[{unit.unitName}]</b>의 {stateName} 상태가 해제되어 수치가 기준점(50)으로 복귀합니다.</color>");
    }

    private void HandleUnitMoved(UnitControl unit, int targetIndex)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=white>[진형 변경] {unit.unitName}이(가) 빈칸({targetIndex}번 슬롯)으로 이동했습니다.</color>");
    }

    private void HandleUnitSwapped(UnitControl unitA, UnitControl unitB)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=white>[진형 변경] {unitA.unitName}이(가) {unitB.unitName}와(과) 자리를 교환했습니다.</color>");
    }

    private void HandleUnitDied(UnitControl unit)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=black><b>[사망]</b> {unit.unitName}이(가) 쓰러졌습니다!</color>");
    }

    private void HandleErosionDeath(UnitControl unit, int value)
    {
        if (!IsLoggingEnabled) return;
        Debug.Log($"<color=purple>[침식 붕괴] {unit.unitName}의 음/양 수치가 한계를 돌파({value})하여 특수 사망합니다!</color>");
    }
}