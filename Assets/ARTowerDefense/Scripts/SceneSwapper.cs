using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwapper : MonoBehaviour
{
    public void SwapScene(int sceneId)
    {
        SceneManager.LoadScene(sceneId);
    }
}
