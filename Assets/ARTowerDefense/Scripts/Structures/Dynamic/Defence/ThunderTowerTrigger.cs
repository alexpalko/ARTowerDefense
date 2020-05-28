using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense
{
    public class ThunderTowerTrigger : MonoBehaviour
    {
        public ThunderTower Tower;
        private HashSet<GameObject> m_CurTargets;

        void Start()
        {
            m_CurTargets = new HashSet<GameObject>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("enemyBug") && !m_CurTargets.Contains(other.gameObject))
            {
                m_CurTargets.Add(other.gameObject);
                Tower.AddTarget(other.gameObject);
            }
        }

        void Update()
        {
            if (m_CurTargets.Any())
            {

                m_CurTargets.RemoveWhere(t =>
                {
                    if (!t.CompareTag("Dead")) return false;
                    Tower.RemoveTarget(t);
                    return true;

                });
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("enemyBug") && m_CurTargets.Contains(other.gameObject))
            {
                m_CurTargets.Remove(other.gameObject);
                Tower.RemoveTarget(other.gameObject);
            }
        }
    }
}