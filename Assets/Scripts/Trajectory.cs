using UnityEngine;

//the aim preview: a row of pebble dots along the predicted arc. the dot texture is tiled along a
//LineRenderer by world distance, so dots stay evenly spaced however the arc bends, scroll toward the
//landing point, and fade out over the last stretch so the preview hints rather than spoils the shot
[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Look")]
    [Tooltip("Dot diameter in world units. Dots are spaced two diameters apart (the tile is 50% dot).")]
    [SerializeField] private float dotSize = 0.26f;

    [Header("Length")]
    [Tooltip("How far along the arc the preview reaches, in world units.")]
    public float visibleLength = 7f;
    [Tooltip("The last this many units of the preview fade to nothing; everything before is fully opaque.")]
    public float fadeLength = 3f;

    [Header("Motion")]
    [Tooltip("How fast the dots travel along the arc, in world units per second (negative to reverse).")]
    public float scrollSpeed = 1.5f;

    //arc sampling: fine enough that a tight lob still looks smooth
    const float TimeStep = 0.02f;
    const int MaxPoints = 400;

    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    readonly Vector3[] _points = new Vector3[MaxPoints];
    Material _material;
    float _scroll;

    void Awake()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        //own copy so scrolling the offset doesn't dirty the shared asset
        _material = lineRenderer.material;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.useWorldSpace = true;
        ApplyLook();
    }

    void OnValidate()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        visibleLength = Mathf.Max(0.1f, visibleLength);
        fadeLength = Mathf.Clamp(fadeLength, 0f, visibleLength);
        dotSize = Mathf.Max(0.02f, dotSize);
        if (lineRenderer != null) ApplyLook();
    }

    //width and tiling go together: one square tile per (2 * dotSize) units so the dot isn't stretched
    void ApplyLook()
    {
        float tile = dotSize * 2f;
        lineRenderer.widthMultiplier = tile;
        lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        lineRenderer.textureScale = new Vector2(1f / tile, 1f);

        //opaque until the fade section, then out. keys are in normalised line length
        float fadeStart = visibleLength <= 0f ? 1f : 1f - Mathf.Clamp01(fadeLength / visibleLength);
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeStart), new GradientAlphaKey(0f, 1f) });
        lineRenderer.colorGradient = g;
    }

    void Update()
    {
        if (_material == null) return;
        //tiles are 1/textureScale.x units long, so one unit of world travel is textureScale.x in UV.
        //offset decreases so the pattern moves toward the end of the line
        _scroll -= scrollSpeed * Time.deltaTime * lineRenderer.textureScale.x;
        _scroll = Mathf.Repeat(_scroll, 1f);
        _material.SetTextureOffset(MainTex, new Vector2(_scroll, 0f));
    }

    //walk the ballistic arc until visibleLength of it has been laid down
    public void DrawTrajectory(Vector2 startPosition, Vector2 startVelocity)
    {
        int count = 0;
        float laid = 0f;
        Vector2 prev = startPosition;
        _points[count++] = startPosition;
        for (int i = 1; i < MaxPoints; i++)
        {
            float t = i * TimeStep;
            Vector2 p = startPosition + startVelocity * t + 0.5f * Physics2D.gravity * t * t;
            float seg = Vector2.Distance(prev, p);
            if (laid + seg >= visibleLength)
            {
                //stop exactly at the visible length so the fade always sits in the same place
                _points[count++] = Vector2.Lerp(prev, p, (visibleLength - laid) / Mathf.Max(seg, 0.0001f));
                break;
            }
            laid += seg;
            prev = p;
            _points[count++] = p;
        }
        lineRenderer.positionCount = count;
        lineRenderer.SetPositions(_points);
    }

    public void Hide()
    {
        lineRenderer.positionCount = 0;
        gameObject.SetActive(false);
    }
}
