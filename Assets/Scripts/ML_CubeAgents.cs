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
    private float previousDistance = float.MaxValue;
    
    private int stepCount = 0;
    private GameObject currentTarget;
    private GameObject currentPredator;
    private float previousDistanceTarget = 0f;
    private float previousDistancePredator = 0f;

    private Vector3 lastSignificantPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        myRenderer = GetComponent<Renderer>();
        UpdateColor();

        var raySensor = GetComponent<Unity.MLAgents.Sensors.RayPerceptionSensorComponent3D>();
        int tagCount = raySensor != null ? raySensor.DetectableTags.Count : -1;
    }

    public override void OnEpisodeBegin()
    {
        isAlive = true;
        gameObject.SetActive(true);

        envManager.MoveToSafeRandomPosition(this);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        stepCount = 0;
        lastSignificantPosition = transform.position;

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

    void FindPredator()
    {
        string predatorTag = "";
        if (myTeam == CubeTeam.Red) predatorTag = "Yellow";
        else if (myTeam == CubeTeam.Blue) predatorTag = "Red";
        else if (myTeam == CubeTeam.Yellow) predatorTag = "Blue";

        GameObject[] predators = GameObject.FindGameObjectsWithTag(predatorTag);
        if (predators.Length > 0)
        {
            float minDist = Mathf.Infinity;
            foreach (GameObject p in predators)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    currentPredator = p;
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

        if (currentTarget != null && currentTarget.gameObject.activeSelf)
        {
            Vector3 dirToTarget = (currentTarget.transform.localPosition - transform.localPosition).normalized;
            sensor.AddObservation(dirToTarget);            
            float distTarget = Vector3.Distance(transform.localPosition, currentTarget.transform.localPosition);
            sensor.AddObservation(distTarget / 20f);        
        }
        else
        {
            currentTarget = null;
            sensor.AddObservation(Vector3.zero);          
            sensor.AddObservation(0f);               
        }

        if (currentPredator != null && currentPredator.gameObject.activeSelf)
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
            sensor.AddObservation(Vector3.zero);         
            sensor.AddObservation(0f);                  
            sensor.AddObservation(Vector3.zero);        
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!isAlive) return;
        stepCount++;

        if (stepCount >= maxSteps)
        {
            AddReward(-1.0f);
            EndEpisode();
            return;
        }

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        Vector3 moveDir = new Vector3(moveX, 0, moveZ).normalized;
        rb.AddForce(moveDir * moveSpeed);

        AddReward(-0.001f);

        if (currentTarget != null && currentTarget.gameObject.activeSelf)
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
        }

        if (currentPredator != null && currentPredator.gameObject.activeSelf)
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
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    private bool isAlive = true;

    public void Revive()
    {
        isAlive = true;
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

        if (otherAgent != null)
        {
            if (IsPrey(otherAgent.myTeam))
            {
                AddReward(5.0f);

                otherAgent.isAlive = false;
                otherAgent.AddReward(-1.0f);
                envManager.OnAgentEliminated(otherAgent);
            }
            else if (IsPredator(otherAgent.myTeam))
            {
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