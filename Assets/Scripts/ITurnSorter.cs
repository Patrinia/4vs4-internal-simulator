using System.Collections.Generic;

public interface ITurnSorter
{
    // 큐 만들고 내부의 유닛 반환함
    Queue<UnitControl> BuildTurnQueue(List<UnitControl> aliveUnits);
}