using UnityEngine;

namespace SniperGame.Player
{
    public enum HitboxType
    {
        Head,
        Body,
        Legs
    }

    public class PlayerHitbox : MonoBehaviour
    {
        [Header("Hitbox Configuration")]
        [SerializeField] private HitboxType hitboxType = HitboxType.Body;
        [SerializeField] private int customDamage = 0; // Optionele override (0 = standaard berekening)

        private PlayerHealth _playerHealth;

        private void Awake()
        {
            _playerHealth = GetComponentInParent<PlayerHealth>();
        }

        public HitboxType Type => hitboxType;
        public PlayerHealth Health => _playerHealth;

        public int GetDamage()
        {
            if (customDamage > 0) return customDamage;

            switch (hitboxType)
            {
                case HitboxType.Head:
                    return 150; // Direct dodelijk
                case HitboxType.Body:
                    return 100; // 1-shot kill op 100 HP
                case HitboxType.Legs:
                    return 70;  // Overleeft met 30 HP
                default:
                    return 100;
            }
        }
    }
}