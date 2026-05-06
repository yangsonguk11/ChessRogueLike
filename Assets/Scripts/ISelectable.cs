using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectable
{
    bool selected { get; set; }
    public abstract bool IsSelectable();
    public void SelectedFalse()
    {
        selected = false;
        ScaleDefault();
    }
    public void SelectedTrue()
    {
        selected = true;
        ScaleHover();
    }
    public IEnumerator ScaleTo(Vector3 target);
    public void ScaleDefault();
    public void ScaleHover();
}
