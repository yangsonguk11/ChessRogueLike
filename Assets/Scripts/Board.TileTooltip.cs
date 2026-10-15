using System.Collections.Generic;

// 보드 칸 호버 정보창 — 칸 상태와 칸 위 기물의 상태를 카드 정보창과 같은 UI로 띄운다(BoardTooltipContent, BoardTooltipHost).
// Button.MouseEnter/MouseExit가 부른다. 보여줄 정보가 없으면(빈 칸) 띄우지 않는다.
public partial class Board
{
    BoardTooltipHost tileTooltip;

    public void ShowTileTooltip(Button button)
    {
        List<TooltipEntry> entries = BoardTooltipContent.Build(button);
        if (entries.Count == 0)
        {
            HideTileTooltip();
            return;
        }
        if (tileTooltip == null)
        {
            ButtonInfo info = BoardUICanvas != null ? BoardUICanvas.GetComponent<ButtonInfo>() : null;
            tileTooltip = BoardTooltipHost.Create(info != null ? info.Font : null);
        }
        tileTooltip.Show(button, entries);
    }

    public void HideTileTooltip()
    {
        if (tileTooltip != null) tileTooltip.Hide();
    }
}
