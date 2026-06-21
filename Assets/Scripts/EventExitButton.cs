using UnityEngine;

public class EventExitButton : MonoBehaviour
{
    public void OnClickLeave()
    {
        GameManager.instance.LeaveEventLevel();
    }
}
