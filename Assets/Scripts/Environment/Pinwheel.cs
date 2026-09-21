using UnityEngine;

//spins with the wind: a lazy idle turn that picks up in the gusts
public class Pinwheel : MonoBehaviour
{
    [Tooltip("Degrees/sec with no wind at all")]
    [SerializeField] private float idleSpeed = 40f;
    [Tooltip("Extra degrees/sec at full gust")]
    [SerializeField] private float gustSpeed = 200f;
    [SerializeField] private float windPhase = 0f;

    void Update()
    {
        float speed = idleSpeed + gustSpeed * Wind.Strength(windPhase);
        transform.Rotate(0f, 0f, -speed * Time.deltaTime);
    }
}
