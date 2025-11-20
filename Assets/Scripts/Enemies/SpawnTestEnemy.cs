using Unity.VisualScripting;
using UnityEngine;

public class SpawnTestEnemy : MonoBehaviour
{
    public KeyCode spawnEnemyInput = KeyCode.Minus;

    public GameObject enemyPrefab;

    public Transform spawnPoint;

    private void Update()
    {
        if (Input.GetKeyDown(spawnEnemyInput))
            {
                Instantiate(enemyPrefab, spawnPoint.transform);
            }
    }
}
