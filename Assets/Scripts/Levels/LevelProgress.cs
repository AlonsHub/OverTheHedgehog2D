using UnityEngine;
using UnityEngine.SceneManagement;

//which level is being played right now, which ones are unlocked, and the best score per level.
//all PlayerPrefs, nothing fancy
public static class LevelProgress
{
    public const string GameSceneName = "GameScene";
    public const string MapSceneName = "LevelMap";
    public const string StartSceneName = "StartMenu";

    const string UnlockedKey = "levels.unlocked";
    const string BestKeyPrefix = "levels.best.";

    //index into the LevelCatalogue of the level the GameScene should set up
    public static int CurrentIndex { get; private set; } = 0;

    public static LevelDefinition Current => LevelCatalogue.Load()?.Get(CurrentIndex);

    //highest level index the player is allowed to start (0 = only the first)
    public static int UnlockedIndex
    {
        get => PlayerPrefs.GetInt(UnlockedKey, 0);
        private set { PlayerPrefs.SetInt(UnlockedKey, value); PlayerPrefs.Save(); }
    }

    //tutorials are always open and never gate what comes after them
    public static bool IsUnlocked(int index)
    {
        var catalogue = LevelCatalogue.Load();
        var level = catalogue?.Get(index);
        if (level != null && level.isTutorial) return true;
        return index <= Mathf.Max(UnlockedIndex, FirstRealLevel);
    }

    //index of the first level that isn't a tutorial
    public static int FirstRealLevel
    {
        get
        {
            var catalogue = LevelCatalogue.Load();
            if (catalogue == null) return 0;
            for (int i = 0; i < catalogue.Count; i++)
                if (!catalogue.Get(i).isTutorial) return i;
            return 0;
        }
    }

    public static int BestScore(int index) => PlayerPrefs.GetInt(BestKeyPrefix + index, 0);
    public static bool IsCompleted(int index) => BestScore(index) > 0;

    //returns true if this was a new record
    public static bool ReportScore(int index, int score)
    {
        bool record = score > BestScore(index);
        if (record)
        {
            PlayerPrefs.SetInt(BestKeyPrefix + index, score);
            PlayerPrefs.Save();
        }
        return record;
    }

    public static void Unlock(int index)
    {
        if (index > UnlockedIndex)
            UnlockedIndex = index;
    }

    public static void Play(int index)
    {
        var catalogue = LevelCatalogue.Load();
        if (catalogue == null || catalogue.Count == 0) return;

        CurrentIndex = Mathf.Clamp(index, 0, catalogue.Count - 1);
        SceneManager.LoadScene(GameSceneName);
    }

    public static bool HasNext => LevelCatalogue.Load() != null && CurrentIndex + 1 < LevelCatalogue.Load().Count;

    public static void Replay() => Play(CurrentIndex);
    public static void PlayNext() => Play(CurrentIndex + 1);
    public static void OpenMap() => SceneManager.LoadScene(MapSceneName);
    public static void OpenStart() => SceneManager.LoadScene(StartSceneName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetOnPlay()
    {
        //domain reload may be off in the editor, don't carry a stale pick between play sessions
        CurrentIndex = 0;
    }
}
