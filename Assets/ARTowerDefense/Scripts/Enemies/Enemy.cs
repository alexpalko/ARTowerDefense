using UnityEngine;

namespace ARTowerDefense.Enemies
{
    public class Enemy : MonoBehaviour
    {
       
        private Animator m_Animator;
        public EnemyHealth EnemyHealth;


        void Start()
        {
            m_Animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (EnemyHealth.EnemyHP <= 0)
            {
                Destroy(gameObject, 5f);
                m_Animator.SetBool("Death", true);
            }
        }
    }
}