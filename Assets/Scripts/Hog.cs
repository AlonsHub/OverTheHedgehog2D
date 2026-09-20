using System.Collections;
using UnityEngine;

public class Hog : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float blastToScreenSpeed;
    [SerializeField] private float blastUpSpeed = 2f;
    [SerializeField] private float ttl = 5f;

    [SerializeField] private Collider2D col;

    //state?
    bool _impacted = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(_impacted) return;
        
        _impacted = true;
            Impact();
            // col.enabled = false;

            Destroy(gameObject, ttl);
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

        StartCoroutine(ToScreenCoroutine());


        // Camera camera = Camera.main;

        // Vector2 dir = (camera.transform.position - transform.position).normalized * blastToScreenSpeed;
        // // StartCoroutine(ToScreenCoroutine());
        // rb.AddForce(dir + Vector2.up * blastUpSpeed, ForceMode2D.Impulse);
    }

    // protected IEnumerator WalkCoroutine()
    // {
    //     while (true)
    //     {
    //         transform.position += Vector3.right * walkSpeed * Time.deltaTime;
    //         yield return null;
    //     }
    // }
    protected IEnumerator ToScreenCoroutine()
    {
        Camera camera = Camera.main;
        rb.simulated = true;
        col.enabled = false;

        Vector3 dir = (camera.transform.position - transform.position).normalized;
        dir += Random.Range(-0.3f, 0.3f) * Vector3.right;

        float goUpFactor = blastUpSpeed;


        while (gameObject != null)
        {
            transform.localScale += Vector3.one * blastToScreenSpeed;
            transform.position += dir * blastToScreenSpeed * Time.deltaTime;
            transform.position += Vector3.up * goUpFactor * Time.deltaTime;

            // goUpFactor -= Time.deltaTime * Physics2D.gravity.y;
            goUpFactor += Time.deltaTime * Physics2D.gravity.y; //gravity is neg
            yield return null;
        }
    }
    

}
