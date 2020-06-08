using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ARTowerDefense.Structures.Dynamic.Defense;

public class TowerTrigger : MonoBehaviour {

	public Tower twr;    
    public bool lockE;
	public GameObject curTarget;
    private List<GameObject> m_PotentialTargets;
    


    void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("EnemyHealth"))
		{
            if (!lockE)
            {
                twr.Target = other.gameObject.transform;
                curTarget = other.gameObject;
                lockE = true;
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
        if (curTarget)
        {
            if (curTarget.CompareTag("Dead"))
            {
                if (m_PotentialTargets.Any())
                {
                    curTarget = m_PotentialTargets[0];
                    m_PotentialTargets.RemoveAt(0);
                    twr.Target = curTarget.transform;
                }
                else
                {
                    lockE = false;
                    twr.Target = null;
                }
            }
        }

        if (!curTarget) 
		{
			lockE = false;            
        }
	}
	void OnTriggerExit(Collider other)
	{
		if(other.CompareTag("EnemyHealth"))
		{
            if (other.gameObject == curTarget)
            {
                if (m_PotentialTargets.Any())
                {
                    curTarget = m_PotentialTargets[0];
                    m_PotentialTargets.RemoveAt(0);
                    twr.Target = curTarget.transform;
                }
                else
                {
                    lockE = false;
                    twr.Target = null;
                }
            }
            else
            {
                m_PotentialTargets.Remove(other.gameObject);
            }
        }
	}
	
}
