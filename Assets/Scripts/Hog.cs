using UnityEngine;

public class Hog : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private float walkSpeed;

    //state?

    void OnCollisionEnter2D(Collision2D collision)
    {
            Impact();
    }

    public void Walk()
    {
        //moves one step forward
    }
    public void Fly()
    {
        anim.SetTrigger("Fly");
    }
    public void Impact()
    {
        anim.SetTrigger("Impact");

        //start fading away now or after a delay?
    }
    

}
