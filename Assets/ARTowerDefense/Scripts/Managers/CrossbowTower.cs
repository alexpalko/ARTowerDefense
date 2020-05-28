using System.Collections;
using ARTowerDefense.Structures.Dynamic.Defense;
using ARTowerDefense.Structures.Dynamic.Defense.Ammo;
using UnityEngine;

namespace ARTowerDefense.Managers
{
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
            yield return new WaitForSeconds(ShootDelay);

            if (Target)
            {
                m_Animator.SetTrigger("Shoot");
                GameObject b = Instantiate(Ammo, ShootElement.position, Quaternion.identity);
                b.transform.LookAt(Target.transform);
                b.GetComponent<CrossbowBolt>().target = Target;
                b.GetComponent<CrossbowBolt>().twr = this;
            }

            m_IsShooting = false;
        }
    }
}