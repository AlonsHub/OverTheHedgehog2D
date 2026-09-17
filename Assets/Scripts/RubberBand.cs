using UnityEngine;

public class RubberBand : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform grabberTrans;
    [SerializeField] private Transform throwerTrans;

    //private void FixedUpdate()
    private void Update()
    {
        //lineRenderer.SetPositions(new Vector3[] { })
        lineRenderer.SetPosition(0, grabberTrans.position);
        lineRenderer.SetPosition(1, throwerTrans.position);
    }
}
