using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public int StartingCoinAmount = 100;

    public GameObject CoinInfo;
    public TextMeshProUGUI CoinAmount;
    public GameObject IncreasePrefab;
    public GameObject DecreasePrefab;

    public static int Coins { get; private set; }

    private static Queue<int> s_AmountChanges;

    void OnEnable()
    {
        Coins = StartingCoinAmount;
        CoinInfo.SetActive(true);
        s_AmountChanges = new Queue<int>();
    }

    void Update()
    {
        CoinAmount.text = Coins.ToString();
        if (s_AmountChanges.Any())
        {
            _DisplayAmountChange();
        }
    }

    public static void AddCoins(int amount)
    {
        Coins += amount;
        s_AmountChanges.Enqueue(amount);
    }

    public static bool RemoveCoins(int amount)
    {
        if (Coins < amount)
        {
            return false;
        }
        Coins -= amount;
        s_AmountChanges.Enqueue(-amount);
        return true;
    }

    private void _DisplayAmountChange()
    {
        int amountChange = s_AmountChanges.Dequeue();
        if (amountChange > 0)
        {
            var increase = Instantiate(IncreasePrefab, CoinAmount.transform);
            increase.GetComponentInChildren<TextMeshProUGUI>().text = "+" + amountChange;
            increase.SetActive(true);
            Destroy(increase, 3);
        }
        else
        {
            var decrease = Instantiate(DecreasePrefab, CoinAmount.transform);
            decrease.GetComponentInChildren<TextMeshProUGUI>().text = amountChange.ToString();
            decrease.SetActive(true);
            Destroy(decrease, 3);
        }
    }

}
