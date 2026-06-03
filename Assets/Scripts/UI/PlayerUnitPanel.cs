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

    // SimulationUIManager가 중복 여부를 판별해 주는 델리게이트
    public delegate bool UnitValidationHandler(UnitData unit);
    public UnitValidationHandler OnValidateUnit;

    // 롤백(Rollback)을 위한 이전 인덱스 캐싱 변수
    private int prevUnitIndex = 0;
    private int[] prevSkillIndices = new int[4];

    public void Initialize(List<UnitData> allUnits)
    {
        cachedUnitList = allUnits;

        // 유닛 선택 드롭다운 갱신
        dropdownSelectUnit.ClearOptions();
        List<string> options = new List<string> { "--- 선택 안함 ---" }; // 0번 인덱스에 빈칸 할당
        foreach (var u in allUnits) options.Add(u.unitName);
        dropdownSelectUnit.AddOptions(options);

        // 이벤트 리스너 등록
        dropdownSelectUnit.onValueChanged.AddListener(OnUnitSelectionChanged);

        // 스킬 드롭다운에도 중복 방지 리스너 등록
        for (int i = 0; i < dropdownNormalSkills.Length; i++)
        {
            int capturedIndex = i; // 클로저 이슈 방지
            dropdownNormalSkills[i].onValueChanged.AddListener((val) => OnSkillSelectionChanged(capturedIndex, val));
        }

        // 초기화 시 빈칸(0번)으로 세팅
        OnUnitSelectionChanged(0);
    }

    private void OnUnitSelectionChanged(int index)
    {
        // [검증] 다른 패널과 중복된 유닛을 골랐는지 매니저에게 물어봅니다.
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

        // 0번("--- 선택 안함 ---")을 골랐다면 모든 스킬 드롭다운을 잠그고 비웁니다.
        if (index == 0)
        {
            dropdownMoveSkill.ClearOptions();
            dropdownUltSkill.ClearOptions();
            for (int i = 0; i < dropdownNormalSkills.Length; i++)
            {
                dropdownNormalSkills[i].ClearOptions();
                dropdownNormalSkills[i].interactable = false;
                prevSkillIndices[i] = 0;
            }
            return;
        }

        UnitData currentUnit = cachedUnitList[index - 1];

        // 1. 이동 스킬 갱신
        dropdownMoveSkill.ClearOptions();
        if (currentUnit.movementSkill != null)
        {
            dropdownMoveSkill.AddOptions(new List<string> { currentUnit.movementSkill.skillName });
        }

        // 2. 필살기 스킬 갱신
        dropdownUltSkill.ClearOptions();
        List<string> ultOptions = new List<string>();
        foreach (var s in currentUnit.ultimateSkillPool) ultOptions.Add(s.skillName);
        dropdownUltSkill.AddOptions(ultOptions);

        // 3. 일반 스킬 스마트 갱신 (기획자님 제안 100% 반영)
        List<string> normOptions = new List<string> { "--- 없음 ---" };
        foreach (var s in currentUnit.normalSkillPool) normOptions.Add(s.skillName);

        for (int i = 0; i < dropdownNormalSkills.Length; i++)
        {
            dropdownNormalSkills[i].ClearOptions();
            dropdownNormalSkills[i].AddOptions(normOptions);

            // 유닛이 가진 스킬 개수 범위 안이라면 순차적으로 할당하고 조작을 엽니다.
            if (i < currentUnit.normalSkillPool.Count)
            {
                dropdownNormalSkills[i].interactable = true;
                dropdownNormalSkills[i].SetValueWithoutNotify(i + 1); // 1, 2, 3... 번 스킬 자동 할당
                prevSkillIndices[i] = i + 1;
            }
            // 스킬이 모자란 잉여 슬롯이라면 0번("--- 없음 ---")으로 고정하고 잠급니다.
            else
            {
                dropdownNormalSkills[i].interactable = false;
                dropdownNormalSkills[i].SetValueWithoutNotify(0);
                prevSkillIndices[i] = 0;
            }
        }
    }

    // 패널 내에서의 스킬 중복 선택을 방어하는 롤백 함수
    private void OnSkillSelectionChanged(int dropdownIndex, int selectedSkillIndex)
    {
        if (selectedSkillIndex > 0) // "--- 없음 ---"이 아닐 때만 검사
        {
            for (int i = 0; i < dropdownNormalSkills.Length; i++)
            {
                if (i != dropdownIndex && dropdownNormalSkills[i].value == selectedSkillIndex)
                {
                    Debug.LogWarning("<color=orange>[UI] 동일한 스킬을 중복해서 장착할 수 없습니다.</color>");
                    dropdownNormalSkills[dropdownIndex].SetValueWithoutNotify(prevSkillIndices[dropdownIndex]);
                    return;
                }
            }
        }
        prevSkillIndices[dropdownIndex] = selectedSkillIndex;
    }

    // 선택된 데이터 반환 API
    public UnitData GetSelectedUnit()
    {
        if (dropdownSelectUnit.value == 0) return null;
        return cachedUnitList[dropdownSelectUnit.value - 1];
    }

    public SkillData GetSelectedMoveSkill()
    {
        UnitData unit = GetSelectedUnit();
        if (unit == null) return null;
        return unit.movementSkill; // 이동기는 고정이므로 풀에서 반환
    }

    public SkillData GetSelectedUltSkill()
    {
        UnitData unit = GetSelectedUnit();
        if (unit == null || unit.ultimateSkillPool.Count == 0 || dropdownUltSkill.options.Count == 0) return null;
        return unit.ultimateSkillPool[dropdownUltSkill.value];
    }

    public List<SkillData> GetSelectedNormalSkills()
    {
        List<SkillData> selectedSkills = new List<SkillData>();
        UnitData unit = GetSelectedUnit();
        if (unit == null) return selectedSkills;

        foreach (var dropdown in dropdownNormalSkills)
        {
            // 0번("--- 없음 ---")으로 지정된 스킬 슬롯은 건너뛰고 정상 스킬만 추출합니다.
            if (dropdown.value > 0)
            {
                selectedSkills.Add(unit.normalSkillPool[dropdown.value - 1]);
            }
        }
        return selectedSkills;
    }
}