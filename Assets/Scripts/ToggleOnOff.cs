using System.Collections.Generic;
using UnityEngine;

public class ToggleOnOff : MonoBehaviour
{
    [SerializeField] List<GameObject> objs;

    public void TurnOnOff()
    {
        if (objs[0].activeSelf)
        {
            foreach (GameObject obj in objs)
            {
                obj.SetActive(false);
            }
        }
        else
        {
            foreach (GameObject obj in objs)
            {
                obj.SetActive(true);
            }

        }
    }
}
