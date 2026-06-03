using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class MainMenuCanvas : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void ResetSave()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.DeleteSaveFile();
        }
        else
        {
            string path = Path.Combine(Application.persistentDataPath, "save.json");
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
