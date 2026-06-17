using System;
using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [BattleLogEvents.cs]
// 단일 책임 원칙(SRP) 및 의존성 역전 원칙(DIP)에 따라,
// 코어 매니저들이 문자열을 조립하는 대신 순수 데이터만 전달하는 이벤트 버스(Event Bus)입니다.
// C#의 이벤트 캡슐화(Encapsulation) 원칙을 준수하기 위해, 외부 접근용 Broadcast 래퍼 메서드를 제공합니다.
// ====================================================
public static class BattleLogEvents
{
    // ----------------------------------------------------
    // [1] 전투 흐름 (Battle Flow)
    // ----------------------------------------------------
    // 거시적 시뮬레이션 세션 세트의 시작과 종료를 관리하기 위한 독립 이벤트 버스 추가
    public static event Action OnSimulationSessionStarted;
    public static void BroadcastSimulationSessionStarted() => OnSimulationSessionStarted?.Invoke();

    public static event Action OnSimulationSessionEnded;
    public static void BroadcastSimulationSessionEnded() => OnSimulationSessionEnded?.Invoke();

    public static event Action OnBattleStarted;
    public static void BroadcastBattleStarted() => OnBattleStarted?.Invoke();

    public static event Action OnBattleEnded;
    public static void BroadcastBattleEnded() => OnBattleEnded?.Invoke();

    public static event Action<int> OnRoundStarted;
    public static void BroadcastRoundStarted(int round) => OnRoundStarted?.Invoke(round);

    public static event Action<int> OnRoundEnded;
    public static void BroadcastRoundEnded(int round) => OnRoundEnded?.Invoke(round);

    public static event Action<UnitControl> OnTurnStarted;
    public static void BroadcastTurnStarted(UnitControl unit) => OnTurnStarted?.Invoke(unit);

    public static event Action<UnitControl> OnTurnEnded;
    public static void BroadcastTurnEnded(UnitControl unit) => OnTurnEnded?.Invoke(unit);

    // 무한 루프 강제 종료(타임아웃) 및 턴 행동 순서 이벤트
    public static event Action<int> OnBattleTimeout;
    public static void BroadcastBattleTimeout(int round) => OnBattleTimeout?.Invoke(round);

    public static event Action<List<UnitControl>> OnTurnOrderCalculated;
    public static void BroadcastTurnOrderCalculated(List<UnitControl> turnOrder) => OnTurnOrderCalculated?.Invoke(turnOrder);

    // ----------------------------------------------------
    // [2] 상태 및 제어권 (Status & Control)
    // ----------------------------------------------------
    public static event Action<UnitControl, string> OnTurnSkipped; // 기절, 침식 등으로 인한 스킵
    public static void BroadcastTurnSkipped(UnitControl unit, string reason) => OnTurnSkipped?.Invoke(unit, reason);

    public static event Action<UnitControl> OnRandomActionForced;  // 양기 과다로 인한 통제 상실
    public static void BroadcastRandomActionForced(UnitControl unit) => OnRandomActionForced?.Invoke(unit);

    // ----------------------------------------------------
    // [3] 스킬 및 전투 연산 (Combat Execution)
    // ----------------------------------------------------
    public static event Action<UnitControl, UnitControl, SkillData> OnSkillCasted;
    public static void BroadcastSkillCasted(UnitControl caster, UnitControl target, SkillData skill) => OnSkillCasted?.Invoke(caster, target, skill);

    public static event Action<UnitControl, UnitControl, SkillData> OnUtilityCasted;
    public static void BroadcastUtilityCasted(UnitControl caster, UnitControl target, SkillData skill) => OnUtilityCasted?.Invoke(caster, target, skill);

    public static event Action<UnitControl, UnitControl, int, string> OnDamageDealt; // (시전자, 타겟, 수치, 타겟종류)
    public static void BroadcastDamageDealt(UnitControl caster, UnitControl target, int damage, string targetTypeStr) => OnDamageDealt?.Invoke(caster, target, damage, targetTypeStr);

    public static event Action<UnitControl, UnitControl, int, string> OnHealed;
    public static void BroadcastHealed(UnitControl caster, UnitControl target, int amount, string targetTypeStr) => OnHealed?.Invoke(caster, target, amount, targetTypeStr);

    // ----------------------------------------------------
    // [4] 상태이상 및 기믹 (Status Effects & Gimmicks)
    // ----------------------------------------------------
    public static event Action<UnitControl, AttributeType, int> OnAttributeModified; // 속성 게이지 증감
    public static void BroadcastAttributeModified(UnitControl target, AttributeType type, int amount) => OnAttributeModified?.Invoke(target, type, amount);

    public static event Action<UnitControl, EffectType, int, int> OnStatusEffectMerged; // 중첩 병합
    public static void BroadcastStatusEffectMerged(UnitControl target, EffectType type, int value, int duration) => OnStatusEffectMerged?.Invoke(target, type, value, duration);

    public static event Action<UnitControl, EffectType, int> OnStatusEffectApplied;  // 신규 부여
    public static void BroadcastStatusEffectApplied(UnitControl target, EffectType type, int value) => OnStatusEffectApplied?.Invoke(target, type, value);

    public static event Action<UnitControl, EffectType> OnStatusEffectExpired;       // 만료/해제
    public static void BroadcastStatusEffectExpired(UnitControl target, EffectType type) => OnStatusEffectExpired?.Invoke(target, type);

    public static event Action<UnitControl, string> OnCorrosionReverted;               // 50으로 기준점 복귀(구제)
    public static void BroadcastCorrosionReverted(UnitControl unit, string stateName) => OnCorrosionReverted?.Invoke(unit, stateName);

    // ----------------------------------------------------
    // [5] 진형 및 생사 (Formation & Death)
    // ----------------------------------------------------
    public static event Action<UnitControl, int> OnUnitMoved;
    public static void BroadcastUnitMoved(UnitControl unit, int targetIndex) => OnUnitMoved?.Invoke(unit, targetIndex);

    public static event Action<UnitControl, UnitControl> OnUnitSwapped;
    public static void BroadcastUnitSwapped(UnitControl unitA, UnitControl unitB) => OnUnitSwapped?.Invoke(unitA, unitB);

    public static event Action<UnitControl> OnUnitDied;
    public static void BroadcastUnitDied(UnitControl unit) => OnUnitDied?.Invoke(unit);

    public static event Action<UnitControl, int> OnCorrosionDeath; // 침식 임계점 돌파 즉사
    public static void BroadcastCorrosionDeath(UnitControl unit, int value) => OnCorrosionDeath?.Invoke(unit, value);
}