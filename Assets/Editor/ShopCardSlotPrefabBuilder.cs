using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ShopCardSlot 프리팹을 코드로 생성하고, 씬에 있는 ShopCanvas의 cardSlotPrefab 필드에 자동 연결하는 에디터 툴.
// 프리팹은 손으로 만들기 번거로워(YAML 직접 작성은 위험) 코드로 GameObject 계층을 구성한 뒤 저장한다.
// Unity 메뉴: Tools > Shop > Build ShopCardSlot Prefab
public static class ShopCardSlotPrefabBuilder
{
    const string OutputPath = "Assets/Prefab/ShopCardSlot.prefab";

    // CardsPanel에 스폰되는 카드와 같은 크기로 맞춘다.
    // (Cards/AttackCard.prefab 루트 RectTransform 기준 SizeDelta 500x600 * LocalScale 0.5 = 실제 표시 250x300)
    static readonly Vector2 CardSize = new Vector2(250f, 300f);
    static readonly Vector2 PriceSize = new Vector2(250f, 50f);
    const float Spacing = 10f;

    [MenuItem("Tools/Shop/Build ShopCardSlot Prefab")]
    public static void Build()
    {
        GameObject root = new GameObject("ShopCardSlot", typeof(RectTransform));
        RectTransform rootRT = (RectTransform)root.transform;

        VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = Spacing;

        LayoutElement rootLayoutElement = root.AddComponent<LayoutElement>();
        rootLayoutElement.preferredWidth = CardSize.x;
        rootLayoutElement.preferredHeight = CardSize.y + Spacing + PriceSize.y;

        // 카드 부분: 실제 Card가 스폰될 자리 (CardsPanel과 동일한 표시 크기)
        RectTransform cardParent = CreateChild(rootRT, "CardParent", CardSize);

        // 텍스트 부분: 가격 등 자유 텍스트
        RectTransform priceRT = CreateChild(rootRT, "PriceText", PriceSize);
        TextMeshProUGUI priceText = priceRT.gameObject.AddComponent<TextMeshProUGUI>();
        priceText.text = "0";
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.fontSize = 32f;
        priceText.color = Color.white;
        if (priceText.font == null) priceText.font = TMP_Settings.defaultFontAsset;

        ShopCardSlot slot = root.AddComponent<ShopCardSlot>();
        SerializedObject slotSO = new SerializedObject(slot);
        slotSO.FindProperty("cardParent").objectReferenceValue = cardParent;
        slotSO.FindProperty("labelText").objectReferenceValue = priceText;
        slotSO.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory("Assets/Prefab");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[ShopCardSlotPrefabBuilder] 생성 완료: {OutputPath} (카드 영역 {CardSize.x}x{CardSize.y})");

        ConnectToShopCanvas(prefab);
        EditorGUIUtility.PingObject(prefab);
    }

    static RectTransform CreateChild(RectTransform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        return rt;
    }

    static void ConnectToShopCanvas(GameObject prefab)
    {
        ShopCanvas shopCanvas = Object.FindFirstObjectByType<ShopCanvas>(FindObjectsInactive.Include);
        if (shopCanvas == null)
        {
            Debug.LogWarning("[ShopCardSlotPrefabBuilder] 씬에서 ShopCanvas를 찾지 못해 자동 연결하지 못했습니다. cardSlotPrefab 필드에 직접 연결해주세요.");
            return;
        }

        SerializedObject canvasSO = new SerializedObject(shopCanvas);
        canvasSO.FindProperty("cardSlotPrefab").objectReferenceValue = prefab;
        canvasSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shopCanvas);

        Debug.Log($"[ShopCardSlotPrefabBuilder] '{shopCanvas.gameObject.name}'의 ShopCanvas.cardSlotPrefab에 자동 연결했습니다.");
    }
}
