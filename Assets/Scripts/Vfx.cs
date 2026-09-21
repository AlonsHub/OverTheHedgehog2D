using UnityEngine;

//fire-and-forget particle spawns. every VFX prefab is a ParticleSystem whose main.stopAction is Destroy,
//this just guards against prefabs that forgot and cleans them up itself
public static class Vfx
{
    public static GameObject Spawn(GameObject prefab, Vector3 position, float scale = 1f)
    {
        if (prefab == null) return null;

        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        go.transform.localScale *= scale;

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null || ps.main.stopAction != ParticleSystemStopAction.Destroy)
        {
            float life = ps == null ? 2f : ps.main.duration + ps.main.startLifetime.constantMax;
            Object.Destroy(go, life);
        }
        return go;
    }
}
