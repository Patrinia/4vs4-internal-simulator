using UnityEngine;
using TMPro;
using System.Collections.Generic;

// ====================================================
// [PlayerUnitPanel.cs]
// 아군 유닛 1명의 세팅(이동, 필살, 일반스킬 4개)을 담당하는 UI 래퍼 클래스입니다.
// PlayerUnitSettingPanel 프리팹의 최상단에 부착됩니다.
// ====================================================
public class PlayerUnitPanel : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownSelectUnit;
    [SerializeField] private TMP_Dropdown dropdownMoveSkill;
    [SerializeField] private TMP_Dropdown dropdownUltSkill;
    [SerializeField] private TMP_Dropdown[] dropdownNormalSkills = new TMP_Dropdown[4];

    private List<UnitData> cachedUnitList;

    public void Initialize(List<UnitData> allUnits)
    {
        cachedUnitList = allUnits;

        // 유닛 선택 드롭다운 갱신
        dropdownSelectUnit.ClearOptions();
        List<string> options = new List<string>();
        foreach (var u in allUnits) options.Add(u.unitName);
        dropdownSelectUnit.AddOptions(options);

        // 이벤트 리스너 등록
        dropdownSelectUnit.onValueChanged.AddListener(OnUnitSelectionChanged);

        // 초기 1회 강제 갱신
        if (allUnits.Count > 0) OnUnitSelectionChanged(0);
    }

    private void OnUnitSelectionChanged(int index)
    {
        UnitData selected = cachedUnitList[index];

        // 1. 이동 스킬 갱신
        dropdownMoveSkill.ClearOptions();
        if (selected.movementSkill != null)
        {
            dropdownMoveSkill.AddOptions(new List<string> { selected.movementSkill.skillName });
        }

        // 2. 필살기 스킬 갱신
        dropdownUltSkill.ClearOptions();
        List<string> ultOptions = new List<string>();
        foreach (var s in selected.ultimateSkillPool) ultOptions.Add(s.skillName);
        dropdownUltSkill.AddOptions(ultOptions);

        // 3. 일반 스킬 갱신 (4개의 드롭다운을 동일한 풀로 갱신)
        List<string> normOptions = new List<string>();
        foreach (var s in selected.normalSkillPool) normOptions.Add(s.skillName);

        for (int i = 0; i < dropdownNormalSkills.Length; i++)
        {
            dropdownNormalSkills[i].ClearOptions();
            dropdownNormalSkills[i].AddOptions(normOptions);
        }
    }

    // 선택된 데이터 반환 API
    public UnitData GetSelectedUnit() => cachedUnitList[dropdownSelectUnit.value];

    public SkillData GetSelectedMoveSkill()
    {
        UnitData unit = GetSelectedUnit();
        return unit.movementSkill; // 이동기는 고정이므로 풀에서 반환
    }

    public SkillData GetSelectedUltSkill()
    {
        UnitData unit = GetSelectedUnit();
        if (unit.ultimateSkillPool.Count == 0) return null;
        return unit.ultimateSkillPool[dropdownUltSkill.value];
    }

    public List<SkillData> GetSelectedNormalSkills()
    {
        List<SkillData> selectedSkills = new List<SkillData>();
        UnitData unit = GetSelectedUnit();

        if (unit.normalSkillPool.Count > 0)
        {
            foreach (var dropdown in dropdownNormalSkills)
            {
                selectedSkills.Add(unit.normalSkillPool[dropdown.value]);
            }
        }
        return selectedSkills;
    }
}