using ARTowerDefense.Enemies;
using ARTowerDefense.Structures.Dynamic.Defense;
using UnityEngine;

namespace Assets.ARTowerDefense.Scripts.Structures.Dynamic.Defense.Ammo
{
    public abstract class Ammo : MonoBehaviour
    {
        public float Speed;
        public Transform Target;
        Vector3 LastTargetPosition;
        public Tower Tower;

        void Update()
        {
            if (Target)
            {
                transform.LookAt(Target);
                transform.position = Vector3.MoveTowards(transform.position, Target.position, Time.deltaTime * Speed);
                LastTargetPosition = Target.transform.position;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, LastTargetPosition, Time.deltaTime * Speed);
                if (transform.position == LastTargetPosition)
                {
                    Destroy(gameObject, .05f);

                    ImpactParticleEffects();
                }
            }
        }


        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.transform == Target)
            {
                Target.GetComponent<Enemy>().DoDamage(Tower.Damage);
                Destroy(gameObject, .05f);
                ImpactParticleEffects();
            }

            OtherCollisionEffects();
        }

        protected virtual void ImpactParticleEffects() { }

        protected virtual void OtherCollisionEffects() { }

    }
}
