using UnityEngine;

//turns with the wind. Rotating a flat sprite around Y in front of a perspective camera
//foreshortens it, which reads as the vane swinging round on its post
public class WeatherVane : MonoBehaviour
{
    [Tooltip("Where it points in a steady breeze, degrees around Y")]
    [SerializeField] private float restAngle = 15f;
    [Tooltip("How far the gusts swing it either side of rest")]
    [SerializeField] private float swing = 35f;
    [SerializeField] private float windPhase = 0.5f;

    void Update()
    {
        float wind = Wind.Strength(windPhase) * 2f - 1f; //-1..1
        transform.localRotation = Quaternion.Euler(0f, restAngle + wind * swing, 0f);
    }
}
