using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class AgentAI : MonoBehaviour
{
    public enum State { Initializing, SeekingStudySpot, MovingToStudySpot, Studying, WaitingNearRoom, MovingToExit, ReachedExit }
    public State currentState = State.Initializing;

    private NavMeshAgent navAgent;
    private AgentGPA agentGPA;
    private StudyRoom targetStudyRoom;
    private Transform assignedStudySpot;
    private Transform taitoStationExit;
    private Transform currentExitPoint;

    private float stateTimer;
    private float seekRetryTimer = 1f;
    private const float SEEK_INTERVAL = 1.25f;

    private float agentMaxStudyTime;
    private float agentMaxWaitTime;
    private float agentGpaBoostRate;
    private float agentGpaWaitingPenaltyRate;
    private float agentGpaLeaveNoStudyPenalty;
    private float moneySpentAtArcade = 0f;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        agentGPA = GetComponent<AgentGPA>();
        if (navAgent == null || agentGPA == null) { Debug.LogError($"Agent {gameObject.name}: NavMeshAgent or AgentGPA is missing! Destroying."); Destroy(gameObject); return; }

        if (!navAgent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.5f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
            }
            else
            {
                Debug.LogError($"Agent {gameObject.name}: FAILED to warp to NavMesh from {transform.position}. Destroying agent.");
                Destroy(gameObject); return;
            }
        }

        if (GameManager.Instance != null)
        {
            UpdateParametersFromGameManager();
            taitoStationExit = GameManager.Instance.GetTaitoStationExitPoint();
            if (taitoStationExit == null) Debug.LogError($"Agent {gameObject.name}: TaitoStationExitPoint is NULL from GameManager. Agents may not exit correctly.");
        }
        else
        {
            Debug.LogError($"Agent {gameObject.name}: GameManager.Instance is null in Start! Agent will not function correctly.");
        }
        SetState(State.SeekingStudySpot);
    }

    public void UpdateSpeed(float newSpeed)
    {
        if (navAgent != null && navAgent.isOnNavMesh) navAgent.speed = newSpeed;
    }

    public void UpdateParametersFromGameManager()
    {
        if (GameManager.Instance == null) return;
        if (navAgent != null && navAgent.isOnNavMesh) navAgent.speed = GameManager.Instance.currentAgentSpeed;
        agentMaxStudyTime = GameManager.Instance.currentStudyDuration;
        agentMaxWaitTime = GameManager.Instance.currentMaxWaitTime;
        agentGpaBoostRate = GameManager.Instance.currentGpaBoostRate;
        agentGpaWaitingPenaltyRate = GameManager.Instance.currentGpaWaitingPenaltyRate;
        agentGpaLeaveNoStudyPenalty = GameManager.Instance.currentGpaLeaveNoStudyPenalty;
    }

    void Update()
    {
        if (navAgent == null || !navAgent.isOnNavMesh) return;

        if (Time.timeScale <= 0)
        {
            if (navAgent.isOnNavMesh && !navAgent.isStopped) navAgent.isStopped = true;
            return;
        }

        if (navAgent.isOnNavMesh && navAgent.isStopped)
        {
            bool isNaturallyStationaryState = currentState == State.Studying || currentState == State.WaitingNearRoom || currentState == State.ReachedExit || currentState == State.Initializing;
            if (!isNaturallyStationaryState) navAgent.isStopped = false;
        }

        if (stateTimer > 0) stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.SeekingStudySpot: ExecuteSeek(); break;
            case State.MovingToStudySpot: ExecuteMoveToSpot(); break;
            case State.Studying: ExecuteStudy(); break;
            case State.WaitingNearRoom: ExecuteWait(); break;
            case State.MovingToExit: ExecuteMoveToExit(); break;
            case State.ReachedExit: Destroy(gameObject); break;
        }
    }

    void SetState(State next)
    {
        currentState = next;
        if (next != State.Studying && next != State.WaitingNearRoom) stateTimer = 0;

        if (navAgent == null) return;
        if (!navAgent.isOnNavMesh && next != State.ReachedExit && next != State.Initializing) return;

        if (navAgent.isOnNavMesh) navAgent.isStopped = false;

        switch (next)
        {
            case State.SeekingStudySpot:
                if (targetStudyRoom != null && assignedStudySpot != null) targetStudyRoom.VacateSpot(assignedStudySpot, this);
                assignedStudySpot = null; targetStudyRoom = null;
                moneySpentAtArcade = 0f;
                seekRetryTimer = 0;
                break;

            case State.MovingToStudySpot:
                if (assignedStudySpot != null)
                {
                    if (navAgent.isOnNavMesh)
                    {
                        if (!navAgent.SetDestination(assignedStudySpot.position))
                        {
                            SetState(State.SeekingStudySpot);
                        }
                    }
                    else { SetState(State.SeekingStudySpot); }
                }
                else { SetState(State.SeekingStudySpot); }
                break;

            case State.Studying:
                if (!navAgent.isOnNavMesh) { SetState(State.SeekingStudySpot); return; }
                navAgent.isStopped = true;
                if (navAgent.hasPath) navAgent.ResetPath();

                if (assignedStudySpot != null) navAgent.Warp(assignedStudySpot.position);
                else { SetState(State.SeekingStudySpot); return; }

                stateTimer = agentMaxStudyTime;
                navAgent.isStopped = true;
                break;

            case State.WaitingNearRoom:
                if (!navAgent.isOnNavMesh) { SetState(State.SeekingStudySpot); return; }
                navAgent.isStopped = true;
                if (navAgent.hasPath) navAgent.ResetPath();
                stateTimer = agentMaxWaitTime;
                break;

            case State.MovingToExit:
                if (targetStudyRoom != null && assignedStudySpot != null) targetStudyRoom.VacateSpot(assignedStudySpot, this);
                assignedStudySpot = null; targetStudyRoom = null;
                currentExitPoint = taitoStationExit;
                if (currentExitPoint != null)
                {
                    if (navAgent.isOnNavMesh)
                    {
                        if (!navAgent.SetDestination(currentExitPoint.position))
                        {
                            SetState(State.ReachedExit);
                        }
                    }
                    else { SetState(State.ReachedExit); }
                }
                else { Debug.LogError($"Agent {gameObject.name}: Exit point NULL for MovingToExit. Forcing ReachedExit."); SetState(State.ReachedExit); }
                break;

            case State.ReachedExit:
                if (navAgent.isOnNavMesh) navAgent.isStopped = true;
                break;
        }
    }

    void ExecuteSeek()
    {
        seekRetryTimer -= Time.deltaTime;
        if (seekRetryTimer > 0) return;
        seekRetryTimer = SEEK_INTERVAL;

        StudyRoom room = null;
        if (GameManager.Instance != null)
        {
            var activeRooms = GameManager.Instance.GetActiveStudyRooms();
            if (activeRooms != null && activeRooms.Any())
            {
                room = activeRooms.Where(r => r != null && r.gameObject.activeInHierarchy && r.HasAvailableSpot())
                                 .OrderBy(r => Vector3.Distance(transform.position, r.transform.position))
                                 .FirstOrDefault();
            }
        }

        if (room != null)
        {
            if (room.TryAssignSpot(this, out Transform spot))
            {
                assignedStudySpot = spot; targetStudyRoom = room;
                SetState(State.MovingToStudySpot);
            }
            else { if (currentState == State.SeekingStudySpot) SetState(State.WaitingNearRoom); }
        }
        else { if (currentState == State.SeekingStudySpot) SetState(State.WaitingNearRoom); }
    }

    void ExecuteMoveToSpot()
    {
        if (assignedStudySpot == null || targetStudyRoom == null || !targetStudyRoom.gameObject.activeInHierarchy)
        {
            SetState(State.SeekingStudySpot); return;
        }
        if (!navAgent.pathPending && navAgent.remainingDistance < 0.75f && navAgent.hasPath) SetState(State.Studying);
        else if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (targetStudyRoom != null && assignedStudySpot != null) targetStudyRoom.VacateSpot(assignedStudySpot, this);
            assignedStudySpot = null; targetStudyRoom = null;
            SetState(State.SeekingStudySpot);
        }
    }

    void ExecuteStudy()
    {
        agentGPA.currentGPA = Mathf.Clamp(agentGPA.currentGPA + (agentGpaBoostRate * Time.deltaTime), AgentGPA.MIN_GPA, AgentGPA.MAX_GPA);
        if (stateTimer <= 0)
        {
            GameManager.Instance?.RecordAgentOutcome(agentGPA.GetCurrentGPA(), true, 0f);
            SetState(State.MovingToExit);
        }
    }

    void ExecuteWait()
    {
        agentGPA.currentGPA = Mathf.Clamp(agentGPA.currentGPA - (agentGpaWaitingPenaltyRate * Time.deltaTime), AgentGPA.MIN_GPA, AgentGPA.MAX_GPA);
        if (stateTimer <= 0)
        {
            agentGPA.currentGPA = Mathf.Clamp(agentGPA.currentGPA - agentGpaLeaveNoStudyPenalty, AgentGPA.MIN_GPA, AgentGPA.MAX_GPA);
            if (GameManager.Instance != null)
            {
                moneySpentAtArcade = Random.Range(GameManager.Instance.minArcadeMoneySpent, GameManager.Instance.maxArcadeMoneySpent);
                GameManager.Instance.RecordAgentOutcome(agentGPA.GetCurrentGPA(), false, moneySpentAtArcade);
            }
            else
            {
                moneySpentAtArcade = Random.Range(500f, 5000f);
            }
            SetState(State.MovingToExit); return;
        }
        seekRetryTimer -= Time.deltaTime;
        if (seekRetryTimer <= 0) { ExecuteSeek(); seekRetryTimer = SEEK_INTERVAL * 1.5f; }
    }

    void ExecuteMoveToExit()
    {
        if (currentExitPoint == null) { SetState(State.ReachedExit); return; }
        if ((navAgent.hasPath && !navAgent.pathPending && navAgent.remainingDistance < 1.0f) ||
            (!navAgent.hasPath && Vector3.Distance(transform.position, currentExitPoint.position) < 1.0f))
        {
            SetState(State.ReachedExit);
        }
        else if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetState(State.ReachedExit);
        }
    }
}