using System.Collections.Generic;
using DigitalRuby.LightningBolt;
using UnityEngine;

public class ThunderTower : Tower
{
    private HashSet<GameObject> m_Targets;
    private Dictionary<GameObject, GameObject> m_Bolts;
    public GameObject CenterPoint;

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
    }

    protected override void Update()
    {
        //base.Update();
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
    }

    public void RemoveTarget(GameObject target)
    {
        m_Targets.Remove(target);
        Destroy(m_Bolts[target]);
        m_Bolts.Remove(target);
    }

}
