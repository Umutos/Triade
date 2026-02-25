using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;

public class PRVGameManager : MonoBehaviour
{
    [Header("=== Agents ===")]
    public PRVAgent pouleAgent;
    public PRVAgent renardAgent;
    public PRVAgent vipereAgent;

    [Header("=== Spawn Points ===")]
    [Tooltip("Tous les spawn points de la map. Ceux hors de la zone active seront ignorés automatiquement.")]
    public PRVSpawnPoint[] spawnPoints;

    [Header("=== Paramètres par défaut (écrasés par le curriculum) ===")]
    public float areaSize = 10f;
    public float maxEpisodeDuration = 30f;

    private float episodeTimer;
    private int pouleScore, renardScore, vipereScore;
    private EnvironmentParameters envParams;

    private void Start()
    {
        envParams = Academy.Instance.EnvironmentParameters;

        if (spawnPoints == null || spawnPoints.Length < 3)
        {
            Debug.LogError("[PRV] Il faut au minimum 3 Spawn Points !");
            return;
        }

        SetupAgentReferences();
        ResetAllAgents();
    }

    private void SetupAgentReferences()
    {
        renardAgent.gameManager = this;
        renardAgent.preyTransform = pouleAgent.transform;
        renardAgent.predatorTransform = vipereAgent.transform;

        pouleAgent.gameManager = this;
        pouleAgent.preyTransform = vipereAgent.transform;
        pouleAgent.predatorTransform = renardAgent.transform;

        vipereAgent.gameManager = this;
        vipereAgent.preyTransform = renardAgent.transform;
        vipereAgent.predatorTransform = pouleAgent.transform;
    }

    private void FixedUpdate()
    {
        episodeTimer += Time.fixedDeltaTime;

        if (episodeTimer >= maxEpisodeDuration)
        {
            pouleAgent.AddReward(-0.1f);
            renardAgent.AddReward(-0.1f);
            vipereAgent.AddReward(-0.1f);
            ResetAllAgents();
        }
    }

    public void OnAgentEaten(PRVAgent prey, PRVAgent hunter)
    {
        switch (hunter.agentRole)
        {
            case PRVAgent.Role.Renard: renardScore++; break;
            case PRVAgent.Role.Poule: pouleScore++; break;
            case PRVAgent.Role.Vipere: vipereScore++; break;
        }

        Debug.Log($"[PRV] {hunter.agentRole} a mange {prey.agentRole} ! " +
                  $"(R={renardScore} P={pouleScore} V={vipereScore})");

        ResetAllAgents();
    }

    private void ResetAllAgents()
    {
        episodeTimer = 0f;

        areaSize = envParams.GetWithDefault("area_size", areaSize);
        maxEpisodeDuration = envParams.GetWithDefault("episode_duration", maxEpisodeDuration);

        List<PRVSpawnPoint> validSpawns = GetValidSpawnPoints();

        if (validSpawns.Count < 3)
        {
            Debug.LogWarning($"[PRV] Seulement {validSpawns.Count} spawn points dans la zone {areaSize}. " +
                            "Ajoute des spawners plus proches du centre !");
            validSpawns = new List<PRVSpawnPoint>(spawnPoints);
        }

        Vector3[] positions = PickSpawnPositions(validSpawns, 3);

        SpawnAgent(renardAgent, positions[0]);
        SpawnAgent(pouleAgent, positions[1]);
        SpawnAgent(vipereAgent, positions[2]);
    }

    private List<PRVSpawnPoint> GetValidSpawnPoints()
    {
        List<PRVSpawnPoint> valid = new List<PRVSpawnPoint>();
        Vector3 center = transform.position;

        foreach (PRVSpawnPoint sp in spawnPoints)
        {
            Vector3 relative = sp.transform.position - center;

            if (Mathf.Abs(relative.x) <= areaSize && Mathf.Abs(relative.z) <= areaSize)
            {
                valid.Add(sp);
            }
        }

        return valid;
    }

    private Vector3[] PickSpawnPositions(List<PRVSpawnPoint> validSpawns, int count)
    {
        Vector3[] result = new Vector3[count];

        for (int i = validSpawns.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (validSpawns[i], validSpawns[j]) = (validSpawns[j], validSpawns[i]);
        }

        for (int i = 0; i < count; i++)
        {
            if (i < validSpawns.Count)
            {
                result[i] = validSpawns[i].GetRandomPosition();
            }
            else
            {
                result[i] = validSpawns[Random.Range(0, validSpawns.Count)].GetRandomPosition();
            }
        }

        return result;
    }

    private void SpawnAgent(PRVAgent agent, Vector3 worldPosition)
    {
        agent.gameObject.SetActive(true);
        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        agent.SetPendingSpawn(worldPosition, rot);
        agent.EndEpisode();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize * 2, 0.1f, areaSize * 2));
    }
}