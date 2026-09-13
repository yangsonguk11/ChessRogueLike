using UnityEngine;

// 게임 전반의 SFX 재생 훅. 지금은 실제 오디오 에셋이 없어 모든 클립 슬롯이 비어있을 수 있으며,
// 그 경우 각 Play*는 조용히 아무 것도 하지 않는다(널 체크로 안전).
//
// 나중에 효과음을 추가하려면: Unity Editor에서 이 컴포넌트를 붙인 GameObject를 만들고
// Inspector에서 클립들을 채운 뒤 "Assets/Resources/AudioManager.prefab"으로 저장해두면,
// 코드 변경 없이 최초 접근 시 그 프리팹이 자동으로 인스턴스화되어 쓰인다.
// 프리팹이 없으면 빈 GameObject로 폴백해 항상 안전하게 동작한다.
//
// 현재 실제로 호출부가 연결된 건 pieceDeath(Piece.DeathCor)뿐이고, 나머지는 전부 향후 연결을
// 위해 미리 만들어둔 자리(플랜 Phase 1/2/3의 "SFX 연결" 항목 참고). 각 필드 주석은 "언제 재생돼야
// 하는가"를 설명한다.
public class AudioManager : MonoBehaviour
{
    static AudioManager _instance;
    public static AudioManager instance
    {
        get
        {
            if (_instance == null)
            {
                AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
                _instance = prefab != null ? Instantiate(prefab) : new GameObject("AudioManager").AddComponent<AudioManager>();
                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    [SerializeField] AudioSource sfxSource;

    [Header("Card")]
    public AudioClip cardDraw; // 카드를 드로우할 때(턴 시작 드로우 등) — 카드 1장마다가 아니라 드로우 배치당 1회
    public AudioClip cardHover; // 손패의 카드에 마우스를 올릴 때(Card.MouseEnter/ScaleHover와 같은 타이밍)
    public AudioClip cardCommit; // 손패의 카드를 커밋(사용 확정)해서 NowUsing 위치로 이동을 마쳤을 때
    public AudioClip cardDiscard; // 사용을 마친 카드가 버림 더미로 갈 때(FinishUseCard의 discard 경로)
    public AudioClip cardExile; // 사용을 마친 카드가 소멸(추방)될 때(FinishUseCard의 exile 경로)
    public AudioClip cardTargetConfirm; // 타겟팅 카드를 유효한 위치에 드롭해서 위치가 확정되는 순간(CardCanvas.OnDragCardReleased)

    [Header("Combat")]
    public AudioClip attackImpact; // 공격이 대상에게 실제로 적중하는 순간 — 시전자 애니메이션의 타격 프레임(OnAnimationEvent)에 맞춰 재생 예정
    public AudioClip counterAttack; // 가시/반격류 상태이상으로 되받아치는 피해 — 일반 attackImpact와 구분되는 소리
    public AudioClip heal; // 회복이 적용되는 순간(HealText가 뜨는 것과 같은 타이밍)
    public AudioClip shieldApply; // 실드가 새로 걸리는(pieceEffect 오빗이 켜지는) 순간
    public AudioClip buffApply; // 버프성 상태이상이 대상에게 걸리는 순간(ShowStatusText의 isBuff=true 쪽)
    public AudioClip debuffApply; // 디버프성 상태이상이 대상에게 걸리는 순간(ShowStatusText의 isBuff=false 쪽)
    public AudioClip stunSkip; // 기절 상태라 턴을 그냥 흘려보낼 때(StunnedCard 처리부)
    public AudioClip pieceSummon; // 새 기물이 보드에 소환될 때
    public AudioClip pieceDeath; // 기물이 사망해 DeathCor가 시작될 때 — 현재 유일하게 실제로 연결된 훅
    public AudioClip battleVictory; // 전투 종료(승리) — 적이 전부 처치되는 순간(GameManager.RemoveEnemy)
    public AudioClip battleDefeat; // 전투 종료(패배) — 아군이 전부 사망하는 순간(GameManager.TriggerDefeat)

    [Header("Meta")]
    public AudioClip cardAcquired; // 카드가 덱에 영구히 추가될 때(상점 구매/레벨 보상/이벤트 보상이 전부 공유)
    public AudioClip goldAcquired; // 골드를 얻을 때(DataManager.AddGold — 전투 보상 등 모든 골드 획득 경로가 공유)
    public AudioClip shopPurchase; // 상점에서 유물을 구매할 때(ShopCanvas.BuyRelic)
    public AudioClip panelOpen; // 모달 선택 패널이 열릴 때(상점/카드 선택 패널/기물 타겟 선택)

    [Header("UI")]
    public AudioClip buttonClick; // 버튼류(턴 종료, 노드 선택 등)를 클릭할 때 — 카드/보드 타일 호버는 제외해 소음 방지
    public AudioClip buttonHover; // 버튼류에 마우스를 올릴 때 — 마찬가지로 버튼에만, 카드/타일 호버는 제외
    public AudioClip turnStartPlayer; // 플레이어 턴이 시작될 때(턴 배너와 같은 타이밍)
    public AudioClip turnStartEnemy; // 적 턴이 시작될 때
    public AudioClip invalidAction; // 잘못된 조작(유효하지 않은 카드 드롭/타겟 선택 등)에 대한 경고 피드백
    public AudioClip enemyTelegraph; // 적의 다음 행동 예고(ShowActionText)가 표시될 때

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
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
