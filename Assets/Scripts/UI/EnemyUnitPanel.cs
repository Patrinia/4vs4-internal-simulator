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

    // [신규 업데이트] SimulationUIManager가 중복 여부를 판별해 주는 델리게이트
    public delegate bool UnitValidationHandler(UnitData unit);
    public UnitValidationHandler OnValidateUnit;

    private int prevUnitIndex = 0;

    public void Initialize(List<UnitData> allUnits)
    {
        cachedUnitList = allUnits;

        dropdownSelectUnit.ClearOptions();
        List<string> options = new List<string> { "--- 선택 안함 ---" }; // 0번 인덱스에 빈칸 할당
        foreach (var u in allUnits) options.Add(u.unitName);
        dropdownSelectUnit.AddOptions(options);

        // 이벤트 리스너 등록
        dropdownSelectUnit.onValueChanged.AddListener(OnUnitSelectionChanged);

        // 초기화 시 빈칸(0번)으로 세팅
        OnUnitSelectionChanged(0);
    }

    private void OnUnitSelectionChanged(int index)
    {
        // [검증] 다른 패널과 중복된 적군을 골랐는지 매니저에게 물어봅니다.
        if (index > 0)
        {
            UnitData selected = cachedUnitList[index - 1]; // 0번이 '선택 안함'이므로 -1 오프셋
            if (OnValidateUnit != null && !OnValidateUnit.Invoke(selected))
            {
                Debug.LogWarning($"<color=orange>[UI] {selected.unitName}은(는) 이미 다른 슬롯에 배치되어 있습니다.</color>");
                // 이벤트 트리거 없이 조용히 이전 값으로 롤백합니다.
                dropdownSelectUnit.SetValueWithoutNotify(prevUnitIndex);
                return;
            }
        }
        prevUnitIndex = index;
    }

    public UnitData GetSelectedUnit()
    {
        if (dropdownSelectUnit.value == 0) return null;
        return cachedUnitList[dropdownSelectUnit.value - 1];
    }
}