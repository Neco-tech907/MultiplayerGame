using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class PickupManager : MonoBehaviour
    {
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float respawnDelay = 10f;

        private bool hasSpawnedInitialPickups;

        private void Start()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (hasSpawnedInitialPickups)
            {
                return;
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                return;
            }

            TryInitialize();
        }

        private void OnEnable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            }
        }

        private void OnServerStarted()
        {
            TryInitialize();
        }

        public void OnPickedUp(Vector3 position)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            StartCoroutine(RespawnAfterDelay(position));
        }

        private void SpawnAll()
        {
            if (healthPickupPrefab == null || spawnPoints == null)
            {
                return;
            }

            foreach (Transform point in spawnPoints)
            {
                if (point == null)
                {
                    continue;
                }

                SpawnPickup(point.position);
            }
        }

        private IEnumerator RespawnAfterDelay(Vector3 position)
        {
            yield return new WaitForSeconds(respawnDelay);
            SpawnPickup(position);
        }

        private void SpawnPickup(Vector3 position)
        {
            if (healthPickupPrefab == null)
            {
                Debug.LogError("PickupManager requires a health pickup prefab.");
                return;
            }

            GameObject pickupObject = Instantiate(healthPickupPrefab, position, Quaternion.identity);
            HealthPickup healthPickup = pickupObject.GetComponent<HealthPickup>();
            NetworkObject networkObject = pickupObject.GetComponent<NetworkObject>();

            if (healthPickup == null || networkObject == null)
            {
                Debug.LogError("Health pickup prefab must contain HealthPickup and NetworkObject components.");
                Destroy(pickupObject);
                return;
            }

            healthPickup.Init(this);
            networkObject.Spawn();
        }

        private bool TryFindSpawnPoints(out Transform[] foundPoints)
        {
            PickupSpawnPoint[] markers = FindObjectsByType<PickupSpawnPoint>(FindObjectsSortMode.None);
            if (markers == null || markers.Length == 0)
            {
                foundPoints = null;
                return false;
            }

            foundPoints = new Transform[markers.Length];
            for (int i = 0; i < markers.Length; i++)
            {
                foundPoints[i] = markers[i].transform;
            }

            return true;
        }

        private void TryInitialize()
        {
            if (hasSpawnedInitialPickups || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if ((spawnPoints == null || spawnPoints.Length == 0) && TryFindSpawnPoints(out Transform[] foundPoints))
            {
                spawnPoints = foundPoints;
            }

            SpawnAll();
            hasSpawnedInitialPickups = true;
        }
    }
}
