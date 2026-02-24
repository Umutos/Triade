using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public enum CubeTeam
{
    Red,
    Blue,
    Yellow
}

public class ML_CubeAgents : Agent
{
    public CubeTeam myTeam;
    public float moveSpeed = 10f;

    public Material matRed;
    public Material matBlue;
    public Material matYellow;

    private Rigidbody rb;
    private Renderer myRenderer;
    private Collider myCollider;
    [SerializeField] private EnvironmentManager envManager;

    private int stepCount = 0;
    private GameObject currentTarget;
    private GameObject currentPredator;
    private float previousDistanceTarget = 0f;
    private float previousDistancePredator = 0f;

    [HideInInspector] public bool isAlive = true;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        myRenderer = GetComponent<Renderer>();
        myCollider = GetComponent<Collider>();
        UpdateColor();
    }

    public override void OnEpisodeBegin()
    {
        Revive();
        envManager.MoveToSafeRandomPosition(this);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        stepCount = 0;

        FindTarget();
        FindPredator();

        if (currentTarget != null)
            previousDistanceTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        else
            previousDistanceTarget = 0f;

        if (currentPredator != null)
            previousDistancePredator = Vector3.Distance(transform.position, currentPredator.transform.position);
        else
            previousDistancePredator = 0f;
    }

    public void Revive()
    {
        isAlive = true;
        myRenderer.enabled = true;
        myCollider.enabled = true;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }


    public void Disable()
    {
        isAlive = false;
        myRenderer.enabled = false;
        myCollider.enabled = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
    void FindTarget()
    {
        currentTarget = FindClosestAgentOfRole(GetPreyTeam());
    }

    void FindPredator()
    {
        currentPredator = FindClosestAgentOfRole(GetPredatorTeam());
    }

    GameObject FindClosestAgentOfRole(CubeTeam targetTeam)
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var agent in envManager.agents)
        {
            if (!agent.isAlive) continue;
            if (agent.myTeam != targetTeam) continue;

            float dist = Vector3.Distance(transform.position, agent.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = agent.gameObject;
            }
        }
        return closest;
    }

    CubeTeam GetPreyTeam()
    {
        if (myTeam == CubeTeam.Red) return CubeTeam.Blue;
        if (myTeam == CubeTeam.Blue) return CubeTeam.Yellow;
        return CubeTeam.Red;
    }

    CubeTeam GetPredatorTeam()
    {
        if (myTeam == CubeTeam.Red) return CubeTeam.Yellow;
        if (myTeam == CubeTeam.Blue) return CubeTeam.Red;
        return CubeTeam.Blue;
    }

    void UpdateColor()
    {
        if (myTeam == CubeTeam.Red) myRenderer.material = matRed;
        else if (myTeam == CubeTeam.Blue) myRenderer.material = matBlue;
        else if (myTeam == CubeTeam.Yellow) myRenderer.material = matYellow;
    }

    bool IsValidTarget(GameObject obj)
    {
        if (obj == null) return false;
        var agent = obj.GetComponent<ML_CubeAgents>();
        return agent != null && agent.isAlive;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rb.linearVelocity);

        if (IsValidTarget(currentTarget))
        {
            Vector3 dirToTarget = (currentTarget.transform.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(dirToTarget);
            float distTarget = Vector3.Distance(transform.localPosition, currentTarget.transform.localPosition);
            sensor.AddObservation(distTarget / 20f);
        }
        else
        {
            currentTarget = null;
            FindTarget();
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        if (IsValidTarget(currentPredator))
        {
            Vector3 dirToPredator = (currentPredator.transform.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(dirToPredator);
            float distPredator = Vector3.Distance(transform.localPosition, currentPredator.transform.localPosition);
            sensor.AddObservation(distPredator / 20f);

            Rigidbody predRb = currentPredator.GetComponent<Rigidbody>();
            if (predRb != null)
                sensor.AddObservation(predRb.linearVelocity);
            else
                sensor.AddObservation(Vector3.zero);
        }
        else
        {
            currentPredator = null;
            FindPredator();
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(Vector3.zero);
        }
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!isAlive) return;
        stepCount++;

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        Vector3 moveDir = new Vector3(moveX, 0, moveZ).normalized;
        rb.AddForce(moveDir * moveSpeed);

        AddReward(-0.001f);

        if (IsValidTarget(currentTarget))
        {
            float distTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            float deltaTarget = previousDistanceTarget - distTarget;
            deltaTarget = Mathf.Clamp(deltaTarget, -1f, 1f);
            AddReward(deltaTarget * 0.05f);
            previousDistanceTarget = distTarget;
        }
        else
        {
            currentTarget = null;
            FindTarget();
            if (currentTarget != null)
                previousDistanceTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        }

        if (IsValidTarget(currentPredator))
        {
            float distPredator = Vector3.Distance(transform.position, currentPredator.transform.position);
            float deltaPredator = distPredator - previousDistancePredator;
            deltaPredator = Mathf.Clamp(deltaPredator, -1f, 1f);
            AddReward(deltaPredator * 0.05f);
            previousDistancePredator = distPredator;
        }
        else
        {
            currentPredator = null;
            FindPredator();
            if (currentPredator != null)
                previousDistancePredator = Vector3.Distance(transform.position, currentPredator.transform.position);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isAlive) return;

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
            return;
        }

        ML_CubeAgents otherAgent = collision.gameObject.GetComponent<ML_CubeAgents>();

        if (otherAgent != null && otherAgent.isAlive)
        {
            if (IsPrey(otherAgent.myTeam))
            {
                AddReward(5.0f);
                otherAgent.AddReward(-1.0f);
                envManager.OnAgentEliminated(otherAgent);
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f);
        }
    }

    bool IsPrey(CubeTeam otherTeam)
    {
        if (myTeam == CubeTeam.Red && otherTeam == CubeTeam.Blue) return true;
        if (myTeam == CubeTeam.Blue && otherTeam == CubeTeam.Yellow) return true;
        if (myTeam == CubeTeam.Yellow && otherTeam == CubeTeam.Red) return true;
        return false;
    }

    bool IsPredator(CubeTeam otherTeam)
    {
        if (myTeam == CubeTeam.Red && otherTeam == CubeTeam.Yellow) return true;
        if (myTeam == CubeTeam.Blue && otherTeam == CubeTeam.Red) return true;
        if (myTeam == CubeTeam.Yellow && otherTeam == CubeTeam.Blue) return true;
        return false;
    }
}