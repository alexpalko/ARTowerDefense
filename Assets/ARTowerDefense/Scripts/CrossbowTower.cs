using System.Collections;
using UnityEngine;

public class CrossbowTower : Tower
{
    private Animator m_Animator;

    protected override void Start()
    {
        base.Start();
        m_Animator = LookAtObj.GetComponent<Animator>();
        m_Animator.speed = 2;
    }

    protected override IEnumerator shoot()
    {
        m_IsShooting = true;
        m_Animator.SetTrigger("Shoot");
        yield return new WaitForSeconds(ShootDelay);


        if (Target)
        {
            GameObject b = Instantiate(Ammo, ShootElement.position, Quaternion.identity) as GameObject;
            b.transform.LookAt(Target.transform);
            b.GetComponent<TowerBullet>().target = Target;
            b.GetComponent<TowerBullet>().twr = this;
        }

        m_IsShooting = false;
    }
}
