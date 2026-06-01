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

            // CSV 컬럼 순서 (업데이트): ID(0), Name(1), Category(2), Tendencies(3), TargetType(4), MaxTargetCount(5), 
            // SubTargetOffsets(6), SubTargetDamageRatio(7), MaxCooldown(8), MinDamage(9), MaxDamage(10), MinHeal(11), MaxHeal(12), AttrMods(13), StatusEffects(14), Desc(15)
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

            // Category (인덱스 2로 변경)
            skillData.category = (SkillCategory)Enum.Parse(typeof(SkillCategory), columns[2]);

            // Tendencies (인덱스 3으로 변경)
            skillData.skillTendencies.Clear();
            if (!string.IsNullOrEmpty(columns[3]))
            {
                string[] tendencies = columns[3].Split(';');
                foreach (string t in tendencies)
                {
                    skillData.skillTendencies.Add((TendencyType)Enum.Parse(typeof(TendencyType), t));
                }
            }

            skillData.targetType = (TargetType)Enum.Parse(typeof(TargetType), columns[4]);

            skillData.maxTargetCount = int.Parse(columns[5]);

            // 서브 타겟 오프셋 파싱 (예: -1;+1)
            skillData.subTargetOffsets.Clear();
            if (!string.IsNullOrEmpty(columns[6]))
            {
                string[] offsets = columns[6].Split(';');
                foreach (string o in offsets)
                {
                    skillData.subTargetOffsets.Add(int.Parse(o));
                }
            }

            // 공란(Empty) 예외 처리 추가 - 값이 없으면 0f 처리
            skillData.subTargetDamageRatio = string.IsNullOrEmpty(columns[7]) ? 0f : float.Parse(columns[7]);

            // 위력 분리 및 쿨타임 (인덱스 8 ~ 12)
            skillData.maxCooldown = int.Parse(columns[8]);
            skillData.minDamage = int.Parse(columns[9]);
            skillData.maxDamage = int.Parse(columns[10]);
            skillData.minHeal = int.Parse(columns[11]);
            skillData.maxHeal = int.Parse(columns[12]);

            // 속성 조작 파싱 (인덱스 13으로 변경)
            skillData.attributeModifiers.Clear();
            if (!string.IsNullOrEmpty(columns[13]))
            {
                string[] modifiers = columns[13].Split(';');
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

            // 신규: 상태이상(하이브리드) 파싱 (인덱스 14)
            skillData.statusEffects.Clear();
            if (!string.IsNullOrEmpty(columns[14]))
            {
                string[] effects = columns[14].Split(';');
                foreach (string e in effects)
                {
                    string[] parts = e.Split('_');
                    SkillEffectData effectData = new SkillEffectData
                    {
                        type = (EffectType)Enum.Parse(typeof(EffectType), parts[0]),
                        value = int.Parse(parts[1]),
                        // 3번째 값(Duration)이 있으면 파싱하고, 없으면(스택제 등) 0으로 처리합니다.
                        duration = parts.Length >= 3 ? int.Parse(parts[2]) : 0
                    };
                    skillData.statusEffects.Add(effectData);
                }
            }

            // 설명 파싱 (인덱스 15로 변경)
            skillData.description = columns.Length > 15 ? columns[15] : "";

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

            // CSV 컬럼 순서 (업데이트): UnitID(0), UnitName(1), UnitRank(2), AIBrainType(3), MaxHP(4), 
            // MinSpeed(5), MaxSpeed(6), BaseAttributes(7), CorrisionImmune(8), MoveSkill(9), UltiSkillPool(10), NormalSkillPool(11)
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
            unitData.defaultAIBrainType = (AIBrainType)Enum.Parse(typeof(AIBrainType), columns[3]);
            unitData.maxHP = int.Parse(columns[4]);
            unitData.minSpeed = int.Parse(columns[5]);
            unitData.maxSpeed = int.Parse(columns[6]);

            // 시작 속성 파싱 (YinYang:50 형식)
            unitData.baseAttributes.Clear();
            if (!string.IsNullOrEmpty(columns[7]))
            {
                string[] attrs = columns[7].Split(';');
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

            // 침식 면역 여부 파싱 (8)
            if (bool.TryParse(columns[8], out bool isImmune))
            {
                unitData.isImmuneToCorrosion = isImmune;
            }
            else
            {
                // 파싱 실패 시나 빈칸일 경우 기본값은 면역 아님(false)
                unitData.isImmuneToCorrosion = false;
            }

            // 고정 이동 스킬
            if (!string.IsNullOrEmpty(columns[9]))
            {
                string targetSkillPath = $"{skillDataPath}/{columns[9]}.asset";
                unitData.movementSkill = AssetDatabase.LoadAssetAtPath<SkillData>(targetSkillPath);
            }
            else
            {
                unitData.movementSkill = null;
            }

            // 필살기 풀
            unitData.ultimateSkillPool.Clear();
            if (!string.IsNullOrEmpty(columns[10]))
            {
                string[] skillIDs = columns[10].Split(';');
                foreach (string sID in skillIDs)
                {
                    string targetSkillPath = $"{skillDataPath}/{sID}.asset";
                    SkillData foundSkill = AssetDatabase.LoadAssetAtPath<SkillData>(targetSkillPath);
                    if (foundSkill != null) unitData.ultimateSkillPool.Add(foundSkill);
                }
            }

            // 일반 스킬 풀
            unitData.normalSkillPool.Clear();
            if (!string.IsNullOrEmpty(columns[11]))
            {
                string[] skillIDs = columns[11].Split(';');
                foreach (string sID in skillIDs)
                {
                    string targetSkillPath = $"{skillDataPath}/{sID}.asset";
                    SkillData foundSkill = AssetDatabase.LoadAssetAtPath<SkillData>(targetSkillPath);
                    if (foundSkill != null) unitData.normalSkillPool.Add(foundSkill);
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