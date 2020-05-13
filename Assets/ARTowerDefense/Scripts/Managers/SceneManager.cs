using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public void SwapScene(int sceneId)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneId);
    }

    public void SetTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
    }
}
