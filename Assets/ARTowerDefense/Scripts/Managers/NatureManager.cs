using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace ARTowerDefense.Managers
{
    class NatureManager : MonoBehaviour
    {
        public float CoverageRate;
        public GameObject[] NaturePrefabs;
        
        void OnEnable()
        {
            List<BuildingDivision> divisions = Master.DivisionGameObjectDictionary.Values.Where(x => !x.IsLocked).ToList();

            var unlockedDivisionsCount = divisions.Count;
            var divisionsWithNatureCount = Mathf.CeilToInt(unlockedDivisionsCount * CoverageRate);
            
            var rand = new Random();
            var divisionsToAddNature = divisions.OrderBy(x => rand.Next()).Take(divisionsWithNatureCount);

            foreach (var division in divisionsToAddNature)
            {
                division.AddNature(NaturePrefabs[rand.Next(NaturePrefabs.Length)]);
            }
        }
    }
}
