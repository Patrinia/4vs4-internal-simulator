using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

// ====================================================
// [CSVToSOParser.cs]
// 기획 엑셀(CSV) 데이터를 ScriptableObject로 자동 변환하고
// Addressable 그룹에 등록하는 에디터 전용 자동화 툴입니다.
// 단일 책임 원칙(SRP)에 따라 파싱과 파일 생성 역할만 수행합니다.
// ====================================================
public class CSVToSOParser : EditorWindow
{
    // 원본 CSV 파일이 위치할 경로
    private static readonly string csvFolderPath = "Assets/Data/DesignData/CSV_Raw";

    // 생성된 ScriptableObject가 저장될 경로
    private static readonly string unitDataPath = "Assets/Data/GameData/Units";
    private static readonly string skillDataPath = "Assets/Data/GameData/Skills";

    // 유니티 상단 메뉴바에 실행 버튼 생성
    [MenuItem("Game Tools/엑셀 데이터 파싱 및 어드레서블 등록")]
    public static void ParseCSVData()
    {
        // 1. 데이터가 저장될 폴더가 없다면 자동으로 생성합니다.
        EnsureDirectoryExists(unitDataPath);
        EnsureDirectoryExists(skillDataPath);

        //새로 생성된 폴더 구조를 유니티 에디터가 즉시 인식하도록 강제 새로고침합니다.
        AssetDatabase.Refresh();

        // 2. 파싱 실행
        ParseSkillData();
        ParseUnitData();

        // 3. 변경된 에셋들을 모두 저장하고 유니티 데이터베이스를 새로고침합니다.
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green><b>[파싱 완료]</b> 모든 CSV 데이터가 성공적으로 변환 및 어드레서블에 등록되었습니다.</color>");
    }

    // ========================================================================
    // [스킬 데이터 파싱 로직]
    // ========================================================================
    private static void ParseSkillData()
    {
        string filePath = $"{csvFolderPath}/SkillDataTable.csv";
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[경고] {filePath} 파일을 찾을 수 없습니다.");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        // 첫 번째 줄(Header)은 건너뛰고 2번째 줄(인덱스 1)부터 읽습니다.
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] columns = lines[i].Split(',');

            // CSV 컬럼 순서 {ID(0), Name(1), Category(2), Desc(3), Cooldown(4), MinPower(5), MaxPower(6), TargetType(7), Tendencies(8), AttributesModifiers(9)}
            string skillID = columns[0];

            string assetPath = $"{skillDataPath}/{skillID}.asset";
            SkillData skillData = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
            bool isNew = false;

            if (skillData == null)
            {
                skillData = ScriptableObject.CreateInstance<SkillData>();
                isNew = true;
            }

            // 데이터 덮어쓰기
            skillData.skillID = skillID;
            skillData.skillName = columns[1];
            skillData.category = (SkillCategory)Enum.Parse(typeof(SkillCategory), columns[2]);
            skillData.description = columns[3];
            skillData.maxCooldown = int.Parse(columns[4]);
            skillData.minPower = int.Parse(columns[5]);
            skillData.maxPower = int.Parse(columns[6]);
            skillData.targetType = (TargetType)Enum.Parse(typeof(TargetType), columns[7]);

            skillData.skillTendencies.Clear();
            if (!string.IsNullOrEmpty(columns[8]))
            {
                string[] tendencies = columns[8].Split(';');
                foreach (string t in tendencies)
                {
                    skillData.skillTendencies.Add((TendencyType)Enum.Parse(typeof(TendencyType), t));
                }
            }

            skillData.attributeModifiers.Clear();
            if (!string.IsNullOrEmpty(columns[9]))
            {
                string[] modifiers = columns[9].Split(';');
                foreach (string m in modifiers)
                {
                    string[] parts = m.Split(':');
                    AttributeModifier mod = new AttributeModifier
                    {
                        type = (AttributeType)Enum.Parse(typeof(AttributeType), parts[0]),
                        amount = int.Parse(parts[1])
                    };
                    skillData.attributeModifiers.Add(mod);
                }
            }

