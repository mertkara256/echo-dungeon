using UnityEngine;

namespace EchoDungeon.Core
{
    public sealed class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        [SerializeField] private int startingCoins;

        public int Coins { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Coins = Mathf.Max(0, startingCoins);
            GameEvents.RaiseCoinsChanged(Coins);
        }

        private void Start()
        {
            GameEvents.RaiseCoinsChanged(Coins);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Coins = Mathf.Max(0, Coins + amount);
            GameEvents.RaiseCoinsChanged(Coins);
        }

        public bool SpendCoins(int amount)
        {
            if (!HasEnoughCoins(amount))
            {
                return false;
            }

            Coins -= Mathf.Max(0, amount);
            GameEvents.RaiseCoinsChanged(Coins);
            return true;
        }

        public bool HasEnoughCoins(int amount)
        {
            return Coins >= Mathf.Max(0, amount);
        }

        public void SetCoins(int amount)
        {
            Coins = Mathf.Max(0, amount);
            GameEvents.RaiseCoinsChanged(Coins);
        }

        public void ResetCoinsToStartingAmount()
        {
            SetCoins(startingCoins);
        }
    }
}
