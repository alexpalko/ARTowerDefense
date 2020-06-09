using System.Collections.Generic;
using System.Linq;
using ARTowerDefense.Managers;
using UnityEngine;

namespace ARTowerDefense.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public float MovementSpeed;

        private Animator m_Animator;

        public int Health = 50;
        private Transform[] m_Waypoints;
        private int m_WaypointIndex;
        private Vector3 m_Target;

        public Queue<float> MovementDebuffQueue { get; set; }

        void Start()
        {
            m_Animator = GetComponent<Animator>();

            // From EnemyMovement
            m_Waypoints = Master.PathWayPoints;
            if (m_Waypoints == null)
            {
                Debug.LogError("Enemy spawned before the path was generated. It will be destroyed.");
                Destroy(gameObject);
                return;
            }

            m_Target = m_Waypoints[m_WaypointIndex].position;
            MovementDebuffQueue = new Queue<float>();
        }


        void Update()
        {
            if (Health <= 0)
            {
                if (tag == "DeadEnemy") return;
                m_Animator.SetBool("Death", true);
                CoinManager.AddCoins(Random.Range(0, 4));
                gameObject.tag = "DeadEnemy";
                Destroy(gameObject, 5f);
                return;
            }

            // Enemy movement
            if (m_WaypointIndex >= m_Waypoints.Length)
            {
                Master.EnemyReachedBase = true;
                return;
            }

            Vector3 dir = m_Target - transform.position;
            transform.Translate(
                dir.normalized * (1 - MovementDebuffQueue.FirstOrDefault()) * MovementSpeed *
                Time.deltaTime, Space.World);

            if (Vector3.Distance(transform.position, m_Target) <= .04f)
            {
                _GetNextWaypoint();
                transform.LookAt(m_Target);
            }
        }

        public void DoDamage(int amount)
        {
            Health -= amount;
        }

        private void _GetNextWaypoint()
        {
            m_WaypointIndex++;
            if (m_WaypointIndex < m_Waypoints.Length)
            {
                m_Target = m_Waypoints[m_WaypointIndex].position;
            }
        }
    }
}