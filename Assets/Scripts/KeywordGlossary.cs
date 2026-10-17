using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// 카드 정보창(CardTooltipView)의 "용어 설명" 창에 쓰는 용어집. 카드 EffectDescription에서 용어를 자동으로 찾아내고,
// 설명 텍스트에 안 나오는 용어는 카드가 Card.ExtraKeywords로 대표 용어(term)를 직접 지정한다.
// 새 용어를 추가할 때는 entries에 한 줄 넣으면 된다 — 매칭은 길이순으로 하므로 등록 순서는 상관없다.
public static class KeywordGlossary
{
    public class Entry
    {
        public readonly string term;       // 정보창 제목으로 쓰는 대표 표기
        public readonly string[] aliases;  // 설명 텍스트에서 함께 찾을 다른 표기
        public readonly string description;

        public Entry(string term, string description, params string[] aliases)
        {
            this.term = term;
            this.description = description;
            this.aliases = aliases;
        }
    }

    static readonly Entry[] entries =
    {
        new Entry("이동공격", "이동으로 적이 있는 칸에 부딪혀 공격합니다. 피해는 시전자의 힘과 같으며, 처치하면 그 칸으로 전진합니다.", "이동 공격"),
        new Entry("이동공격 판정", "제자리에서 공격하지만 이동공격으로 취급합니다. 다음 이동공격 버프와 가시 반격 등이 똑같이 적용되고, 처치해도 전진하지 않습니다."),
        new Entry("힘", "이동공격의 피해량입니다. 공격 카드의 피해에도 더해집니다."),
        new Entry("방어도", "받는 피해를 먼저 대신 받아 줍니다. 턴이 끝나면 사라집니다.", "방어막"),
        new Entry("방어막 보너스", "방어도를 얻는 카드의 방어도에 더해지는 수치입니다."),
        new Entry("마법 피해", "힘의 영향을 받지 않는 피해입니다."),
        new Entry("소멸", "사용한 뒤 버린 카드 더미로 가지 않고 이번 전투에서 제외됩니다."),
        new Entry("무덤", "같은 진영의 기물이 쓰러질 때마다 살아 있는 기물마다 1씩 쌓입니다."),
        new Entry("독", "자기 턴이 끝날 때마다 수치만큼 피해를 받습니다."),
        new Entry("화상", "자기 턴이 끝날 때마다 수치만큼 피해를 받습니다."),
        new Entry("재생", "자기 턴이 끝날 때마다 수치만큼 체력을 회복합니다."),
        new Entry("기절", "행동할 수 없습니다."),
        new Entry("강화", "지속되는 동안 힘이 수치만큼 오릅니다."),
        new Entry("약화", "지속되는 동안 힘이 수치만큼 내려갑니다."),
        new Entry("가시", "이동공격을 받으면 공격자에게 수치만큼 피해를 돌려줍니다."),
        new Entry("취약", "공격과 이동공격으로 받는 피해가 수치만큼 늘어납니다. 독·화상 등 다른 피해에는 적용되지 않습니다."),
        new Entry("도발", "상대는 사거리 안에 도발 상태인 기물이 있으면 그 기물을 먼저 노립니다."),
        new Entry("이동 불가", "이동할 수 없는 상태입니다."),
        new Entry("버프", "기물에게 이로운 상태 효과입니다."),
        new Entry("디버프", "기물에게 해로운 상태 효과입니다."),
        new Entry("드로우", "덱에서 카드를 뽑아 손으로 가져옵니다."),
        new Entry("소환", "빈 칸에 새 기물을 불러냅니다."),
        new Entry("사용 불가", "이 카드는 사용할 수 없습니다."),
        new Entry("소멸 저주", "지속되는 동안 사용하는 카드가 모두 소멸합니다."),
        new Entry("결속", "같은 편 기물이 쓰러질 때마다 체력을 모두 회복하고 힘이 오릅니다."),
        new Entry("상처 입히기", "이 기물에게 공격 피해를 받아 체력이 줄면 버린 카드 더미에 상처가 들어갑니다."),
    };

    static readonly Regex RichTextTag = new Regex("<[^>]*>");

    // (표기, 항목) 쌍을 긴 표기부터 — "방어막 보너스"가 "방어막"보다, "이동공격 판정"이 "이동공격"보다 먼저 매칭되게 한다.
    static readonly (string word, Entry entry)[] wordsLongestFirst = entries
        .SelectMany(e => new[] { e.term }.Concat(e.aliases).Select(w => (w, e)))
        .OrderByDescending(p => p.Item1.Length)
        .ToArray();

    public static Entry Get(string term) => entries.FirstOrDefault(e => e.term == term);

    // text에 등장하는 용어를 첫 등장 위치 순으로 돌려준다(같은 용어는 한 번만). 리치 텍스트 태그는 무시하고,
    // 이미 더 긴 용어로 매칭된 구간은 지워 두어 그 안의 짧은 용어가 다시 잡히지 않게 한다.
    public static List<Entry> FindIn(string text)
    {
        var firstIndex = new Dictionary<Entry, int>();
        if (string.IsNullOrEmpty(text)) return new List<Entry>();

        char[] masked = RichTextTag.Replace(text, "").ToCharArray();
        foreach (var (word, entry) in wordsLongestFirst)
        {
            string current = new string(masked);
            int index = current.IndexOf(word, System.StringComparison.Ordinal);
            while (index >= 0)
            {
                if (!firstIndex.TryGetValue(entry, out int prev) || index < prev)
                    firstIndex[entry] = index;
                for (int i = index; i < index + word.Length; i++)
                    masked[i] = '\0';
                index = current.IndexOf(word, index + word.Length, System.StringComparison.Ordinal);
            }
        }
        return firstIndex.OrderBy(p => p.Value).Select(p => p.Key).ToList();
    }
}
