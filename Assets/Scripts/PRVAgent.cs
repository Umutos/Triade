using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PRVAgent : Agent
{
    public enum Role { Poule, Renard, Vipere }

    [Header("=== Configuration ===")]
    public Role agentRole;

    [Header("=== Mouvement ===")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;

    [Header("=== Références (assignées par le GameManager) ===")]
    [HideInInspector] public PRVGameManager gameManager;
    [HideInInspector] public Transform preyTransform; 
    [HideInInspector] public Transform predatorTransform;

    private Rigidbody rb;
    private Vector3 startPosition;
    private float existentialTimer;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPosition + new Vector3(
            Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)
        );
        transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        existentialTimer = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float areaSize = gameManager != null ? gameManager.areaSize : 10f;
        sensor.AddObservation(transform.localPosition.x / areaSize);
        sensor.AddObservation(transform.localPosition.z / areaSize);

        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        if (preyTransform != null && preyTransform.gameObject.activeSelf)
        {
            Vector3 toPrey = preyTransform.localPosition - transform.localPosition;
            sensor.AddObservation(toPrey.x / areaSize);
            sensor.AddObservation(toPrey.z / areaSize);
            sensor.AddObservation(toPrey.magnitude / areaSize);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
        }

        if (predatorTransform != null && predatorTransform.gameObject.activeSelf)
        {
            Vector3 toPredator = predatorTransform.localPosition - transform.localPosition;
            sensor.AddObservation(toPredator.x / areaSize);
            sensor.AddObservation(toPredator.z / areaSize);
            sensor.AddObservation(toPredator.magnitude / areaSize);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
        }

        sensor.AddOneHotObservation((int)agentRole, 3);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveInput = actions.ContinuousActions[0]; 
        float turnInput = actions.ContinuousActions[1]; 

        Vector3 move = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        float rotation = turnInput * rotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0f, rotation, 0f);

        existentialTimer += Time.fixedDeltaTime;
        AddReward(-0.0005f);

        if (preyTransform != null && preyTransform.gameObject.activeSelf)
        {
            float distToPrey = Vector3.Distance(transform.localPosition, preyTransform.localPosition);
            float areaSize = gameManager != null ? gameManager.areaSize : 10f;
            AddReward((1f - distToPrey / areaSize) * 0.001f);
        }

        if (predatorTransform != null && predatorTransform.gameObject.activeSelf)
        {
            float distToPredator = Vector3.Distance(transform.localPosition, predatorTransform.localPosition);
            if (distToPredator < 3f)
            {
                AddReward(-0.002f);
            }
        }

        float aSize = gameManager != null ? gameManager.areaSize : 10f;
        if (Mathf.Abs(transform.localPosition.x) > aSize ||
            Mathf.Abs(transform.localPosition.z) > aSize)
        {
            AddReward(-0.01f);
            Vector3 pos = transform.localPosition;
            pos.x = Mathf.Clamp(pos.x, -aSize, aSize);
            pos.z = Mathf.Clamp(pos.z, -aSize, aSize);
            transform.localPosition = pos;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        continuous[0] = Input.GetAxis("Vertical");
        continuous[1] = Input.GetAxis("Horizontal");
    }

    private void OnCollisionEnter(Collision collision)
    {
        PRVAgent other = collision.gameObject.GetComponent<PRVAgent>();
        if (other == null) return;

        if (IsPrey(other.agentRole))
        {
            OnAteTarget(other);
        }
    }

    public bool IsPrey(Role otherRole)
    {
        return agentRole switch
        {
            Role.Renard => otherRole == Role.Poule,
            Role.Poule => otherRole == Role.Vipere,
            Role.Vipere => otherRole == Role.Renard,
            _ => false
        };
    }

    public bool IsPredator(Role otherRole)
    {
        return agentRole switch
        {
            Role.Poule => otherRole == Role.Renard,
            Role.Vipere => otherRole == Role.Poule,
            Role.Renard => otherRole == Role.Vipere,
            _ => false
        };
    }

    private void OnAteTarget(PRVAgent prey)
    {
        AddReward(1.0f);  
        prey.AddReward(-1.0f); 

        if (gameManager != null)
        {
            gameManager.OnAgentEaten(prey, this);
        }
    }
}