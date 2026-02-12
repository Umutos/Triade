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

public class ML_CubeAgents: Agent
{
    public CubeTeam myTeam;
    public float moveSpeed = 10f;
    public int maxSteps = 1000;

    public Material matRed;
    public Material matBlue;
    public Material matYellow;

    private Rigidbody rb;
    private Renderer myRenderer;
    [SerializeField] private EnvironmentManager envManager;
    
    private int stepCount = 0;
    private GameObject currentTarget;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        myRenderer = GetComponent<Renderer>();
        UpdateColor();
    }

    public override void OnEpisodeBegin()
    {
        envManager.MoveToSafeRandomPosition(this);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        stepCount = 0;
        FindTarget();
    }

    void FindTarget()
    {
        string targetTag = "";
        if (myTeam == CubeTeam.Red) targetTag = "Blue";
        else if (myTeam == CubeTeam.Blue) targetTag = "Yellow";
        else if (myTeam == CubeTeam.Yellow) targetTag = "Red";

        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        if (targets.Length > 0)
        {
            float minDist = Mathf.Infinity;
            foreach (GameObject t in targets)
            {
                float dist = Vector3.Distance(transform.position, t.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    currentTarget = t;
                }
            }
        }
    }

    void UpdateColor()
    {
        if (myTeam == CubeTeam.Red) myRenderer.material = matRed;
        else if (myTeam == CubeTeam.Blue) myRenderer.material = matBlue;
        else if (myTeam == CubeTeam.Yellow) myRenderer.material = matYellow;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        
        sensor.AddObservation(rb.linearVelocity);
        
        if (currentTarget != null)
        {
            Vector3 dirToTarget = (currentTarget.transform.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(dirToTarget);
            
            float distance = Vector3.Distance(transform.localPosition, currentTarget.transform.localPosition);
            sensor.AddObservation(distance / 20f);
            
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                sensor.AddObservation(targetRb.linearVelocity);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
            }
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f); 
            sensor.AddObservation(Vector3.zero);
        }
        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        stepCount++;
        
        if (stepCount >= maxSteps)
        {
            AddReward(-0.5f);
            EndEpisode();
            return;
        }

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        Vector3 moveDir = new Vector3(moveX, 0, moveZ).normalized;
        rb.AddForce(moveDir * moveSpeed);

        AddReward(-0.001f);

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
            
            float normalizedDistance = distance / 20f;
            AddReward((1f - normalizedDistance) * 0.01f);
        }
        else
        {
            FindTarget();
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
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f);
            EndEpisode();
            return;
        }

        ML_CubeAgents otherAgent = collision.gameObject.GetComponent<ML_CubeAgents>();

        if (otherAgent != null)
        {
            if (IsPrey(otherAgent.myTeam))
            {
                AddReward(2.0f);
                EndEpisode();
                Debug.Log(myTeam + " mange " + otherAgent.myTeam);

                otherAgent.AddReward(-1.0f);
                otherAgent.EndEpisode();
            }
            else if (IsPredator(otherAgent.myTeam))
            {
                AddReward(-1.0f);
                EndEpisode();
            }
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