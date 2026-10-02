using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SniperGame.UI.DamageNumbers
{
    public class DamageNumberManager : MonoBehaviour
    {
        public static DamageNumberManager Instance { get; private set; }

        [SerializeField] private DamageNumberItem damageNumberPrefab;
        [SerializeField] private TMP_FontAsset customFont;
        [SerializeField] private int initialPoolSize = 15;

        private readonly Queue<DamageNumberItem> _pool = new();
        private Transform _poolParent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _poolParent = new GameObject("DamageNumberPool").transform;
            _poolParent.SetParent(transform);

            // Prewarm pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                var item = CreateNewItem();
                item.gameObject.SetActive(false);
                _pool.Enqueue(item);
            }
        }

        private DamageNumberItem CreateNewItem()
        {
            if (damageNumberPrefab != null)
            {
                var instantiated = Instantiate(damageNumberPrefab, _poolParent);
                return instantiated;
            }

            // Procedural fallback: creates clean 3D TextMeshPro GameObject
            var go = new GameObject("DamageNumber_Procedural");
            go.transform.SetParent(_poolParent);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 6f;

            if (customFont != null)
            {
                tmp.font = customFont;
            }
            else
            {
                // Try load Working Fonts AkenaOtos SDF
                var font = Resources.Load<TMP_FontAsset>("AkenaOtos SDF");
                if (font != null) tmp.font = font;
            }

            var item = go.AddComponent<DamageNumberItem>();
            return item;
        }

        public void SpawnDamageNumber(int damage, Vector3 worldPosition, bool isCrit)
        {
            DamageNumberItem item = (_pool.Count > 0) ? _pool.Dequeue() : CreateNewItem();
            item.Initialize(damage, worldPosition, isCrit, ReturnToPool);
        }

        private void ReturnToPool(DamageNumberItem item)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
                _pool.Enqueue(item);
            }
        }
    }
}