            // 에셋 저장 및 어드레서블 등록
            if (isNew) AssetDatabase.CreateAsset(skillData, assetPath);
            EditorUtility.SetDirty(skillData);
            RegisterToAddressables(assetPath, skillID, "SkillData");
        }
    }

    // ========================================================================
    // [유닛 데이터 파싱 로직]
    // ========================================================================
    private static void ParseUnitData()
    {
        string filePath = $"{csvFolderPath}/UnitDataTable.csv";
        if (!File.Exists(filePath)) return;

        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] columns = lines[i].Split(',');

            // CSV 컬럼 순서 {ID(0), Name(1), Rank(2), MaxHP(3), MinSpeed(4), MaxSpeed(5), Attributes(6), SkillPool(7)}
            string unitID = columns[0];
            string assetPath = $"{unitDataPath}/{unitID}.asset";

            UnitData unitData = AssetDatabase.LoadAssetAtPath<UnitData>(assetPath);
            bool isNew = false;

            if (unitData == null)
            {
                unitData = ScriptableObject.CreateInstance<UnitData>();
                isNew = true;
            }

            unitData.unitID = unitID;
            unitData.unitName = columns[1];
            unitData.unitRank = int.Parse(columns[2]);
            unitData.maxHP = int.Parse(columns[3]);
            unitData.minSpeed = int.Parse(columns[4]);
            unitData.maxSpeed = int.Parse(columns[5]);

            // 시작 속성 파싱 (YinYang:50 형식)
            unitData.baseAttributes.Clear();
            if (!string.IsNullOrEmpty(columns[6]))
            {
                string[] attrs = columns[6].Split(';');
                foreach (string a in attrs)
                {
                    string[] parts = a.Split(':');
                    UnitAttribute attr = new UnitAttribute
                    {
                        type = (AttributeType)Enum.Parse(typeof(AttributeType), parts[0]),
                        baseValue = int.Parse(parts[1])
                    };
                    unitData.baseAttributes.Add(attr);
                }
            }

            // 스킬 풀 파싱 (SkillID 텍스트를 기반으로 저장된 에셋을 찾아 연결)
            unitData.skillPool.Clear();
            if (!string.IsNullOrEmpty(columns[7]))
            {
                string[] skillIDs = columns[7].Split(';');
                foreach (string sID in skillIDs)
                {
                    string targetSkillPath = $"{skillDataPath}/{sID}.asset";
                    SkillData foundSkill = AssetDatabase.LoadAssetAtPath<SkillData>(targetSkillPath);
                    if (foundSkill != null) unitData.skillPool.Add(foundSkill);
                    else Debug.LogWarning($"[누락 알림] {unitID}의 스킬 풀에 추가하려는 {sID} 에셋을 찾을 수 없습니다.");
                }
            }

            if (isNew) AssetDatabase.CreateAsset(unitData, assetPath);
            EditorUtility.SetDirty(unitData);
            RegisterToAddressables(assetPath, unitID, "UnitData");
        }
    }

    // ========================================================================
    // [유틸리티 기능]
    // ========================================================================

    // 폴더가 없으면 생성하는 안전장치
    /// <summary>
    /// 지정된 경로에 폴더가 없을 경우, 하위 깊은 단계의 폴더까지 안전하게 강제 생성합니다.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        // Assets 폴더 바깥의 실제 운영체제(OS) 절대 경로로 변환하여 물리적 폴더를 생성합니다.
        string absolutePath = Path.Combine(Application.dataPath, path.Substring(7));
        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
        }
    }

    // 생성된 에셋을 Addressable 그룹에 넣고 라벨을 달아주는 자동화 함수
    private static void RegisterToAddressables(string assetPath, string addressName, string label)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return; // 어드레서블 세팅이 안 되어있으면 무시

        AddressableAssetGroup group = settings.DefaultGroup;
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        // 에셋을 그룹에 추가
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

        // 호출하기 편하도록 주소(Address)를 ID로 변경
        entry.SetAddress(addressName);

        // 라벨(Label) 등록 (라벨이 세팅에 없으면 새로 추가함)
        settings.AddLabel(label);
        entry.SetLabel(label, true, true);
    }
}