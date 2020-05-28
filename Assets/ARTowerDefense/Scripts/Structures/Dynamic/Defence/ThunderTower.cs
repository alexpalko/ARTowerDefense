using System.Collections.Generic;
using ARTowerDefense.Enemies;
using DigitalRuby.LightningBolt;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense
{
    public class ThunderTower : Tower
    {
        private HashSet<GameObject> m_Targets;
        private Dictionary<GameObject, GameObject> m_Bolts;
        public GameObject CenterPoint;

        public float MovementDebuff = .4f;

        protected override void Start()
        {
            base.Start();
            m_Targets = new HashSet<GameObject>();
            m_Bolts = new Dictionary<GameObject, GameObject>();
        }

        void OnDestroy()
        {
            foreach (var bolt in m_Bolts)
            {
                Destroy(bolt.Value);
            }

            foreach (var target in m_Targets)
            {
                target.transform.parent.GetComponent<EnemyMovement>().MovementDebuffQueue.Dequeue();
            }
        }

        protected override void Update()
        {
            foreach (var bolt in m_Bolts)
            {
                if (bolt.Key == null)
                {
                    Destroy(bolt.Value);
                }
            }
        }

        public void AddTarget(GameObject target)
        {
            m_Targets.Add(target);
            var newBolt = Instantiate(Ammo);
            var script = newBolt.GetComponent<LightningBoltScript>();
            script.StartObject = CenterPoint;
            script.EndObject = target;
            m_Bolts.Add(target, newBolt);
            var enemyMovement = target.transform.parent.GetComponent<EnemyMovement>();
            enemyMovement.MovementDebuffQueue.Enqueue(MovementDebuff);
        }

        public void RemoveTarget(GameObject target)
        {
            m_Targets.Remove(target);
            Destroy(m_Bolts[target]);
            m_Bolts.Remove(target);
            var enemyMovement = target.transform.parent.GetComponent<EnemyMovement>();
            enemyMovement.MovementDebuffQueue.Dequeue();
        }
    }
}