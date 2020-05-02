using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private static bool m_IsPaused;

    public void Pause()
    {
        Time.timeScale = m_IsPaused ? 1 : 0;
        m_IsPaused = !m_IsPaused;
    }
}
