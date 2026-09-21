using UnityEngine;

//one shared breeze so everything that reacts to wind gusts together
public static class Wind
{
    //how quickly gusts come and go
    public static float gustSpeed = 0.15f;

    //0..1, slowly wandering. Give things a different phase so they don't move in perfect lockstep
    public static float Strength(float phase = 0f)
    {
        return Mathf.PerlinNoise(Time.time * gustSpeed + phase, 0.37f);
    }
}
