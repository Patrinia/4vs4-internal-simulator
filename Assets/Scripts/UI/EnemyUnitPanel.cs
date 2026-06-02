using UnityEngine;
using TMPro;
using System.Collections.Generic;

// ====================================================
// [EnemyUnitPanel.cs]
// 적군 유닛 1명의 세팅을 담당하는 UI 래퍼 클래스입니다.
// EnemyUnitSettingPanel 프리팹의 최상단에 부착됩니다.
// ====================================================
public class EnemyUnitPanel : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownSelectUnit;

    private List<UnitData> cachedUnitList;

    public void Initialize(List<UnitData> allUnits)
    {
        cachedUnitList = allUnits;

        dropdownSelectUnit.ClearOptions();
        List<string> options = new List<string>();
        foreach (var u in allUnits) options.Add(u.unitName);
        dropdownSelectUnit.AddOptions(options);
    }

    public UnitData GetSelectedUnit() => cachedUnitList[dropdownSelectUnit.value];
}