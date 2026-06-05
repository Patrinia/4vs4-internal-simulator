using System;
using UnityEngine;

// ====================================================
// [SkillEffect.cs]
// 1. 엑셀에서 파싱될 순수 데이터 구조체 (SkillEffectData)
// 2. 런타임에 유닛에게 부착될 상태이상 추상 클래스 (StatusEffectBase)
// ====================================================

// 1. 정적 데이터 규격 (Data Container)

/// <summary>
/// 상태이상의 종류를 정의합니다. 기획이 추가될 때마다 여기에 누적됩니다.
/// </summary>
public enum EffectType
{
    None = 0,
    AtkUp = 1,       // 공격력 증가 (기간제)
    DefDown = 2,     // 방어력 감소 (기간제)
    Burn = 3,        // 화상 (스택제)
    Bleed = 4,       // 출혈 (스택제)
    Stun = 5         // 기절 (기간제)
}

/// <summary>
/// 엑셀에서 "AtkUp_20_3" 형식으로 파싱되어 SkillData에 영구 저장될 순수 데이터입니다.
/// </summary>
[Serializable]
public struct SkillEffectData
{
    public EffectType type;
    public int value;
    public int duration;
}


// 2. 런타임 동적 객체 규격 (Runtime Object)

/// <summary>
/// 전투 중 유닛에게 부착되어 스스로 생명주기를 관리하는 상태이상 기본 클래스입니다.
/// </summary>
public abstract class StatusEffectBase
{
    public UnitControl caster;
    public UnitControl target;
    public EffectType type;
    public int value;
    public int duration;

    // 이 값이 true가 되면 StatusEffectManager가 쓰레기통에 버립니다.
    public bool isExpired { get; protected set; }

    public virtual void Init(UnitControl caster, UnitControl target, SkillEffectData data)
    {
        this.caster = caster;
        this.target = target;
        this.type = data.type;
        this.value = data.value;
        this.duration = data.duration;
        this.isExpired = false;
    }

    // 6단계 라이프사이클 훅 (하위 클래스에서 필요에 따라 오버라이드하여 씁니다)
    public virtual void OnRoundStart() { }
    public virtual void OnTurnStart() { }
    public virtual void OnTurnEnd() { }
    public virtual void OnRoundEnd() { }
}

/// <summary>
/// [하이브리드 A] 기간제 버프/디버프 (턴이 지날수록 Duration이 깎이는 타입)
/// </summary>
public abstract class DurationEffect : StatusEffectBase
{
    public override void OnRoundEnd()
    {
        duration--;
        if (duration <= 0) isExpired = true; // 수명이 다하면 소멸
    }
}

/// <summary>
/// [하이브리드 B] 스택제 버프/디버프 (턴이 지날수록 Value가 깎이는 타입)
/// </summary>
public abstract class StackEffect : StatusEffectBase
{
    // 스택제는 duration을 사용하지 않으며, 구체적인 감소 로직(절반, -1 등)은 
    // 이를 상속받을 BurnEffect, BleedEffect 등 개별 클래스에서 직접 제어합니다.
}
