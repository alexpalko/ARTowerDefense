using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public GameObject CoinInfo;
    public TextMeshProUGUI CoinAmount;

    public static int Coins { get; private set; }

    void OnEnable()
    {
        Coins = 10000;
        CoinInfo.SetActive(true);
    }

    void Update()
    {
        CoinAmount.text = Coins.ToString();
    }

    public static void AddCoins(int amount)
    {
        Coins += amount;
    }

    public static bool RemoveCoins(int amount)
    {
        if (Coins < amount)
        {
            return false;
        }
        Coins -= amount;
        return true;
    }

}
