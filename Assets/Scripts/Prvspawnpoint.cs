using UnityEngine;

public class PRVSpawnPoint : MonoBehaviour
{
    [Tooltip("Rayon autour du point dans lequel l'agent peut apparaître")]
    public float spawnRadius = 1.5f;

    [Tooltip("Hauteur de spawn (Y)")]
    public float spawnHeight = 0.5f;

    public Vector3 GetRandomPosition()
    {   
        return new Vector3(
            transform.position.x,
            spawnHeight,
            transform.position.z
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, spawnRadius);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        float cross = 0.3f;
        Gizmos.DrawLine(transform.position - Vector3.right * cross,
                        transform.position + Vector3.right * cross);
        Gizmos.DrawLine(transform.position - Vector3.forward * cross,
                        transform.position + Vector3.forward * cross);
    }
}