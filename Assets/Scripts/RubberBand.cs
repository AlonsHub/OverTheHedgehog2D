using UnityEngine;

//draws the sling band. with the fork tips and cup loops assigned it runs
//left tip -> left loop -> right loop -> right tip; without them it's the old two-point line
public class RubberBand : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform grabberTrans;
    [SerializeField] private Transform throwerTrans;

    [Header("Slingshot rig (optional)")]
    [SerializeField] private Transform tipLeft;
    [SerializeField] private Transform tipRight;
    [SerializeField] private Transform cupLeft;
    [SerializeField] private Transform cupRight;

    bool Rigged => tipLeft != null && tipRight != null && cupLeft != null && cupRight != null;

    private void Update()
    {
        if (Rigged)
        {
            if (lineRenderer.positionCount != 4) lineRenderer.positionCount = 4;
            lineRenderer.SetPosition(0, tipLeft.position);
            lineRenderer.SetPosition(1, cupLeft.position);
            lineRenderer.SetPosition(2, cupRight.position);
            lineRenderer.SetPosition(3, tipRight.position);
            return;
        }

        if (lineRenderer.positionCount != 2) lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, grabberTrans.position);
        lineRenderer.SetPosition(1, throwerTrans.position);
    }
}
