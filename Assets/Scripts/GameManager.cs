using System;
using UnityEngine;

//sets the level up, keeps score, and calls the win or the loss.
//runs its Awake before everything else so the HogStock finds its loadout already set
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private HogStock stock;
    [SerializeField] private Thrower thrower;
    [SerializeField] private ResultWindow resultWindow;
    [Tooltip("Where the level's block set goes if the level doesn't say")]
    [SerializeField] private Transform blockSetAnchor;
    [Tooltip("Used when the scene is played straight from the editor with no level picked")]
    [SerializeField] private LevelDefinition fallbackLevel;

    [Header("Scoring")]
    [SerializeField] private int bonusPerSparedHog = 500;

    [Header("Timing")]
    [Tooltip("Breathing room after the last hen pops before the win window")]
    [SerializeField] private float winDelay = 1.5f;
    [Tooltip("How long we wait for the dust to settle after the last hog is gone before calling it")]
    [SerializeField] private float loseDelay = 3f;

    public int Score { get; private set; }
    public LevelDefinition Level { get; private set; }
    public int LevelIndex { get; private set; }
    public bool IsOver { get; private set; }
    public int HensAtStart => _hensAtStart;

    public event Action<int> ScoreChanged;

    float _endTimer = -1f;
    bool _endingWithWin;
    int _hensAtStart;

    void Awake()
    {
        Instance = this;

        //stale statics from the previous level
        Enemy.enemies.Clear();
        Hog.airborne.Clear();

        SetupLevel();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void SetupLevel()
    {
        Level = LevelProgress.Current;
        LevelIndex = LevelProgress.CurrentIndex;
        if (Level == null)
        {
            Level = fallbackLevel;
            LevelIndex = LevelCatalogue.Load()?.IndexOf(Level) ?? 0;
        }
        if (Level == null)
        {
            Debug.LogError("GameManager: no level to play and no fallback set", this);
            return;
        }

        if (Level.blockSet != null)
        {
            Vector3 at = blockSetAnchor != null ? blockSetAnchor.position + Level.blockSetPosition : Level.blockSetPosition;
            GameObject set = Instantiate(Level.blockSet, at, Quaternion.identity);
            set.name = Level.blockSet.name;
        }

        if (Level.loadout != null && stock != null)
            stock.SetLoadout(Level.loadout);
    }

    void Start()
    {
        //the garden theme carries in from the map; boss levels get their own band
        if (Level != null) Music.Play(Level.isBoss ? "music_boss" : "music_garden", 1.2f, Level.isBoss ? 0.55f : 0.5f);
        _hensAtStart = Enemy.enemies.Count;
        ScoreChanged?.Invoke(Score);
    }

    void Update()
    {
        if (IsOver || Level == null) return;

        //a countdown to the result screen, restarted whenever the situation changes
        if (_endTimer >= 0f)
        {
            _endTimer -= Time.deltaTime;
            if (_endTimer <= 0f)
            {
                if (_endingWithWin) Win();
                else Lose();
            }
            return;
        }

        if (_hensAtStart > 0 && Enemy.AliveCount == 0)
        {
            _endingWithWin = true;
            _endTimer = winDelay;
        }
        else if (OutOfHogs)
        {
            _endingWithWin = false;
            _endTimer = loseDelay;
        }
    }

    //nothing left to throw and nothing still flying
    bool OutOfHogs =>
        stock != null && stock.Count == 0 &&
        thrower != null && !thrower.IsLoaded && !thrower.IsLoading && !thrower.IsThrowing &&
        Hog.airborne.Count == 0;

    public void AddScore(int amount)
    {
        if (IsOver) return;
        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    public void OnEnemyDied(Enemy enemy)
    {
        //a hen popping while the lose countdown runs (tumbling tower) flips it into a win check
        if (!IsOver && !_endingWithWin && _endTimer >= 0f && Enemy.AliveCount == 0)
        {
            _endingWithWin = true;
            _endTimer = winDelay;
        }
    }

    void Win()
    {
        IsOver = true;

        int spared = stock.Count + (thrower.IsLoaded || thrower.IsLoading ? 1 : 0);
        int bonus = spared * bonusPerSparedHog;
        Score += bonus;
        ScoreChanged?.Invoke(Score);

        bool record = LevelProgress.ReportScore(LevelIndex, Score);
        LevelProgress.ReportStars(LevelIndex, LevelProgress.StarsFor(Score, HensAtStart));
        LevelProgress.Unlock(LevelIndex + 1);

        resultWindow?.Show(true, Score, LevelProgress.BestScore(LevelIndex), record, bonus, LevelProgress.HasNext);
    }

    void Lose()
    {
        IsOver = true;
        resultWindow?.Show(false, Score, LevelProgress.BestScore(LevelIndex), false, 0, false);
    }
}
