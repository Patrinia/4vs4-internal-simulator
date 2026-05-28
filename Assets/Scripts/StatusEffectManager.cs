using System.Collections.Generic;

public class StatusEffectManager
{
    public void OnRoundStart(List<UnitControl> allUnits)
    {
        /* Phase 1-1 */
        /* 라운드 시작 시 효과 발동 */
    }
    public void OnRoundEnd(List<UnitControl> allUnits)
    {
        /* Phase 3-1 */
        /* 라운드 종료 시 효과 발동 */
    }

    public void OnTurnStart(UnitControl unit)
    {
        /* Phase 2-2: */
        /* 개별 유닛의 턴 시작 시 효과 발동 */
    }
    public void OnTurnEnd(UnitControl unit)
    {
        /* Phase 2 - 6 */
        /* 개별 유닛의 턴 종료 시 효과 발동*/
    }
}