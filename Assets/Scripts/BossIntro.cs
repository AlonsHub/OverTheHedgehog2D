using System.Collections;
using DG.Tweening;
using UnityEngine;

//boss levels open on the boss: the camera starts tight on it, holds a beat while it struts, then eases
//back out and over to the normal framing. the slingshot is locked until the camera is home
public class BossIntro : MonoBehaviour
{
    [SerializeField] private Grabber grabber;
    [Tooltip("Camera size while framing the boss (normal play is 5)")]
    [SerializeField] private float closeSize = 2.6f;
    [SerializeField] private float holdSeconds = 1.4f;
    [SerializeField] private float pullBackSeconds = 1.6f;
    [SerializeField] private string stingSound = "boss_hit";

    IEnumerator Start()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Level == null || !gm.Level.isBoss) yield break;
        var cam = Camera.main;
        if (cam == null) yield break;

        //the boss is the toughest hen on the field
        Enemy boss = null;
        foreach (var e in Enemy.enemies) if (boss == null || e.HitPoints > boss.HitPoints) boss = e;
        if (boss == null) yield break;

        Vector3 home = cam.transform.position;
        float homeSize = cam.orthographicSize;
        if (grabber != null) grabber.locked = true;

        cam.orthographicSize = closeSize;
        cam.transform.position = new Vector3(boss.transform.position.x, boss.transform.position.y + 0.4f, home.z);
        yield return null;
        Sfx.Play(stingSound);
        yield return new WaitForSeconds(holdSeconds);

        var seq = DOTween.Sequence().SetLink(gameObject);
        seq.Join(cam.transform.DOMove(home, pullBackSeconds).SetEase(Ease.InOutCubic));
        seq.Join(cam.DOOrthoSize(homeSize, pullBackSeconds).SetEase(Ease.InOutCubic));
        yield return seq.WaitForCompletion();

        cam.transform.position = home;
        cam.orthographicSize = homeSize;
        if (grabber != null) grabber.locked = false;
    }
}
