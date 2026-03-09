using UnityEngine;

public interface ISelectable
{
    bool selected { get; set; }
    public abstract bool IsSelectable();
    public void SelectedFalse()
    {
        selected = false;
        //ScaleDefault();
    }
    public void SelectedTrue()
    {
        selected = true;
        //ScaleHover();
    }
}
