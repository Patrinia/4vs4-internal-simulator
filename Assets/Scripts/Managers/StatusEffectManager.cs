using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [StatusEffectManager.cs]
// 전투의 6단계 라이프사이클 타이밍에 맞춰 상태 이상, 쿨타임, 
// 그리고 전역 기믹의 효과 발동을 관리하는 전담 관리자입니다.
// ====================================================
public class StatusEffectManager
{
    /// <summary>
    /// 1단계: 전투 진입 시 최초 1회 발동
    /// </summary>
    public void OnBattleStart(List<UnitControl> allUnits)
    {
        /* 전역 기믹 초기화, 영구 패시브 효과 적용 및 유닛별 초기 세팅 */
    }

    /// <summary>
    /// 2단계: 매 라운드 시작 시 발동 (Phase 1-1)
    /// </summary>
    public void OnRoundStart(List<UnitControl> allUnits)
    {
        /* 라운드 시작 시 지속 효과 정산 및 스킬 쿨타임 감소 등 */
    }

    /// <summary>
    /// 3단계: 개별 유닛의 턴 시작 시 발동 (Phase 2-2)
    /// </summary>
    public void OnTurnStart(UnitControl unit)
    {
        /* 턴 시작 시 도트 데미지 정산, 속성 변화 디버프 처리 등 */
    }

    /// <summary>
    /// 4단계: 개별 유닛의 행동이 끝난 후 턴 종료 시 발동 (Phase 2-6)
    /// </summary>
    public void OnTurnEnd(UnitControl unit)
    {
        /* 턴 종료 시 1턴짜리 버프 수명 감소 및 상태 갱신 */
    }

    /// <summary>
    /// 5단계: 모든 유닛이 행동을 마친 후 라운드 종료 시 발동 (Phase 3-1)
    /// 침식 상태 복귀 기믹 작성 되어 있음
    /// </summary>
    public void OnRoundEnd(List<UnitControl> allUnits)
    {
        // 1. 살아있는 모든 유닛을 검사하여 침식 상태 해제 (기획 A)
        foreach (UnitControl unit in allUnits)
        {
            if (unit.isDead) continue; // 사망한 유닛은 연산에서 제외

            if (unit.currentAttributes.TryGetValue(AttributeType.YinYang, out int yyValue))
            {
                // 음기 침식(0~10) 또는 양기 침식(90~100) 상태인지 확인
                if ((yyValue >= 0 && yyValue <= 10) || (yyValue >= 90 && yyValue <= 100))
                {
                    string stateName = yyValue <= 10 ? "음기 침식" : "양기 과다";
                    Debug.Log($"<color=cyan><b>[{unit.unitName}]</b>의 {stateName} 상태가 해제되어 수치가 기준점(50)으로 복귀합니다.</color>");

                    // 수치를 완벽한 조화(50)로 강제 리셋
                    unit.currentAttributes[AttributeType.YinYang] = 50;
                }
            }
        }

        /* 기타 라운드 종료 시 전장 환경 변화 정산 등 추가 가능 */
    }

    /// <summary>
    /// 6단계: 승패가 결정되어 전투가 최종 종료될 때 발동
    /// </summary>
    public void OnBattleEnd(List<UnitControl> allUnits)
    {
        /* 전투 결과 데이터 저장 트리거 및 승리/패배 상태 정산 */
    }
}