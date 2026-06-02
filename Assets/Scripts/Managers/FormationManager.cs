using System.Collections.Generic;
using UnityEngine;

// ====================================================
// [FormationManager.cs]
// 1차원 진형(0~3번 슬롯)을 관리하는 중앙 통제소입니다.
// 유닛의 물리적 위치(Index)를 추적하고, 오프셋(-1, +1 등)을 
// 활용한 서브 타겟(부분 광역기) 연산을 안전하게 수행합니다.
// ====================================================
public class FormationManager
{
    // 각 진영당 최대 슬롯 수 고정 (0번: 최전방, 3번: 최후방)
    private const int MAX_SLOTS = 4;

    // 배열을 활용하여 빈자리(null)의 개념을 명확하게 표현합니다.
    private UnitControl[] playerSlots = new UnitControl[MAX_SLOTS];
    private UnitControl[] enemySlots = new UnitControl[MAX_SLOTS];

    /// <summary>
    /// 전투 진입 시 유닛들을 진형 배열에 차례대로 배치합니다.
    /// </summary>
    public void InitializeFormation(List<UnitControl> players, List<UnitControl> enemies)
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            playerSlots[i] = (i < players.Count) ? players[i] : null;
            // 유닛의 뇌에 자신의 인덱스를 직접 캐싱(주입)합니다.
            if (playerSlots[i] != null) playerSlots[i].positionIndex = i;

