using UnityEngine;
using System.Collections;

public class TowerTrigger : MonoBehaviour {

	public Tower twr;    
    public bool lockE;
	public GameObject curTarget;
    


    void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("enemyBug") && !lockE)
		{   
			twr.Target = other.gameObject.transform;            
            curTarget = other.gameObject;
			lockE = true;
		}
       
    }
	void Update()
	{
        if (curTarget)
        {
            if (curTarget.CompareTag("Dead")) // get it from EnemyHealth
            {
                lockE = false;
                twr.Target = null;               
            }
        }




        if (!curTarget) 
		{
			lockE = false;            
        }
	}
	void OnTriggerExit(Collider other)
	{
		if(other.CompareTag("enemyBug") && other.gameObject == curTarget)
		{
			lockE = false;
            twr.Target = null;            
        }
	}
	
}
