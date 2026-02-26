using UnityEngine;
using Unity.MLAgents;
using System.Collections.Generic;

public class PRVGameManager : MonoBehaviour
{
    public PRVAgent[] allAgents;

    public PRVSpawnPoint[] spawnPoints;

    public float areaSize = 10f;
    public float maxEpisodeDuration = 30f;

    public float winBonus = 0.5f;

    private float episodeTimer;
    private int pouleScore, renardScore, vipereScore;
    private EnvironmentParameters envParams;

    private void Start()
    {
        envParams = Academy.Instance.EnvironmentParameters;

        if (allAgents == null || allAgents.Length < 3)
        {
            Debug.LogError("[PRV] Il faut au minimum 3 agents !");
            return;
        }

        foreach (PRVAgent agent in allAgents)
        {
            agent.gameManager = this;
        }

        ResetAllAgents();
    }

    private void FixedUpdate()
    {
        episodeTimer += Time.fixedDeltaTime;

        if (episodeTimer >= maxEpisodeDuration)
        {
            foreach (PRVAgent agent in allAgents)
            {
                if (agent.IsAlive)
                {
                    agent.AddReward(-0.1f);
                }
            }
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

        bool poulesAlive = false;
        bool renardsAlive = false;
        bool viperesAlive = false;

        foreach (PRVAgent agent in allAgents)
        {
            if (!agent.IsAlive) continue;

            switch (agent.agentRole)
            {
                case PRVAgent.Role.Poule: poulesAlive = true; break;
                case PRVAgent.Role.Renard: renardsAlive = true; break;
                case PRVAgent.Role.Vipere: viperesAlive = true; break;
            }
        }

        int rolesAlive = 0;
        if (poulesAlive) rolesAlive++;
        if (renardsAlive) rolesAlive++;
        if (viperesAlive) rolesAlive++;

        if (rolesAlive <= 1)
        {
            foreach (PRVAgent agent in allAgents)
            {
                if (agent.IsAlive)
                {
                    agent.AddReward(winBonus);
                }
            }

            ResetAllAgents();
        }
    }

    private void ResetAllAgents()
    {
        episodeTimer = 0f;

        areaSize = envParams.GetWithDefault("area_size", areaSize);
        maxEpisodeDuration = envParams.GetWithDefault("episode_duration", maxEpisodeDuration);

        List<PRVSpawnPoint> validSpawns = GetValidSpawnPoints();

        if (validSpawns.Count < allAgents.Length)
        {
            validSpawns = new List<PRVSpawnPoint>(spawnPoints);
        }

        for (int i = validSpawns.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (validSpawns[i], validSpawns[j]) = (validSpawns[j], validSpawns[i]);
        }

        for (int i = 0; i < allAgents.Length; i++)
        {
            int spawnIdx = i % validSpawns.Count;
            Vector3 pos = validSpawns[spawnIdx].GetRandomPosition();
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            allAgents[i].gameObject.SetActive(true);
            allAgents[i].SetPendingSpawn(pos, rot);
            allAgents[i].EndEpisode();
        }
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize * 2, 0.1f, areaSize * 2));
    }
}