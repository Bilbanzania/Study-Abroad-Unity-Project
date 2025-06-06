using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StudyRoom : MonoBehaviour
{
    public List<Transform> studySpotsInRoom;
    private Dictionary<Transform, AgentAI> occupiedSpotsMap = new Dictionary<Transform, AgentAI>();

    void Awake()
    {
        occupiedSpotsMap = new Dictionary<Transform, AgentAI>();
        if (studySpotsInRoom == null)
        {
            studySpotsInRoom = new List<Transform>();
        }
        studySpotsInRoom = studySpotsInRoom.Where(spot => spot != null).ToList();
    }

    public bool HasAvailableSpot()
    {
        if (studySpotsInRoom == null) return false;
        return occupiedSpotsMap.Count < studySpotsInRoom.Count;
    }

    public bool TryAssignSpot(AgentAI agent, out Transform assignedSpot)
    {
        assignedSpot = null;
        if (studySpotsInRoom == null || agent == null || !HasAvailableSpot())
        {
            return false;
        }

        foreach (Transform spot in studySpotsInRoom)
        {
            if (spot == null) continue;

            if (!occupiedSpotsMap.ContainsKey(spot))
            {
                assignedSpot = spot;
                occupiedSpotsMap[spot] = agent;
                return true;
            }
        }
        return false;
    }

    public void VacateSpot(Transform spotToVacate, AgentAI agent)
    {
        if (agent == null || spotToVacate == null) return;

        if (occupiedSpotsMap.TryGetValue(spotToVacate, out AgentAI occupyingAgent))
        {
            if (occupyingAgent == agent)
            {
                occupiedSpotsMap.Remove(spotToVacate);
            }
        }
    }

    public void SetMaxStudyTime(float newMaxTime)
    {
        // Functionality related to a 'maxStudyTime' variable would go here if used.
        // Currently, agent study time is primarily driven by GameManager settings via AgentAI.
    }
}