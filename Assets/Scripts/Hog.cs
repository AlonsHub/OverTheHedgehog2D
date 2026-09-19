using UnityEngine;

public class Hog : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private float walkSpeed;
    [SerializeField] private Collider2D col;

    //state?

    void OnCollisionEnter2D(Collision2D collision)
    {
            Impact();
            col.enabled = false;

            Destroy(gameObject, 2f);
    }

    public void Walk()
    {
        //moves one step forward
    }
    public void Fly()
    {
        anim.SetTrigger("Fly");
    }
    public virtual void Impact()
    {
        Debug.Log("Impact");
        anim.SetTrigger("Impact");

        //start fading away now or after a delay?
    }
    

}
