using UnityEngine;

namespace Assets.ARTowerDefense.Scripts
{
    public class Division
    {
        public Vector3 Point1 { get; }
        public Vector3 Point2 { get; }
        public Vector3 Center { get; }

        public Division(Vector3 point1, Vector3 point2)
        {
            Point1 = point1;
            Point2 = point2;
            Center = Vector3.Lerp(point1, point2, 0.5f);
        }

        public bool Includes(Vector3 point)
        {
            return point.x >= Point1.x && point.x <= Point2.x && 
                   point.z >= Point1.z && point.z <= Point2.z;
        }

        public static bool operator ==(Division division1, Division division2)
        {
            if (ReferenceEquals(division1, null))
            {
                if (ReferenceEquals(division2, null))
                {
                    return true;
                }
                
                return false;
            }

            // Return true if the fields match:
            return division1.Equals(division2);
        }

        public static bool operator !=(Division division1, Division division2)
        {
            return !(division1 == division2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Division division))
            {
                return false;
            }

            return division.Point1 == Point1 && division.Point2 == Point2;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int hashingBase = (int)2166136261;
                const int hashingMultiplier = 16777619;

                int hash = hashingBase;
                hash = (hash * hashingMultiplier) ^ Point1.GetHashCode();
                hash = (hash * hashingMultiplier) ^ Point2.GetHashCode();
                return hash;
            }
        }
    }
}
