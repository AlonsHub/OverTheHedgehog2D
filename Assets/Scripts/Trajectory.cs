using UnityEngine;

public class Trajectory : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    public void DrawTrajectory(Vector2 startPosition, Vector2 startVelocity, float timeStep, int maxSteps, float mass = 1f)
    {
        Vector3[] points = new Vector3[maxSteps];
        lineRenderer.positionCount = maxSteps;  
        for (int i = 0; i < maxSteps; i++)
        {
            float t = i * timeStep;
            points[i] = startPosition + (startVelocity * t + (0.5f * Physics2D.gravity * t * t))/mass;
        }

        lineRenderer.SetPositions(points);
    }
}
