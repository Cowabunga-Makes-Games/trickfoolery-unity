using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum GameState {
    Tutorial,
    OutOfCombat,
    Combat
}

public class GameManager : MonoBehaviour
{
    // state variables
    public GameState state = GameState.OutOfCombat;
    public bool isPaused = true;
    public bool showPauseMenu = false;
    private bool isGameWon = false;
    private bool inCombat = false;
    public bool playerInput = false;
    public bool isGameOver = false;

    // wwise event variable declaration
    public AK.Wwise.Event pauseSFX;
    public AK.Wwise.Event unpSFX;
    public AK.Wwise.Event playpauseMUS;
    public AK.Wwise.Event mutepauseMUS;
    public AK.Wwise.Event firstmutepauseMUS;
    public AK.Wwise.Event unmpauseMUS;
    public AK.Wwise.Event playAaaMus;
    public AK.Wwise.Event muteAaaMus;
    public AK.Wwise.Event unmAaaMus;
    public AK.Wwise.Event stopAaaMus;
    public AK.Wwise.Event stoppauseMUS;

    // object references
    public UIManager uiManager;
    private DialogueManager dialogueManager;
    private JumbotronController jumbotron;
    private PlayerMovement player;

    // for respawn
    public GameObject[] enemies;
    public Collider spawnRange;

    // parameters
    [SerializeField]
    private int minEnemyNumber;
    [SerializeField]
    private string nextScene;
    [SerializeField]
    UnityEvent preCombat;
    [SerializeField]
    public GameObject trackerPrefab;

    [SerializeField] private bool TauntEnemyInLevel;

    // events for listeners
    [SerializeField]
    public UnityEvent startCombatEvent;
    [SerializeField]
    public UnityEvent startTutorialEvent;
    [SerializeField]
    public UnityEvent stopCombatEvent;

    private bool isRestart;

    [SerializeField]
    private GameObject progressTrackerObj;

    void Start()
    {
        uiManager = gameObject.GetComponent<UIManager>();
        dialogueManager = gameObject.GetComponent<DialogueManager>();
        jumbotron = GameObject.Find("Jumbotron").GetComponent<JumbotronController>();

        GameObject progressTracker = GameObject.Find("ProgressTracker");
        if (progressTracker) {
            isRestart = progressTracker.GetComponent<ProgressTracker>().isRestart;
        }
        else {
            GameObject trackerObj = GameObject.Instantiate(progressTrackerObj);
            trackerObj.name = "ProgressTracker";
            isRestart = false;
        }

        playpauseMUS.Post(gameObject);
        firstmutepauseMUS.Post(gameObject);
        AkSoundEngine.SetState("Gameplay_State", "Combat");
        playAaaMus.Post(gameObject);
        StartCoroutine(StartLevel());
    }

    void Awake() {
        GameObject.Find("FadeInOut").GetComponent<Animator>().SetTrigger("FadeIn");
    }

    IEnumerator StartLevel() {
        yield return new WaitForSecondsRealtime(1f); // wait for fade-in animation
        bool hasPersistentTarget = false;
        for (int i = 0; i < preCombat.GetPersistentEventCount(); i++) {
            if (preCombat.GetPersistentTarget(i) != null)
                hasPersistentTarget = true;
                break;
        }

        if (hasPersistentTarget && !isRestart) {
            preCombat.Invoke(); // execute pre-combat code
        }
        else {
            // TODO: make this.. better
            GameObject dialogueFade = GameObject.Find("DialogueFadeIn");
            if (dialogueFade) {
                dialogueFade.SetActive(false);
            }
            StartCoroutine(uiManager.StartCombat()); // starts combat
        }
    }

    public void StartCombat() {
        print("starting combat");
        Time.timeScale = 1;
        state = GameState.Combat;
        playerInput = true;
    }

    public void StartTutorial() {
        state = GameState.Tutorial;
        playerInput = true;
    }

    public void StopCombat() {
        state = GameState.OutOfCombat;
        playerInput = false;
    }

    public void OnPause() {
        if (jumbotron.state != JumbotronState.Disabled && SceneManager.GetActiveScene().name != "Lvl1Lukey")
        {
            TogglePause();
        }
    }

