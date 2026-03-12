using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] GameObject Board;
    [SerializeField] GameObject CardCanvas;
    private void Start()
    {
        if (instance == null)
            instance = this;

    }
}
