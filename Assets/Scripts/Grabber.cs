using UnityEngine;

public class Grabber : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;
    [SerializeField] private Thrower thrower;
    [SerializeField] private Trajectory trajectory;

    public bool isGrabbing;

    //the band creaks as it's pulled further, one creak per this much extra stretch
    [SerializeField] private float creakEvery = 0.6f;
    float _creakedAt;

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
            Sfx.Play("grab");
            _creakedAt = 0f;
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

        trajectory.DrawTrajectory(thrower.anchor.position, (thrower.anchor.position - transform.position) * thrower.throwForce);

        float stretch = Vector3.Distance(thrower.anchor.position, transform.position);
        if (stretch > _creakedAt + creakEvery)
        {
            _creakedAt = stretch;
            //higher and tighter the further it's pulled
            Sfx.Play("stretch", 0.8f, 0.9f + 0.08f * stretch);
        }
    }

    private void OnMouseUp()
    {
        //Throw!
        thrower.Throw();
        isGrabbing = false;
        trajectory.Hide();
    }
}
