using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 카드 프리펩을 자동 생성하는 에디터 툴.
/// 기존 프리펩(템플릿)을 텍스트 그대로 복제한 뒤, 이름 관련 부분만 새 카드 이름으로 치환한다.
/// 카드 스크립트(Card 컴포넌트)는 동일한 이름의 스크립트가 이미 존재하면 자동으로 연결하고,
/// 아직 스크립트를 작성하지 않았다면 비워서(Missing Script) 생성한다 - 이 경우에만
/// 유니티 에디터에서 해당 컴포넌트의 Script 필드에 직접 스크립트를 드래그해 넣어야 한다.
/// (같은 컴포넌트 슬롯을 유지하므로 EventTrigger 등 다른 참조는 그대로 살아있다)
/// </summary>
public class CardPrefabGenerator : EditorWindow
{
    const string OutputFolder = "Assets/Prefab/Cards";
    const string DatabasePrefabPath = "Assets/Prefab/Database.prefab";
    const string DefaultTemplatePath = "Assets/Prefab/Cards/AttackCard.prefab";

    GameObject template;
    string newCardName = "";
    bool registerToDatabase = true;
    Vector2 scroll;
    string[] missingCardNames = Array.Empty<string>();

    [MenuItem("Tools/Cards/Card Prefab Generator")]
    static void Open()
    {
        var window = GetWindow<CardPrefabGenerator>("Card Prefab Generator");
        window.template = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTemplatePath);
        window.RefreshMissingList();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("템플릿으로 사용할 카드 프리펩", EditorStyles.boldLabel);
        template = (GameObject)EditorGUILayout.ObjectField(template, typeof(GameObject), false);