            enemySlots[i] = (i < enemies.Count) ? enemies[i] : null;
            // 유닛의 뇌에 자신의 인덱스를 직접 캐싱(주입)합니다.
            if (enemySlots[i] != null) enemySlots[i].positionIndex = i;
        }
    }

    /// <summary>
    /// 특정 유닛이 현재 몇 번 슬롯(Index)에 있는지 찾아서 반환합니다.
    /// </summary>
    public int GetUnitIndex(UnitControl unit)
    {
        if (unit == null) return -1;

        UnitControl[] targetArray = unit.isPlayer ? playerSlots : enemySlots;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (targetArray[i] == unit) return i;
        }

        return -1; // 진형에 존재하지 않음 (사망 후 이탈 등)
    }

    /// <summary>
    /// 메인 타겟의 위치를 기준으로 오프셋(상대 위치)을 계산하여, 
    /// 합법적으로 공격 가능한 서브 타겟 유닛들의 리스트를 반환합니다. (바운드 체크 포함)
    /// </summary>
    public List<UnitControl> GetSubTargets(UnitControl mainTarget, List<int> offsets)
    {
        List<UnitControl> validSubTargets = new List<UnitControl>();

        // 안전 장치: 메인 타겟이 없거나 오프셋(범위) 데이터가 없다면 빈 리스트 반환
        if (mainTarget == null || offsets == null || offsets.Count == 0)
            return validSubTargets;

        int mainIndex = GetUnitIndex(mainTarget);
        if (mainIndex == -1) return validSubTargets; // 메인 타겟이 진형에 없음

        UnitControl[] targetArray = mainTarget.isPlayer ? playerSlots : enemySlots;

        // 엑셀에서 파싱한 오프셋(-1, +1 등)을 하나씩 검사합니다.
        foreach (int offset in offsets)
        {
            int subTargetIndex = mainIndex + offset;

            // [핵심 최적화 & 예외 처리] 배열의 경계(0~3)를 벗어나지 않는지 확인합니다. (IndexOutOfRange 방어)
            if (subTargetIndex >= 0 && subTargetIndex < MAX_SLOTS)
            {
                UnitControl subUnit = targetArray[subTargetIndex];

                // 해당 슬롯에 유닛이 실제로 존재하며, 사망하지 않은 상태인지 최종 확인
                if (subUnit != null && !subUnit.isDead)
                {
                    validSubTargets.Add(subUnit);
                }
            }
        }

        return validSubTargets;
    }

    /// <summary>
    /// 유닛이 사망하거나 진형에서 이탈할 때 슬롯을 공석(null)으로 만듭니다.
    /// </summary>
    public void RemoveUnit(UnitControl unit)
    {
        int index = GetUnitIndex(unit);
        if (index != -1)
        {
            if (unit.isPlayer) playerSlots[index] = null;
            else enemySlots[index] = null;

            // [업데이트] 진형 이탈 시 인덱스 초기화
            unit.positionIndex = -1;
        }
    }

    /// <summary>
    /// 향후 '위치 이동 스킬' 발동 시 두 유닛의 슬롯 위치를 교환(Swap)합니다.
    /// </summary>
    public void SwapUnits(UnitControl unitA, UnitControl unitB)
    {
        if (unitA == null || unitB == null || unitA.isPlayer != unitB.isPlayer) return;

        UnitControl[] targetArray = unitA.isPlayer ? playerSlots : enemySlots;
        int indexA = GetUnitIndex(unitA);
        int indexB = GetUnitIndex(unitB);

        if (indexA != -1 && indexB != -1)
        {
            // 배열 내 위치 스왑 알고리즘
            UnitControl temp = targetArray[indexA];
            targetArray[indexA] = targetArray[indexB];
            targetArray[indexB] = temp;

            // [업데이트] 캐싱된 positionIndex 데이터도 서로 교환합니다.
            unitA.positionIndex = indexB;
            unitB.positionIndex = indexA;
        }
    }

    // ========================================================================
    // [빈칸 이동 및 통합 진형 제어 시스템]
    // ========================================================================

    /// <summary>
    /// 유닛을 목표 슬롯(Index)으로 이동시킵니다. 
    /// 목표 지점이 빈칸이면 순수 이동(Move)을, 유닛이 있다면 교환(Swap)을 수행합니다.
    /// </summary>
    public void MoveUnitToSlot(UnitControl unit, int targetIndex)
    {
        if (unit == null || unit.isDead) return;

        // [업데이트 복구] 진형 이탈 방지 및 벽면 넉백 보정(Clamping)
        // AI의 ValidRange 오프셋 연산 결과나 넉백/당기기가 맵 밖을 가리켜도 가장 끝 슬롯으로 안전하게 고정시킵니다.
        targetIndex = Mathf.Clamp(targetIndex, 0, MAX_SLOTS - 1);

        int currentIndex = GetUnitIndex(unit);
        if (currentIndex == -1 || currentIndex == targetIndex) return; // 이미 제자리거나 진형에 없음

        UnitControl[] targetArray = unit.isPlayer ? playerSlots : enemySlots;
        UnitControl targetOccupant = targetArray[targetIndex];

        if (targetOccupant == null)
        {
            // 1. 목표 지점이 빈칸일 경우: 내 자리 비우기 + 새 자리 차지 (순수 이동)
            targetArray[targetIndex] = unit;
            targetArray[currentIndex] = null;

            // 캐싱된 인덱스 갱신
            unit.positionIndex = targetIndex;
            Debug.Log($"<color=white>[진형 변경] {unit.unitName}이(가) 빈칸({targetIndex}번 슬롯)으로 이동했습니다.</color>");
        }
        else
        {
            // 2. 목표 지점에 다른 유닛이 있을 경우: 기존 Swap 로직 재사용
            SwapUnits(unit, targetOccupant);
            Debug.Log($"<color=white>[진형 변경] {unit.unitName}이(가) {targetOccupant.unitName}와(과) 자리를 교환했습니다.</color>");
        }
    }

    // ========================================================================
    // [절대 좌표 기반 사거리 계산 시스템]
    // ========================================================================

    /// <summary>
    /// 두 유닛 간의 1차원 절대 좌표 기반 물리적 거리를 계산하여 반환합니다.
    /// </summary>
    public int GetDistance(UnitControl unitA, UnitControl unitB)
    {
        if (unitA == null || unitB == null) return -1;

        // 진형에 포함되어 있지 않은(사망 등) 경우 예외 처리
        if (unitA.positionIndex == -1 || unitB.positionIndex == -1) return -1;

        int absolutePosA = GetAbsolutePosition(unitA);
        int absolutePosB = GetAbsolutePosition(unitB);

        // 절댓값 연산으로 항상 양수의 거리를 반환
        return Mathf.Abs(absolutePosA - absolutePosB);
    }

    /// <summary>
    /// 내부 헬퍼 함수: 아군(0~3)과 적군(0~3)의 분리된 상대 좌표를 
    /// 체스판 전체(0~7)의 통합 절대 좌표로 환산합니다.
    /// </summary>
    private int GetAbsolutePosition(UnitControl unit)
    {
        // 아군 진형 (3, 2, 1, 0) : 최전방인 0번 슬롯이 3이 됨
        if (unit.isPlayer)
        {
            return (MAX_SLOTS - 1) - unit.positionIndex;
        }
        // 적군 진형 (4, 5, 6, 7) : 최전방인 0번 슬롯이 4가 됨
        else
        {
            return MAX_SLOTS + unit.positionIndex;
        }
    }
}