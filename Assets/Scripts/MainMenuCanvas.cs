using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class MainMenuCanvas : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    // 세이브를 지우고 startingPieceIndex에 해당하는 기물로 로스터를 구성한 뒤 바로 게임을 시작한다.
    // startingPieceIndex는 DataManager가 해석한다 (0: 기본 기물, 1: 소환사).
    public void StartGameWithPiece(int startingPieceIndex)
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.ResetSaveWithStartingPiece(startingPieceIndex);
        }
        else
        {
            // TitleScene에는 DataManager가 없어 Instance가 아직 null인 경우(앱을 막 켜서 바로 여기서
            // 시작하는 일반적인 경우). 세이브만 지우고 선택한 인덱스는 PlayerPrefs에 남겨서,
            // MainScene에서 DataManager가 처음 생성될 때 LoadFromFile()이 읽어 적용하게 한다.
            string path = Path.Combine(Application.persistentDataPath, "save.json");
            if (File.Exists(path))
                File.Delete(path);
            PlayerPrefs.SetInt(DataManager.PendingStartingPieceIndexPrefKey, startingPieceIndex);
            PlayerPrefs.Save();
        }
        StartGame();
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
