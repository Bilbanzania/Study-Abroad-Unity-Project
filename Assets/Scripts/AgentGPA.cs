using UnityEngine;
public class AgentGPA : MonoBehaviour
{
    public float currentGPA = 2.5f;
    public const float MAX_GPA = 4.0f; public const float MIN_GPA = 0.0f;
    public float GetCurrentGPA() { return currentGPA; }
}