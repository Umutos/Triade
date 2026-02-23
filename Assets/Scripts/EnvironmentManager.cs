using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Paramètres de la Zone")]
    public float range = 10f;

    [Header("Zones de Spawn")]
    public Transform spawnZoneRed;
    public Transform spawnZoneBlue;
    public Transform spawnZoneYellow;
    public float spawnRadius = 3f;

    [Header("Liste des Agents")]
    public List<ML_CubeAgents> agents;

    private const float spawnSafetyRadius = 1.2f;

    void Start()
    {
        ResetRound();
    }

    public void ResetRound()
    {
        foreach (var agent in agents)
        {
            agent.gameObject.SetActive(true);
            agent.Revive();
            MoveToSafeRandomPosition(agent);
        }
    }

    public void OnAgentEliminated(ML_CubeAgents eliminated)
    {
        eliminated.gameObject.SetActive(false);

        HashSet<CubeTeam> aliveTeams = new HashSet<CubeTeam>();
        foreach (var agent in agents)
        {
            if (agent.gameObject.activeSelf)
            {
                aliveTeams.Add(agent.myTeam);
            }
        }

        if (aliveTeams.Count <= 1)
        {
            if (aliveTeams.Count == 1)
            {
                CubeTeam winnerTeam = aliveTeams.First();
                foreach (var agent in agents)
                {
                    if (agent.gameObject.activeSelf && agent.myTeam == winnerTeam)
                    {
                        agent.AddReward(3.0f);
                    }
                }
            }

            foreach (var agent in agents)
            {
                agent.gameObject.SetActive(true);
            }

            foreach (var agent in agents)
            {
                agent.EndEpisode();
            }
        }
    }

    public void MoveToSafeRandomPosition(ML_CubeAgents agent)
    {
        Transform spawnZone = null;
        if (agent.myTeam == CubeTeam.Red) spawnZone = spawnZoneRed;
        else if (agent.myTeam == CubeTeam.Blue) spawnZone = spawnZoneBlue;
        else if (agent.myTeam == CubeTeam.Yellow) spawnZone = spawnZoneYellow;

        if (spawnZone == null) spawnZone = transform;

        bool safePositionFound = false;
        int attempts = 100;
        Vector3 potentialPosition = Vector3.zero;

        while (!safePositionFound && attempts > 0)
        {
            attempts--;
            float x = Random.Range(-spawnRadius, spawnRadius);
            float z = Random.Range(-spawnRadius, spawnRadius);
            potentialPosition = spawnZone.position + new Vector3(x, 0.5f, z);

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
                safePositionFound = true;
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
    }
}