using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

public class MainMenuSceneCreator
{
    [MenuItem("Tools/Create Main Menu Scene")]
    static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/NanumGothicExtraBold SDF.asset");
        if (fontAsset == null)
            Debug.LogWarning("[MainMenuSceneCreator] 폰트를 찾을 수 없습니다: Assets/NanumGothicExtraBold SDF.asset");

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var menuCanvas = canvasGO.AddComponent<MainMenuCanvas>();

        // 배경 (검정)
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // 타이틀 텍스트
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "ChessRogueLike";
        titleText.fontSize = 80;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        if (fontAsset != null) titleText.font = fontAsset;

        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.65f);
        titleRect.anchorMax = new Vector2(0.5f, 0.65f);
        titleRect.sizeDelta = new Vector2(900, 130);
        titleRect.anchoredPosition = Vector2.zero;

        // 버튼 컨테이너
        var containerGO = new GameObject("ButtonContainer");
        containerGO.transform.SetParent(canvasGO.transform, false);
        var containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.35f);
        containerRect.anchorMax = new Vector2(0.5f, 0.35f);
        containerRect.sizeDelta = new Vector2(320, 300);
        containerRect.anchoredPosition = Vector2.zero;

        var vlg = containerGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        AddButton(containerGO.transform, "StartButton", "Start", menuCanvas, fontAsset, nameof(MainMenuCanvas.StartGame));
        AddButton(containerGO.transform, "ResetButton", "Reset", menuCanvas, fontAsset, nameof(MainMenuCanvas.ResetSave));
        AddButton(containerGO.transform, "ExitButton", "Exit", menuCanvas, fontAsset, nameof(MainMenuCanvas.ExitGame));

        // 씬 저장
        string scenePath = "Assets/Scenes/TitleScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        // Build Settings에 추가
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool alreadyExists = buildScenes.Exists(s => s.path == scenePath);
        if (!alreadyExists)
        {
            buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        AssetDatabase.Refresh();
        Debug.Log("[MainMenuSceneCreator] TitleScene 생성 완료: " + scenePath);
    }

    static void AddButton(Transform parent, string goName, string label, MainMenuCanvas menuCanvas, TMP_FontAsset fontAsset, string methodName)
    {
        var buttonGO = new GameObject(goName);
        buttonGO.transform.SetParent(parent, false);

        var rect = buttonGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 80);

        var image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.25f, 1f);

        var button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.18f, 0.25f, 1f);
        colors.highlightedColor = new Color(0.28f, 0.28f, 0.40f, 1f);
        colors.pressedColor = new Color(0.10f, 0.10f, 0.16f, 1f);
        button.colors = colors;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 38;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        if (fontAsset != null) text.font = fontAsset;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var method = menuCanvas.GetType().GetMethod(methodName);
        if (method != null)
        {
            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), menuCanvas, method);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
        }
    }
}
