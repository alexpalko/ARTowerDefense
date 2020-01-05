using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int EnemyHP = 50;

    public void DoDamage(int amount)
    {
        EnemyHP -= amount;
    }

    private void Update()
    {

        if (EnemyHP <= 0)
        {
            gameObject.tag = "Dead"; // send it to TowerTrigger to stop the shooting

        }
    }

}
