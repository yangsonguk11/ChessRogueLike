using System.Collections.Generic;
using UnityEngine;

public class ResultCanvas : MonoBehaviour
{
    public static ResultCanvas Instance;

    [Tooltip("RewardSlot이 부착된 프리팹. 보상 그리드에 스폰된다.")]
    [SerializeField] GameObject rewardSlotPrefab;

    [Tooltip("rewardSlotPrefab이 스폰될 부모(RewardGrid). Grid Layout Group이 붙어 있어 스폰만 하면 자동 정렬된다. " +
        "SlideInFromBottom도 이 오브젝트에 붙어 있어서, 이 오브젝트 자체를 SetActive(true/false)하면 " +
        "그리드 표시/숨김과 슬라이드인 애니메이션이 함께 처리된다(별도 래퍼 패널 없음).")]
    [SerializeField] RectTransform rewardGridContainer;

    [Tooltip("기물 카드 보상 버튼 클릭 시 카드 3장을 보여주는 선택 패널의 루트. 기본 비활성. " +
        "rewardGridContainer와 별개로 독립적으로 SetActive된다.")]
    [SerializeField] GameObject cardChoicePanelRoot;

    [Tooltip("카드 3장이 스폰될 곳(cardChoicePanelRoot의 자식).")]
    [SerializeField] RectTransform cardChoiceContainer;

    [Tooltip("나가기 버튼, 배경 이미지 등 RewardGrid와 같이 켜고 꺼야 하지만 RewardGrid의 자식으로 두면 " +
        "GridLayoutGroup이 셀로 배치해버리는 오브젝트들. RewardGrid와 형제로 두고 여기서 같이 토글한다.")]
    [SerializeField] GameObject[] rewardGridExtras;

