using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ThunderTowerTrigger : MonoBehaviour
{
    public ThunderTower twr;
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
            twr.AddTarget(other.gameObject);
        }
    }

    void Update()
    {
        if (m_CurTargets.Any())
        {

            m_CurTargets.RemoveWhere(t =>
            {
                if (!t.CompareTag("Dead")) return false;
                twr.RemoveTarget(t);
                return true;

            });
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("enemyBug") && m_CurTargets.Contains(other.gameObject))
        {
            m_CurTargets.Remove(other.gameObject);
            twr.RemoveTarget(other.gameObject);
        }
    }
}