    void Update()
    {
        if (state == GameState.Combat && GameObject.FindGameObjectsWithTag("Enemy").Length < minEnemyNumber)
        {
            SpawnRandomEnemy();
        }     
        if (Input.GetKeyDown(KeyCode.R)) {
            stopAaaMus.Post(gameObject);
            stoppauseMUS.Post(gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    IEnumerator LoadNextScene() {
        GameObject.Find("FadeInOut").GetComponent<Animator>().SetTrigger("FadeOut");
        yield return new WaitForSecondsRealtime(1.5f);
        if (isGameWon) {
            GameObject.Find("ProgressTracker").GetComponent<ProgressTracker>().isRestart = false;
            stopAaaMus.Post(gameObject);
            stoppauseMUS.Post(gameObject);
            SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
        }
        else {
            GameObject.Find("ProgressTracker").GetComponent<ProgressTracker>().isRestart = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void TriggerLoadNextScene() {
        StartCoroutine(LoadNextScene());
    }

    public void TogglePause() {
        if (jumbotron.state == JumbotronState.Hidden || jumbotron.state == JumbotronState.HypeBar) {
            pauseSFX.Post(gameObject);
            unmpauseMUS.Post(gameObject);
            muteAaaMus.Post(gameObject);
            uiManager.ShowPauseMenu();
            isPaused = true;
            Time.timeScale = 0;
        }
        else if (jumbotron.state == JumbotronState.Pause || jumbotron.state == JumbotronState.PauseFromHidden) {
            unpSFX.Post(gameObject);
            mutepauseMUS.Post(gameObject);
            unmAaaMus.Post(gameObject);
            uiManager.HidePauseMenu();
            Time.timeScale = 1;
        }
        else {
            return;
        }

        jumbotron.state = JumbotronState.Disabled;  // for animation duration
    }

    public IEnumerator GameOverWin()
    {
        print("gameOverWin");
        isGameOver = true;
        isGameWon = true;
        AkSoundEngine.SetState("Gameplay_State", "Victory");
        yield return new WaitForSeconds(0.5f);
        stopCombatEvent.Invoke();
        uiManager.GameOverWin();
        yield return new WaitForSeconds(1f);
        dialogueManager.StartDialogueScene("onWin", TriggerLoadNextScene);
    }

    public IEnumerator GameOverLose()
    {
        isGameOver = true;
        stopAaaMus.Post(gameObject);
        stopCombatEvent.Invoke();
        yield return new WaitForSeconds(3f);
        uiManager.GameOverLose();
        yield return new WaitForSeconds(1f);
        dialogueManager.StartDialogueScene("onLoss", ShowLoseMenu);
    }

    void ShowLoseMenu() {
        print("show lose menu");
        jumbotron.state = JumbotronState.Disabled;
        stopAaaMus.Post(gameObject);
        uiManager.ShowLoseMenu();
        isPaused = true;
        Time.timeScale = 0;
    }

    void SpawnRandomEnemy()
    {
        Vector3 newPosition = Vector3.zero;
        Collider[] colliders = spawnRange.GetComponentsInChildren<Collider>();

        if (colliders.Length > 0 ){
            Collider collider = colliders[Random.Range(0,colliders.Length)];
            Vector3 center = collider.bounds.center;
            Vector3 extents = collider.bounds.extents;

            newPosition = new Vector3(
                Random.Range(center.x - extents.x, center.x + extents.x),
                center.y,
                Random.Range(center.z - extents.z, center.z + extents.z)
            );
        }

        Quaternion newRotation = Random.rotation;
        newRotation.w = 0;
        newRotation.x = 0;
        newRotation.z = 0;
        GameObject.Instantiate(enemies[Random.Range(0, enemies.Length)], newPosition, newRotation);
    }

    Vector3 RandonPosInCollider (Collider[] colliders){
        Vector3 boundsCenter = Vector3.zero;
        Vector3 boundsExtents = Vector3.zero;

        foreach (var collider in colliders)
        {
        boundsCenter += collider.bounds.center;    
        boundsExtents += collider.bounds.extents;    
        }

        boundsCenter /= colliders.Length;
        boundsExtents /= colliders.Length;

        Vector3 randomPoint = new Vector3(
            Random.Range(boundsCenter.x - boundsExtents.x, boundsCenter.x + boundsExtents.x),
            boundsCenter.y,
            Random.Range(boundsCenter.z - boundsExtents.z, boundsCenter.z + boundsExtents.z)
        );

        return randomPoint;
    }
}

