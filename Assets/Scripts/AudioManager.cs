using UnityEngine;

// 게임 전반의 SFX 재생 훅. 모든 효과음은 sfxSource.PlayOneShot 하나로 재생된다.
//
// 실제 인스턴스는 GameManager.prefab의 GameManager 오브젝트에 붙어 있다(GameManager와 같은 오브젝트라
// DontDestroyOnLoad·중복 파괴도 함께 따라간다). 클립과 AudioSource(볼륨·믹서 그룹 등)는 이 프리팹의
// Inspector에서 설정한다. 클립 슬롯이 비어 있으면 해당 Play*는 조용히 아무 것도 하지 않는다.
//
// GameManager가 없는 씬에서 instance에 접근하면 빈 GameObject로 폴백한다(클립이 없어 무음).
// RequireComponent 덕분에 이때도 AudioSource가 함께 붙는다.
//
// 각 필드 주석은 "언제 재생되는가"를 설명한다.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    static AudioManager _instance;
    public static AudioManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("AudioManager").AddComponent<AudioManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // 같은 오브젝트의 AudioSource를 프리팹에서 연결해둔다. 볼륨 등 재생 설정은 그 AudioSource에서 조절.
    [SerializeField] AudioSource sfxSource;

    [Header("Card")]
    public AudioClip cardDraw; // 카드를 드로우할 때(턴 시작 드로우 등) — 카드 1장마다가 아니라 드로우 배치당 1회
    public AudioClip cardHover; // 손패의 카드에 마우스를 올릴 때(Card.MouseEnter/ScaleHover와 같은 타이밍)
    public AudioClip cardCommit; // 손패의 카드를 사용 대기 상태로 커밋할 때(CardCanvas.CommitNowUsingCard 시작 시점)
    public AudioClip cardDiscard; // 사용을 마친 카드가 버림 더미로 갈 때(CardCanvas.OnUsedCardAnimationsComplete의 discard 경로)
    public AudioClip cardExile; // 사용을 마친 카드가 소멸(추방)될 때(CardCanvas.OnUsedCardAnimationsComplete의 exile 경로)
    public AudioClip cardTargetConfirm; // 타겟팅 카드를 유효한 위치에 드롭해서 위치가 확정되는 순간(CardCanvas.OnDragCardReleased)

    [Header("Combat")]
    public AudioClip attackImpact; // 공격이 대상에게 적중해 데미지 텍스트가 뜰 때(Piece.DamageText, isAttack=true) — 상태이상 틱 데미지는 제외
    public AudioClip counterAttack; // 가시/반격류로 되받아치는 피해(Piece.DamageText, isCounter=true) — attackImpact 대신 재생
    public AudioClip heal; // 회복이 적용되는 순간(Piece.HealText)
    public AudioClip shieldApply; // 실드가 걸려 pieceEffect 오빗이 켜지는 순간(Piece.ShieldVisualOn, 부여 후 실드량 > 0일 때만)
    public AudioClip buffApply; // 버프성 상태이상이 대상에게 걸리는 순간(Piece.ShowStatusText의 isBuff=true 쪽, textOnly 제외)
    public AudioClip debuffApply; // 디버프성 상태이상이 대상에게 걸리는 순간(Piece.ShowStatusText의 isBuff=false 쪽, textOnly 제외)
    public AudioClip stunSkip; // 기절 상태라 턴을 그냥 흘려보낼 때(Board.StunSkipReaction — 시전자 OnAnimationEvent 시점)
    public AudioClip pieceSummon; // 새 기물이 보드에 소환될 때(Piece.SummonVisualEffect)
    public AudioClip pieceDeath; // 기물 사망 시 "Die" 애니메이션과 같은 시점(Piece.PieceDeathSound — 시전자 OnAnimationEvent에 합류)
    public AudioClip battleVictory; // 전투 종료(승리) — 적이 전부 처치되는 순간(GameManager.RemoveEnemy)
    public AudioClip battleDefeat; // 전투 종료(패배) — 아군이 전부 사망하는 순간(GameManager.TriggerDefeat)

    [Header("Meta")]
    public AudioClip cardAcquired; // 카드가 덱에 영구히 추가될 때(DataManager.AddCardToPieceDeck — 상점 구매/레벨 보상/이벤트 보상이 전부 공유)
    public AudioClip goldAcquired; // 골드를 얻을 때(DataManager.AddGold — 전투 보상 등 모든 골드 획득 경로가 공유)
    public AudioClip shopPurchase; // 상점에서 골드를 써서 거래가 성사될 때(카드·유물 구매, 카드 제거 — ShopCanvas)
    public AudioClip panelOpen; // 모달 선택 패널이 열릴 때(상점/카드 선택 패널/기물 타겟 선택)

    [Header("UI")]
    public AudioClip buttonClick; // 버튼류(턴 종료, 노드 선택 등)를 클릭할 때 — 카드/보드 타일 호버는 제외해 소음 방지
    public AudioClip buttonHover; // 버튼류에 마우스를 올릴 때(현재는 선택 가능한 맵 노드 버튼만) — 카드/타일 호버는 제외
    public AudioClip turnStartPlayer; // 플레이어 턴이 시작될 때(턴 배너와 같은 타이밍)
    public AudioClip turnStartEnemy; // 적 턴이 시작될 때
    public AudioClip invalidAction; // 경고 안내가 뜰 때(AnnouncementUI.Show, isWarning=true — 골드 부족·사용 불가 등)
    public AudioClip enemyTelegraph; // 적의 다음 행동 예고(ShowActionText)가 표시될 때

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // 연결이 빠졌을 때만 같은 오브젝트의 AudioSource를 찾아 쓴다(새로 추가하지 않음).
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
    }

    void Play(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayCardDraw() => Play(cardDraw);
    public void PlayCardHover() => Play(cardHover);
    public void PlayCardCommit() => Play(cardCommit);
    public void PlayCardDiscard() => Play(cardDiscard);
    public void PlayCardExile() => Play(cardExile);
    public void PlayCardTargetConfirm() => Play(cardTargetConfirm);
    public void PlayAttackImpact() => Play(attackImpact);
    public void PlayCounterAttack() => Play(counterAttack);
    public void PlayHeal() => Play(heal);
    public void PlayShieldApply() => Play(shieldApply);
    public void PlayBuffApply() => Play(buffApply);
    public void PlayDebuffApply() => Play(debuffApply);
    public void PlayStunSkip() => Play(stunSkip);
    public void PlayPieceSummon() => Play(pieceSummon);
    public void PlayPieceDeath() => Play(pieceDeath);
    public void PlayBattleVictory() => Play(battleVictory);
    public void PlayBattleDefeat() => Play(battleDefeat);
    public void PlayCardAcquired() => Play(cardAcquired);
    public void PlayGoldAcquired() => Play(goldAcquired);
    public void PlayShopPurchase() => Play(shopPurchase);
    public void PlayPanelOpen() => Play(panelOpen);
    public void PlayButtonClick() => Play(buttonClick);
    public void PlayButtonHover() => Play(buttonHover);
    public void PlayTurnStartPlayer() => Play(turnStartPlayer);
    public void PlayTurnStartEnemy() => Play(turnStartEnemy);
    public void PlayInvalidAction() => Play(invalidAction);
    public void PlayEnemyTelegraph() => Play(enemyTelegraph);
}
