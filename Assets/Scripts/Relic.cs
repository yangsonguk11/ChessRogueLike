using System.Collections.Generic;

public enum RelicTiming { CombatStart, CardUsed, OnHit, TurnStart, TurnEnd, OnKill }

// 유물 기반 클래스. Card.cs와 동일한 패턴 — Assets/Scripts/Relics/ 아래에 유물 하나당 파일 하나로
// 서브클래스를 만들고, 생성자에서 Name/Description/Timing/Effects를 채운다(Card의 Awake()와 같은 역할.
// MonoBehaviour가 아니라 생성자를 쓰는 이유는 Awake()가 Unity 오브젝트 전용 콜백이기 때문).
// MonoBehaviour가 아닌 순수 데이터 클래스라서 화면에 보이는 아이콘 오브젝트와는 완전히 분리돼 있다
// (아이콘은 그냥 그림일 뿐, 유물 기능을 갖지 않는다). RelicDatabase.CreateRelic이 이름(=클래스 이름)으로
// 이 서브클래스를 리플렉션으로 찾아 만들어내고, Board가 전투 동안 들고 있다가 타이밍마다 Effects를 적용한다.
public abstract class Relic
{
    public string Name;
    public string Description;
    public RelicTiming Timing;
    public List<CardEffect> Effects = new List<CardEffect>();

    // CardUsed 전용: false면 카드를 낸 기물(시전자) 위치를 대상으로, true면 그 카드가 실제로 겨냥한
    // 대상 위치를 기준으로 Effects를 적용한다. 다른 타이밍에서는 사용되지 않는다.
    public bool TargetsCardTarget = false;
}