        registerToDatabase = EditorGUILayout.ToggleLeft(
            $"생성 후 {DatabasePrefabPath} 의 cardPrefabs 목록에 자동 등록",
            registerToDatabase);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("새 카드 이름 (스크립트 클래스 이름과 동일하게)", EditorStyles.boldLabel);
        newCardName = EditorGUILayout.TextField(newCardName);
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(newCardName)))
        {
            if (GUILayout.Button("생성 (스크립트 칸은 비워둠)"))
            {
                GenerateBlank(newCardName);
                RefreshMissingList();
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("프리펩 없는 카드 스크립트 새로고침"))
            RefreshMissingList();

        EditorGUILayout.LabelField($"이미 스크립트는 있지만 프리펩이 없는 카드 ({missingCardNames.Length})", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var name in missingCardNames)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name);
            if (GUILayout.Button("생성", GUILayout.Width(60)))
            {
                GenerateBlank(name);
                RefreshMissingList();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    void RefreshMissingList()
    {
        missingCardNames = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(t => t != null && t.IsClass && !t.IsAbstract && typeof(Card).IsAssignableFrom(t))
            .Select(t => t.Name)
            .Where(name => !File.Exists($"{OutputFolder}/{name}.prefab"))
            .OrderBy(n => n)
            .ToArray();
    }

    static Type[] SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
    }

    void GenerateBlank(string cardName)
    {
        if (template == null)
        {
            EditorUtility.DisplayDialog("카드 프리펩 생성", "템플릿 프리펩을 먼저 지정하세요.", "확인");
            return;
        }

        string newPath = GenerateCardPrefab(cardName, AssetDatabase.GetAssetPath(template));
        if (newPath != null && registerToDatabase)
            RegisterInDatabase(newPath);
    }

    /// <summary>
    /// 템플릿 프리펩을 텍스트 그대로 복제해 <paramref name="cardName"/> 카드 프리펩을 만든다.
    /// 카드 스크립트(Card 컴포넌트)는 Missing Script 상태로 비워둔다.
    /// 성공 시 생성된 프리펩 경로를 반환.
    /// </summary>
    public static string GenerateCardPrefab(string cardName, string templatePath)
    {
        string outputPath = $"{OutputFolder}/{cardName}.prefab";
        if (File.Exists(outputPath))
        {
            Debug.LogWarning($"[CardPrefabGenerator] 이미 존재합니다: {outputPath}");
            return null;
        }

        string templateClassName = Path.GetFileNameWithoutExtension(templatePath);
        string text = File.ReadAllText(templatePath);

        string idMarker = $"Assembly-CSharp::{templateClassName}";
        int idIdx = text.IndexOf(idMarker, StringComparison.Ordinal);
        if (idIdx < 0)
        {
            Debug.LogError($"[CardPrefabGenerator] 템플릿 {templatePath} 에서 {templateClassName} 컴포넌트를 찾지 못했습니다.");
            return null;
        }

        int guidKeyIdx = text.LastIndexOf("guid: ", idIdx, StringComparison.Ordinal);
        int guidLineStart = text.LastIndexOf("m_Script: {fileID: 11500000, guid: ", idIdx, StringComparison.Ordinal);
        if (guidKeyIdx < 0 || guidLineStart < 0)
        {
            Debug.LogError($"[CardPrefabGenerator] 템플릿 {templatePath} 에서 스크립트 참조를 찾지 못했습니다.");
            return null;
        }

        int guidStart = guidKeyIdx + "guid: ".Length;
        int guidEnd = text.IndexOf(',', guidStart);
        string templateGuid = text.Substring(guidStart, guidEnd - guidStart);

        int nameMatches = Regex.Matches(text, $@"(?m)^  m_Name: {Regex.Escape(templateClassName)}$").Count;
        if (nameMatches != 1)
        {
            Debug.LogError($"[CardPrefabGenerator] 템플릿의 루트 오브젝트 이름을 특정할 수 없습니다 ({nameMatches}개 일치).");
            return null;
        }

        // 같은 이름의 카드 스크립트가 이미 존재하면 자동으로 연결하고, 없으면 비워서(Missing Script) 생성
        string scriptGuid = FindScriptGuid(cardName);
        string newScriptLine = scriptGuid != null
            ? $"m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}"
            : "m_Script: {fileID: 0}";

        string newText = text
            .Replace($"m_Script: {{fileID: 11500000, guid: {templateGuid}, type: 3}}", newScriptLine)
            .Replace($"Assembly-CSharp::{templateClassName}", $"Assembly-CSharp::{cardName}")
            .Replace($"{templateClassName}, Assembly-CSharp", $"{cardName}, Assembly-CSharp")
            .Replace($"m_Name: {templateClassName}\n", $"m_Name: {cardName}\n");

        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(outputPath, newText);
        AssetDatabase.ImportAsset(outputPath);
        AssetDatabase.Refresh();

        if (scriptGuid != null)
            Debug.Log($"[CardPrefabGenerator] 생성 완료: {outputPath} (템플릿: {templatePath}) - {cardName} 스크립트를 자동으로 연결했습니다.");
        else
            Debug.LogWarning($"[CardPrefabGenerator] 생성 완료: {outputPath} (템플릿: {templatePath}) - {cardName} 스크립트를 찾지 못해 Card 컴포넌트를 비워뒀습니다. 스크립트 작성 후 직접 드래그해 넣어주세요.");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(outputPath));
        return outputPath;
    }

    /// <summary>cardName과 클래스 이름이 정확히 일치하는 MonoScript를 찾아 GUID를 반환한다. 없으면 null.</summary>
    static string FindScriptGuid(string cardName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {cardName}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass()?.Name == cardName)
                return guid;
        }
        return null;
    }

    /// <summary>새로 만든 카드 프리펩을 Database.prefab의 cardPrefabs 목록에 추가한다.</summary>
    public static void RegisterInDatabase(string newPrefabPath)
    {
        if (!File.Exists(DatabasePrefabPath))
        {
            Debug.LogWarning($"[CardPrefabGenerator] {DatabasePrefabPath} 를 찾을 수 없어 자동 등록을 건너뜁니다.");
            return;
        }

        string newGuid = AssetDatabase.AssetPathToGUID(newPrefabPath);
        if (string.IsNullOrEmpty(newGuid))
        {
            Debug.LogWarning($"[CardPrefabGenerator] {newPrefabPath} 의 guid를 아직 가져올 수 없습니다. Database.prefab 자동 등록을 건너뜁니다.");
            return;
        }

        string dbText = File.ReadAllText(DatabasePrefabPath);
        if (dbText.Contains($"guid: {newGuid}, type: 3}}"))
        {
            Debug.Log($"[CardPrefabGenerator] 이미 Database.prefab에 등록되어 있습니다: {newPrefabPath}");
            return;
        }

        var match = Regex.Match(dbText, @"(?m)^  cardPrefabs:\n(?:  - .*\n)*");
        if (!match.Success)
        {
            Debug.LogWarning($"[CardPrefabGenerator] {DatabasePrefabPath} 에서 cardPrefabs 목록을 찾지 못했습니다.");
            return;
        }

        // 목록 내 첫 항목의 fileID(모든 카드 프리펩 루트가 공유하는 고정 fileID)를 그대로 사용
        var firstEntry = Regex.Match(match.Value, @"fileID: (\d+)");
        if (!firstEntry.Success)
        {
            Debug.LogWarning("[CardPrefabGenerator] cardPrefabs 목록의 fileID 형식을 인식하지 못했습니다.");
            return;
        }

        string newEntry = $"  - {{fileID: {firstEntry.Groups[1].Value}, guid: {newGuid}, type: 3}}\n";
        string newDbText = dbText.Insert(match.Index + match.Length, newEntry);
        File.WriteAllText(DatabasePrefabPath, newDbText);
        AssetDatabase.ImportAsset(DatabasePrefabPath);
        AssetDatabase.Refresh();

        Debug.Log($"[CardPrefabGenerator] Database.prefab의 cardPrefabs에 등록 완료: {Path.GetFileName(newPrefabPath)}");
    }
}
