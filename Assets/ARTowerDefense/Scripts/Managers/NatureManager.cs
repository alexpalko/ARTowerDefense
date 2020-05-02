using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using UnityEngine;
using Random = System.Random;

namespace Assets.ARTowerDefense.Scripts.Managers
{
    class NatureManager : MonoBehaviour
    {
        public float CoverageRate;
        public GameObject[] NaturePrefabs;

        private List<BuildingDivision> m_Divisions;
        
        void OnEnable()
        {
            m_Divisions = Master.DivisionGameObjectDictionary.Values.ToList();

            var unlockedDivisionsCount = m_Divisions.Count(x => !x.IsLocked);
            var divisionsWithNatureCount = Mathf.CeilToInt(unlockedDivisionsCount * CoverageRate);
            
            var rand = new Random();
            var divisionsToAddNature = m_Divisions.OrderBy(x => rand.Next()).Take(divisionsWithNatureCount);

            foreach (var division in divisionsToAddNature)
            {
                division.AddNature(NaturePrefabs[rand.Next(NaturePrefabs.Length)]);
            }
        }
    }
}
