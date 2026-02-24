using UnityEngine;
using Unity.MLAgents;

public class PRVGameManager : MonoBehaviour
{
    [Header("=== Agents ===")]
    public PRVAgent pouleAgent;
    public PRVAgent renardAgent;
    public PRVAgent vipereAgent;

    [Header("=== Parametres de l'arene ===")]
    [Tooltip("Demi-taille de la zone de jeu")]
    public float areaSize = 10f;

    [Tooltip("Duree max d'un episode en secondes")]
    public float maxEpisodeDuration = 30f;

    [Header("=== Spawn ===")]
    [Tooltip("Distance minimum entre les agents au spawn")]
    public float minSpawnDistance = 4f;

    private float episodeTimer;
    private int pouleScore, renardScore, vipereScore;
    private void Start()
    {
        Debug.Assert(pouleAgent.agentRole == PRVAgent.Role.Poule, "L'agent Poule doit avoir le role Poule !");
        Debug.Assert(renardAgent.agentRole == PRVAgent.Role.Renard, "L'agent Renard doit avoir le role Renard !");
        Debug.Assert(vipereAgent.agentRole == PRVAgent.Role.Vipere, "L'agent Vipere doit avoir le role Vipere !");

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
                  $"(Score: R={renardScore} P={pouleScore} V={vipereScore})");

        ResetAllAgents();
    }

    private void ResetAllAgents()
    {
        episodeTimer = 0f;

        Vector3[] spawnPositions = GenerateSpawnPositions(3);

        ResetSingleAgent(renardAgent, spawnPositions[0]);
        ResetSingleAgent(pouleAgent, spawnPositions[1]);
        ResetSingleAgent(vipereAgent, spawnPositions[2]);
    }

    private void ResetSingleAgent(PRVAgent agent, Vector3 position)
    {
        agent.transform.localPosition = position;
        agent.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        agent.gameObject.SetActive(true);
        agent.EndEpisode();
    }

    private Vector3[] GenerateSpawnPositions(int count)
    {
        Vector3[] positions = new Vector3[count];
        float margin = areaSize * 0.7f;

        for (int i = 0; i < count; i++)
        {
            int attempts = 0;
            bool valid;

            do
            {
                positions[i] = new Vector3(
                    Random.Range(-margin, margin),
                    0.5f,
                    Random.Range(-margin, margin)
                );

                valid = true;
                for (int j = 0; j < i; j++)
                {
                    if (Vector3.Distance(positions[i], positions[j]) < minSpawnDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                attempts++;
            } while (!valid && attempts < 50);
        }

        return positions;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize * 2, 0.1f, areaSize * 2));
    }

    /*private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        float y = 10f;
        GUI.Label(new Rect(10, y, 400, 25), $"Renard: {renardScore}", style); y += 25;
        GUI.Label(new Rect(10, y, 400, 25), $"Poule: {pouleScore}", style); y += 25;
        GUI.Label(new Rect(10, y, 400, 25), $"Vipere: {vipereScore}", style); y += 25;
        GUI.Label(new Rect(10, y, 400, 25), $"Temps: {episodeTimer:F1}s / {maxEpisodeDuration}s", style);
    }*/
}