using System.Collections.Generic;
using System.Linq;

public class SpeedTurnSorter : ITurnSorter
{
    public Queue<UnitControl> BuildTurnQueue(List<UnitControl> aliveUnits)
    {
        /* Phase 1-2 */
        /* 속도 계산 */
        foreach (var unit in aliveUnits)
        {
            //UnitConrol에 있는 메소드
            unit.RollCurrentSpeed(); // Assumes UnitControl has this method
                                
        }

        /* Phase 1-3: Queueing */
        /* 현재 속도 높은 순서대로 유닛들 큐에 넣기 */
        /* 동일한 속도 유닛들끼리는 랜덤하게 넣어짐 */
        var sortedList = aliveUnits
            .OrderByDescending(u => u.currentSpeed)
            .ThenBy(u => System.Guid.NewGuid())
            .ToList();

        return new Queue<UnitControl>(sortedList);
    }
}