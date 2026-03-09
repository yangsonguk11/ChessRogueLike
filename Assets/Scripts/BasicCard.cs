using UnityEngine;

public class BasicCard : Card
{

    public override bool CanUse()
    {
        throw new System.NotImplementedException();
    }

    public override void Execute()
    {
        throw new System.NotImplementedException();
    }
    public void MouseEnter()
    {
        ScaleHover();
    }

    public void MouseExit()
    {
        if (!selected) ScaleDefault();
    }
    public void MouseDown()
    {
        if (!selected) SelectedTrue();
        else if (selected) SelectedFalse();
    }
}
