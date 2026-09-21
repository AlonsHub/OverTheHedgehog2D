using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float fadeDuration = 1f;

    bool _dying = false;

    void Awake()
    {
        enemies.Add(this);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Hog"))
        {
            Die();
        }
    }

    private void Die()
    {
        //a second hit mid-fade shouldn't restart the fade or count us out twice
        if (_dying) return;
        _dying = true;

        //play impact anim
        spriteRenderer.DOFade(0f, fadeDuration)
            .SetLink(gameObject) //killed if we get destroyed early
            .OnComplete(() =>
            {
                enemies.Remove(this);

                if(enemies.Count == 0)
                {
                    //win game
                }
                //destroy self after anim
                Destroy(gameObject);
            });
    }
}
