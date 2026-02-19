using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Parameres de la Zone")]
    public float range = 10f; 

    [Header("Liste des Agents")]
    public List<ML_CubeAgents> agents;

    private const float spawnSafetyRadius = 1.2f;

    void Start()
    {
        ResetAllAgents();
    }

    public void ResetAllAgents()
    {
        foreach (var agent in agents)
        {
            MoveToSafeRandomPosition(agent);
        }
    }

    public void MoveToSafeRandomPosition(ML_CubeAgents agent)
    {
        bool safePositionFound = false;
        int attempts = 100;
        Vector3 potentialPosition = Vector3.zero;

        while (!safePositionFound && attempts > 0)
        {
            attempts--;

            float x = Random.Range(-range, range);
            float z = Random.Range(-range, range);

            potentialPosition = transform.position + new Vector3(x, 0.5f, z);

            Collider[] colliders = Physics.OverlapSphere(potentialPosition, spawnSafetyRadius);

            bool collisionFound = false;
            foreach (var col in colliders)
            {
                if (col.gameObject != agent.gameObject && !col.CompareTag("Ground"))
                {
                    collisionFound = true;
                    break;
                }
            }

            if (!collisionFound)
            {
                safePositionFound = true;
            }
        }

        if (safePositionFound)
        {
            agent.transform.position = potentialPosition;
            agent.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Rigidbody rb = agent.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("Impossible de trouver une place libre pour " + agent.name);
        }
    }
}