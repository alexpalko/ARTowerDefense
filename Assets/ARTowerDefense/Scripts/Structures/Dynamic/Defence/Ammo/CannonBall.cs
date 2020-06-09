using ARTowerDefense.Enemies;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense.Ammo
{
    public class CannonBall : Assets.ARTowerDefense.Scripts.Structures.Dynamic.Defense.Ammo.Ammo
    {
        public GameObject ImpactParticle;

        public Vector3 ImpactNormal;

        protected override void ImpactParticleEffects()
        {
            ImpactParticle = Instantiate(ImpactParticle, transform.position,
                Quaternion.FromToRotation(Vector3.up, ImpactNormal));
            Destroy(ImpactParticle, 3);
        }

        protected override void OtherCollisionEffects()
        {
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies)
            {
                if (enemy.transform == Target) continue;

                if (Vector3.Distance(enemy.transform.position, Target.position) < .2f)
                {
                    enemy.GetComponent<Enemy>().DoDamage(5);
                }
            }
        }
    }
}