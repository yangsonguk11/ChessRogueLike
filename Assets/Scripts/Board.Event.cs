public partial class Board
{
    // 휴식 오브젝트의 휴식 버튼에서 호출: 보드 위 모든 아군을 각자의 최대 HP까지만 회복시킨다.
    public void RestHeal()
    {
        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        HealAllAllies(caster);
        if (caster is RestObject restObject) restObject.used = true;

        ClearSelectedButton();

        // 휴식을 사용했으면(회복할 게 없어도) 이벤트에서 할 일이 끝난 것으로 보고 나가기 버튼을 표시
        EventExitButtonObj?.SetActive(true);
    }

    // DialogueUI의 확인 버튼에서 호출: 대화가 끝났으니 선택을 해제하고 나가기 버튼을 표시
    public void OnDialogueClosed()
    {
        ClearSelectedButton();
        EventExitButtonObj?.SetActive(true);
    }
}
