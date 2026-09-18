using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static List<Enemy> enemies = new List<Enemy>(); 
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Awake()
    {
        enemies.Add(this);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Hog"))
        {
            //play impact anim
            //destroy self after anim
            // Destroy(gameObject);
            StartCoroutine(Die());
        }
    }

    private System.Collections.IEnumerator Die()
    {
        //play impact anim
        // yield return new WaitForSeconds(impactAnimDuration);
        float t = 0f;   
        Color colour = spriteRenderer.color;

        while(t <= 1f)
        {
            colour.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = colour;
            //play impact anim
            yield return null;
            t += Time.deltaTime;
        }
            
        enemies.Remove(this);

        if(enemies.Count == 0)
        {
            //win game
        }
        //destroy self after anim
        Destroy(gameObject);
    }
}
