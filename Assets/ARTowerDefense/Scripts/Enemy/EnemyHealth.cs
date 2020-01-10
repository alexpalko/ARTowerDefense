using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int EnemyHP = 50;

    public void DoDamage(int amount)
    {
        EnemyHP -= amount;
    }

    private void Update()
    {

        if (EnemyHP <= 0)
        {
            int coinsGenerated = Random.Range(0, 4);
            if (coinsGenerated > 0 && tag != "Dead")
            {
                CoinManager.AddCoins(coinsGenerated);
            }
            gameObject.tag = "Dead"; 
        }
    }
}