    readonly List<GameObject> spawnedRewardSlots = new List<GameObject>();
    readonly List<GameObject> spawnedChoiceCards = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        SetRewardGridActive(false);
        cardChoicePanelRoot?.SetActive(false);
    }

    // 시작 덱 카드(AttackCard/DefenseCard/MoveCard/SummonCard)는 이미 누구나 갖고 있으므로 보상 후보에서 제외한다.
    static readonly string[] ExcludedFromRewards = { "AttackCard", "DefenseCard", "MoveCard", "SummonCard" };

    public void EnableCanvas(int goldMin, int goldMax)
    {
        BuildRewardGrid(goldMin, goldMax);
        SetRewardGridActive(true); // RewardGrid에 붙은 SlideInFromBottom.OnEnable()이 슬라이드인을 자동 재생한다.
    }

    // RewardGrid 자신과, GridLayoutGroup에 셀로 배치되면 안 돼서 형제로 둔 부속 오브젝트(나가기 버튼, 배경 등)를 함께 토글한다.
    void SetRewardGridActive(bool active)
    {
        rewardGridContainer.gameObject.SetActive(active);
        if (rewardGridExtras == null) return;
        foreach (var obj in rewardGridExtras)
            if (obj != null) obj.SetActive(active);
    }

    void BuildRewardGrid(int goldMin, int goldMax)
    {
        ClearRewardSlots();

        int goldAmount = Random.Range(goldMin, goldMax + 1);
        if (goldAmount > 0)
        {
            GameObject slotObj = Instantiate(rewardSlotPrefab, rewardGridContainer);
            RewardSlot slot = slotObj.GetComponent<RewardSlot>();
            if (slot != null)
            {
                slot.SetLabel($"골드 +{goldAmount}");
                slot.SetOnClick(() =>
                {
                    DataManager.Instance.AddGold(goldAmount);
                    spawnedRewardSlots.Remove(slotObj);
                    Destroy(slotObj);
                });
            }
            spawnedRewardSlots.Add(slotObj);
        }

        IReadOnlyList<PieceData> pieces = DataManager.Instance.Pieces;
        for (int i = 0; i < pieces.Count; i++)
        {
            int pieceIndex = i; // 클로저 캡처용 로컬 복사
            GameObject slotObj = Instantiate(rewardSlotPrefab, rewardGridContainer);
            RewardSlot slot = slotObj.GetComponent<RewardSlot>();
            if (slot != null)
            {
                slot.SetLabel($"{pieces[i].pieceName} 카드");
                slot.SetOnClick(() => OpenCardChoiceFor(pieceIndex, slotObj));
            }
            spawnedRewardSlots.Add(slotObj);
        }
    }

    // pool에 카드 3장을 스폰하고, 카드를 클릭하면 onCardPicked(cardName)을 호출한다.
    void SpawnCardChoices(IEnumerable<string> pool, System.Action<string> onCardPicked)
    {
        ICardDatabase cardDb = CardDatabase.instance;
        List<string> picks = cardDb.PickRandomDistinctFrom(3, pool, ExcludedFromRewards);

        foreach (string cardName in picks)
        {
            GameObject spawned = cardDb.SpawnCard(cardChoiceContainer, cardName);
            if (spawned == null) continue;

            Card card = spawned.GetComponent<Card>();
            if (card == null) { Destroy(spawned); continue; }

            card.onClickOverride = onCardPicked;
            spawnedChoiceCards.Add(spawned);
        }
    }

    // pieceIndex 기물의 직업 전용 보상 카드 풀을 찾는다. PieceInfo/Job을 못 찾거나 풀이
    // 비어있으면 전역 카드 풀로 폴백한다(직업 설정이 안 된 기물도 보상이 안 뜨는 일은 없게).
    IEnumerable<string> ResolveRewardPoolFor(int pieceIndex)
    {
        string pieceName = DataManager.Instance.Pieces[pieceIndex].pieceName;
        PieceInfo info = PieceDatabase.instance != null ? PieceDatabase.instance.GetPieceInfo(pieceName) : null;
        List<string> jobPool = info?.Job != null ? info.Job.RewardCardPool : null;
        return jobPool != null && jobPool.Count > 0 ? jobPool : CardDatabase.instance.GetAllCardNames();
    }

    // 기물 카드 보상 버튼 클릭 시 호출: 그 기물 직업의 카드 풀에서 3장을 보여주고, 고른 카드를
    // (기물 선택 없이) 바로 pieceIndex의 덱에 추가한 뒤 그 보상 버튼을 그리드에서 제거한다.
    void OpenCardChoiceFor(int pieceIndex, GameObject originSlotObj)
    {
        ClearChoiceCards();
        SpawnCardChoices(ResolveRewardPoolFor(pieceIndex), cardName =>
        {
            DataManager.Instance.AddCardToPieceDeck(pieceIndex, cardName);
            spawnedRewardSlots.Remove(originSlotObj);
            Destroy(originSlotObj);
            CloseCardChoice();
        });
        cardChoicePanelRoot?.SetActive(true);
    }

    void CloseCardChoice()
    {
        cardChoicePanelRoot?.SetActive(false);
        ClearChoiceCards();
    }

    // 나가기 버튼의 OnClick에 연결. 보상을 하나도 안 받았어도 항상 호출 가능하다.
    // 지금은 별도 퇴장 연출 없이 즉시 RewardGrid를 끈다(SlideInFromBottom은 등장 연출만 처리).
    public void OnClickExit()
    {
        AudioManager.instance?.PlayButtonClick();
        CloseCardChoice();
        ClearRewardSlots();
        SetRewardGridActive(false);
        MapCanvas.instance.Show();
    }

    void ClearRewardSlots()
    {
        foreach (var obj in spawnedRewardSlots)
            if (obj != null) Destroy(obj);
        spawnedRewardSlots.Clear();
    }

    void ClearChoiceCards()
    {
        foreach (var obj in spawnedChoiceCards)
            if (obj != null) Destroy(obj);
        spawnedChoiceCards.Clear();
    }
}
