using UnityEngine;

public class EventExitButton : MonoBehaviour
{
    public void OnClickLeave()
    {
        AudioManager.instance?.PlayButtonClick();
        GameManager.instance.FinishEventLevel();
    }
}
