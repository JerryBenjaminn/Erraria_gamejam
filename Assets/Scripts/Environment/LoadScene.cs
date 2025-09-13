using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public void LoadFirstLevel()
    {
        SceneManager.LoadScene("Level01");
        SoundManager.PlayLevelMusic();
    }
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit game");
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
