using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ARTowerDefense.Structures.Dynamic.Defense;

namespace ARTowerDefense.Structures.Dynamic.Defence
{
    public class TowerTrigger : MonoBehaviour {

        public Tower Tower;    
        public bool LockEnemy;
        public GameObject CurrentTarget;
        private List<GameObject> m_PotentialTargets;

        void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Enemy"))
            {
                if (!LockEnemy)
                {
                    Tower.Target = other.gameObject.transform;
                    CurrentTarget = other.gameObject;
                    LockEnemy = true;
                }
                else
                {
                    m_PotentialTargets.Add(other.gameObject);
                }
            }
       
        }

        void Start()
        {
            m_PotentialTargets = new List<GameObject>(); 
        }

        void Update()
        {
            if (CurrentTarget)
            {
                if (CurrentTarget.CompareTag("DeadEnemy"))
                {
                    if (m_PotentialTargets.Any())
                    {
                        CurrentTarget = m_PotentialTargets[0];
                        m_PotentialTargets.RemoveAt(0);
                        Tower.Target = CurrentTarget.transform;
                    }
                    else
                    {
                        LockEnemy = false;
                        Tower.Target = null;
                    }
                }
            }

            if (!CurrentTarget) 
            {
                LockEnemy = false;            
            }
        }
        void OnTriggerExit(Collider other)
        {
            if(other.CompareTag("Enemy"))
            {
                if (other.gameObject == CurrentTarget)
                {
                    if (m_PotentialTargets.Any())
                    {
                        CurrentTarget = m_PotentialTargets[0];
                        m_PotentialTargets.RemoveAt(0);
                        Tower.Target = CurrentTarget.transform;
                    }
                    else
                    {
                        LockEnemy = false;
                        Tower.Target = null;
                    }
                }
                else
                {
                    m_PotentialTargets.Remove(other.gameObject);
                }
            }
        }
	
    }
}