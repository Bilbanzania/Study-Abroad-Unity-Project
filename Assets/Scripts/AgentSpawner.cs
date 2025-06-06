using UnityEngine;
public class AgentSpawner : MonoBehaviour
{
    public GameObject agentPrefab;

    public void SpawnAgents(int count)
    {
        if (agentPrefab == null)
        {
            Debug.LogError($"CRITICAL: AgentPrefab not assigned on Spawner '{gameObject.name}'! Cannot spawn agents.", gameObject);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            Instantiate(agentPrefab, transform.position, transform.rotation);
        }
    }
}