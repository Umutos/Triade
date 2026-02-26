using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class PRVAgent : Agent
{
    public enum Role { Poule, Renard, Vipere }

    public Role agentRole;

    public float moveSpeed = 5f;
    public float rotationSpeed = 200f;

    [HideInInspector] public PRVGameManager gameManager;
    [HideInInspector] public Transform preyTransform;
    [HideInInspector] public Transform predatorTransform;

    private Rigidbody rb;

    private Vector3 pendingSpawnPosition;
    private Quaternion pendingSpawnRotation;
    private bool hasPendingSpawn = false;

    private float previousDistToPrey;

    private Vector3 RelativePosition(Vector3 worldPos)
    {
        Vector3 arenaCenter = gameManager != null ? gameManager.transform.position : Vector3.zero;
        return worldPos - arenaCenter;
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetPendingSpawn(Vector3 worldPosition, Quaternion rotation)
    {
        pendingSpawnPosition = worldPosition;
        pendingSpawnRotation = rotation;
        hasPendingSpawn = true;
    }

    public override void OnEpisodeBegin()
    {
        if (preyTransform != null)
            previousDistToPrey = Vector3.Distance(transform.position, preyTransform.position);
        else
            previousDistToPrey = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (hasPendingSpawn)
        {
            transform.position = pendingSpawnPosition;
            transform.rotation = pendingSpawnRotation;

            if (rb != null)
            {
                rb.position = pendingSpawnPosition;
                rb.rotation = pendingSpawnRotation;
            }

            hasPendingSpawn = false;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float areaSize = gameManager != null ? gameManager.areaSize : 10f;

        Vector3 myRelPos = RelativePosition(transform.position);
        sensor.AddObservation(myRelPos.x / areaSize);
        sensor.AddObservation(myRelPos.z / areaSize);

        sensor.AddObservation(transform.forward.x);
        sensor.AddObservation(transform.forward.z);

        if (preyTransform != null && preyTransform.gameObject.activeSelf)
        {
            Vector3 toPrey = preyTransform.position - transform.position;
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
            Vector3 toPredator = predatorTransform.position - transform.position;
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

        AddReward(-0.0005f);

        float areaSize = gameManager != null ? gameManager.areaSize : 10f;

        if (preyTransform != null && preyTransform.gameObject.activeSelf)
        {
            float distToPrey = Vector3.Distance(transform.position, preyTransform.position);

            float delta = previousDistToPrey - distToPrey;
            AddReward(delta * 0.01f);

            previousDistToPrey = distToPrey;
        }

        /*if (predatorTransform != null && predatorTransform.gameObject.activeSelf)
        {
            float distToPredator = Vector3.Distance(transform.position, predatorTransform.position);
            if (distToPredator < 3f)
            {
                AddReward(-0.002f);
            }
        }*/

        Vector3 relPos = RelativePosition(transform.position);
        if (Mathf.Abs(relPos.x) > areaSize || Mathf.Abs(relPos.z) > areaSize)
        {
            AddReward(-0.01f);

            relPos.x = Mathf.Clamp(relPos.x, -areaSize, areaSize);
            relPos.z = Mathf.Clamp(relPos.z, -areaSize, areaSize);

            Vector3 arenaCenter = gameManager != null ? gameManager.transform.position : Vector3.zero;
            transform.position = new Vector3(
                arenaCenter.x + relPos.x,
                transform.position.y,
                arenaCenter.z + relPos.z
            );
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

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.0005f);
        }
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