using UnityEngine;

public class Grabber : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;
    [SerializeField] private Thrower thrower;
    [SerializeField] private Trajectory trajectory;

    public bool isGrabbing;

    private void Awake()
    {
        if(m_Camera == null)
        {
            m_Camera = Camera.main;
        }
    }

    private void OnMouseDown()
    {
        if (thrower.IsLoaded)
        {
            isGrabbing = true;
            trajectory.gameObject.SetActive(true);
        }
    }
    private void OnMouseDrag()
    {
        //limit from thrower

        Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 point = hit.point;
            point.z = 0;
            transform.position = point;
        }

        trajectory.DrawTrajectory(thrower.anchor.position, (thrower.anchor.position - transform.position) * thrower.throwForce, .1f, 30
        );   
    }

    private void OnMouseUp()
    {
        //Throw!
        thrower.Throw();
        isGrabbing = false;
    }
}