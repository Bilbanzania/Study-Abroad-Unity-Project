using UnityEngine;
using System.Linq;

public class MoveAtoB : MonoBehaviour
{
    public Transform[] targets;
    // Max speed is now controlled by GameManager
    public float acceleration = 0.02f;
    public float decelerationStartDistance = 10f;
    public int agentsToSpawnAtStation = 5; // Fallback if GameManager not found

    private float internalMaxSpeed = 5f;
    private int currentOriginIndex = 0;
    private int currentDestinationIndex = 0;
    private float currentSpeed = 0f;

    private enum TrainState { IdleBeforeStart, Accelerating, Cruising, Decelerating, AtStation }
    private TrainState currentState = TrainState.IdleBeforeStart;

    private int stationWaitTimer = -1;
    private const int STATION_WAIT_DURATION_STEPS = 120;

    void Start()
    {
        if (targets == null || targets.Length == 0)
        {
            Debug.LogError("MoveAtoB CRITICAL: Targets array is null or empty! Disabling script.", gameObject);
            enabled = false;
            return;
        }

        if (GameManager.Instance != null)
        {
            UpdateMaxSpeedFromGameManager(GameManager.Instance.currentTrainMaxSpeed);
        }
        else
        {
            this.internalMaxSpeed = 5f;
        }

        transform.position = targets[0].position;
        currentSpeed = 0f;
        currentState = TrainState.IdleBeforeStart;

        if (targets.Length > 1)
        {
            currentOriginIndex = 0;
            currentDestinationIndex = 1;
            LookAtTarget(targets[currentDestinationIndex]);
        }
    }

    public void UpdateMaxSpeedFromGameManager(float newMaxSpeed)
    {
        this.internalMaxSpeed = newMaxSpeed;
        if (currentSpeed > this.internalMaxSpeed)
        {
            currentSpeed = this.internalMaxSpeed;
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsSimulationRunning)
        {
            currentSpeed = 0f;
            return;
        }

        if (currentState == TrainState.IdleBeforeStart)
        {
            if (targets.Length == 1)
            {
                if (IsStation(targets[0]))
                {
                    SnapToTarget(targets[0]);
                    PerformStationActions(targets[0]);
                }
                else { enabled = false; }
            }
            else if (targets.Length > 1)
            {
                currentState = TrainState.Accelerating;
                LookAtTarget(targets[currentDestinationIndex]);
            }
            else { enabled = false; return; }
        }

        if (currentDestinationIndex >= targets.Length && currentState != TrainState.AtStation)
        {
            HandlePathCompletion(); return;
        }
        Transform destination = (currentDestinationIndex < targets.Length) ? targets[currentDestinationIndex] : null;
        if (destination == null && currentState != TrainState.AtStation)
        {
            HandlePathCompletion(); return;
        }

        float distanceToDestination = (destination != null) ? Vector3.Distance(transform.position, destination.position) : float.MaxValue;

        switch (currentState)
        {
            case TrainState.Accelerating:
                currentSpeed += acceleration;
                if (currentSpeed >= internalMaxSpeed)
                {
                    currentSpeed = internalMaxSpeed;
                    currentState = TrainState.Cruising;
                }
                if (destination != null && IsStation(destination) && distanceToDestination <= decelerationStartDistance)
                {
                    currentState = TrainState.Decelerating;
                }
                break;
            case TrainState.Cruising:
                if (destination != null && IsStation(destination) && distanceToDestination <= decelerationStartDistance)
                {
                    currentState = TrainState.Decelerating;
                }
                break;
            case TrainState.Decelerating:
                currentSpeed -= acceleration;
                if (currentSpeed <= 0f) currentSpeed = 0f;
                break;
            case TrainState.AtStation:
                stationWaitTimer--;
                if (stationWaitTimer <= 0)
                {
                    AdvanceToNextSegment();
                    if (currentDestinationIndex < targets.Length)
                    {
                        currentState = TrainState.Accelerating;
                        LookAtTarget(targets[currentDestinationIndex]);
                    }
                }
                return;
        }

        if (currentSpeed > 0 && destination != null && currentState != TrainState.IdleBeforeStart)
        {
            Vector3 direction = (destination.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
                transform.position += direction * currentSpeed * Time.fixedDeltaTime;
            }
        }

        if (destination != null && currentState != TrainState.AtStation && currentState != TrainState.IdleBeforeStart &&
            Vector3.Distance(transform.position, destination.position) < 0.5f)
        {
            SnapToTarget(destination);
            currentSpeed = 0f;
            if (IsStation(destination)) PerformStationActions(destination);
            else
            {
                AdvanceToNextSegment();
                if (currentDestinationIndex < targets.Length)
                {
                    currentState = TrainState.Accelerating;
                    LookAtTarget(targets[currentDestinationIndex]);
                }
            }
        }
    }

    bool IsStation(Transform targetNode)
    {
        if (targetNode == null) return false;
        return targetNode.GetComponent<AgentSpawner>() != null;
    }

    void PerformStationActions(Transform stationNode)
    {
        currentState = TrainState.AtStation;
        stationWaitTimer = STATION_WAIT_DURATION_STEPS;
        AgentSpawner spawner = stationNode.GetComponent<AgentSpawner>();
        if (spawner != null)
        {
            int spawnCount = GameManager.Instance != null ? GameManager.Instance.currentAgentsPerBatch : agentsToSpawnAtStation;
            spawner.SpawnAgents(spawnCount);
        }
    }

    void AdvanceToNextSegment()
    {
        currentOriginIndex = currentDestinationIndex;
        currentDestinationIndex++;
        if (currentDestinationIndex >= targets.Length) HandlePathCompletion();
    }

    void SnapToTarget(Transform target)
    {
        if (target != null) transform.position = target.position;
    }

    void LookAtTarget(Transform target)
    {
        if (target == null) return;
        if (transform.position == target.position && (currentDestinationIndex >= targets.Length - 1 || target == targets[targets.Length - 1])) return;
        Vector3 direction = (target.position - transform.position).normalized;
        if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
    }

    void HandlePathCompletion()
    {
        currentSpeed = 0f;
        if (targets.Length == 0) { enabled = false; return; }
        transform.position = targets[0].position;
        if (targets.Length == 1)
        {
            currentOriginIndex = 0; currentDestinationIndex = 0;
            if (IsStation(targets[0])) PerformStationActions(targets[0]);
            else currentState = TrainState.IdleBeforeStart;
        }
        else if (targets.Length > 1)
        {
            currentOriginIndex = 0; currentDestinationIndex = 1;
            LookAtTarget(targets[currentDestinationIndex]);
            currentState = TrainState.Accelerating;
        }
    }
}