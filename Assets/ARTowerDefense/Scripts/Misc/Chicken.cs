using UnityEngine;

public class Chicken : MonoBehaviour
{
    private Animator m_Animator;
    private float m_Cooldown;
    private const float k_CooldownDefault = 5;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (m_Cooldown >= 0)
        {
            m_Cooldown -= Time.deltaTime;
            return;
        }
        var chance = Random.value;
        if (chance < .15f)
        {
            m_Animator.SetBool("Eat", !m_Animator.GetBool("Eat"));
            m_Cooldown = k_CooldownDefault;
        }
        else if (chance < .3f)
        {
            m_Animator.SetBool("Turn Head", !m_Animator.GetBool("Turn Head"));
            m_Cooldown = k_CooldownDefault;
        }
    }
}
