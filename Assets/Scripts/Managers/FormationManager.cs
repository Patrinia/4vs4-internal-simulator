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
    /// (추후 RoundManager에서 이 함수를 호출하여 초기화합니다.)
    /// </summary>
    public void InitializeFormation(List<UnitControl> players, List<UnitControl> enemies)
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            playerSlots[i] = (i < players.Count) ? players[i] : null;
            enemySlots[i] = (i < enemies.Count) ? enemies[i] : null;
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
        }
    }
}