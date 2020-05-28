using UnityEngine;

namespace ARTowerDefense.Managers
{
    public class CoinGenerator : MonoBehaviour
    {
        public int Amount;
        public float TimeInterval;

        private float m_TimeSinceLastGeneration;

        void Start()
        {
            m_TimeSinceLastGeneration = TimeInterval;
        }

        void Update()
        {
            if (m_TimeSinceLastGeneration <= 0)
            {
                CoinManager.AddCoins(Amount);
                m_TimeSinceLastGeneration = TimeInterval;
                return;
            }

            m_TimeSinceLastGeneration -= Time.deltaTime;
        }
    }
}