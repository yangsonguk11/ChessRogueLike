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

    // 휴식 오브젝트의 강화 버튼에서 호출: DialogueUI의 영구 스탯 강화 선택지와 동일한 방식
    // (RequestPieceSelection으로 아군을 고른 뒤 ApplyPermanentStatBuff)으로 처리한다.
    public void RestUpgrade()
    {
        Piece caster = GetButtonScript(selectedButton).GetPieceScript();
        if (!(caster is RestObject restObject)) return;

        ClearSelectedButton();

        RequestPieceSelection(
            restObject.upgradeSelectCount,
            selected =>
            {
                ApplyPermanentStatBuff(selected, restObject.upgradeColDamageBonus, restObject.upgradeShieldBonusBonus);
                restObject.used = true;
                EventExitButtonObj?.SetActive(true);
            },
            PieceSelectFilters.Team(0));
    }

    // GameManager.FinishLevel에서 호출: currentLevelData.rewardType에 따라 카드 보상(기존 방식),
    // 기물 영구 강화 보상(RestUpgrade/DialogueUI와 동일한 RequestPieceSelection 방식), 또는 보상 없음을 처리한다.
    public void GrantLevelReward()
    {
        if (currentLevelData != null && currentLevelData.rewardType == LevelData.RewardType.PieceUpgrade)
        {
            RequestPieceSelection(
                currentLevelData.rewardUpgradeSelectCount,
                selected =>
                {
                    ApplyPermanentStatBuff(selected, currentLevelData.rewardUpgradeColDamageBonus, currentLevelData.rewardUpgradeShieldBonusBonus);
                    MapCanvas.instance.Show();
                },
                PieceSelectFilters.Team(0));
            return;
        }

        if (currentLevelData != null && currentLevelData.rewardType == LevelData.RewardType.None)
        {
            MapCanvas.instance.Show();
            return;
        }

        if (ResultCanvas.Instance != null)
            ResultCanvas.Instance.EnableCanvas();
        else
            UnityEngine.Debug.LogError("[Board] GrantLevelReward: ResultCanvas.Instance가 null입니다.");
    }

    // DialogueUI의 확인 버튼에서 호출: 대화가 끝났으니 선택을 해제하고 나가기 버튼을 표시
    public void OnDialogueClosed()
    {
        ClearSelectedButton();
        EventExitButtonObj?.SetActive(true);
    }
}
