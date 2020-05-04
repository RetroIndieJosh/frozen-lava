using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor( typeof( LavaPlayer ) )]
public class LavaPlayerScriptEditor : ScriptEditor
{
    public override void OnInspectorGUI() {
        DrawDefaultScriptEditor();

        var bestTime = PlayerPrefs.GetFloat( "best time", -1 );
        if ( bestTime > 0 ) {
            var minutes = bestTime / 60.0f;
            var seconds = Mathf.Floor( bestTime - minutes * 60.0f * 100.0f ) / 100.0f;
            GUILayout.Label( "Best Time: " + minutes + ":" + seconds );
        } else GUILayout.Label( "Best Time: (not set)" );

        GUILayout.Label( "Deaths: " + PlayerPrefs.GetInt( "deaths", 0 ) );
        GUILayout.Label( "Highest Round: " + PlayerPrefs.GetInt( "highest round", 1 ) );

        // round
        GUILayout.BeginHorizontal();
        {
            var spawnerIndex = PlayerPrefs.GetInt( "spawner index", 0 );
            var spawnManager = FindObjectOfType<PlayerSpawnManager>();
            var spawner = spawnManager[spawnerIndex];

            GUILayout.Label( string.Format( "Spawn index ({0}):", spawner == null ? "INVALID" : spawner.name ) );
            spawnerIndex = EditorGUILayout.DelayedIntField( spawnerIndex );
            PlayerPrefs.SetInt( "spawner index", spawnerIndex );
        }
        GUILayout.EndHorizontal();

        // round
        GUILayout.BeginHorizontal();
        {
            var round = PlayerPrefs.GetInt( "round", -1 );
            GUILayout.Label( "Round: " );
            round = EditorGUILayout.DelayedIntField( round );
            PlayerPrefs.SetInt( "round", round );
        }
        GUILayout.EndHorizontal();

        // lives
        GUILayout.BeginHorizontal();
        {
            var lives = PlayerPrefs.GetInt( "lives", -1 );
            GUILayout.Label( "Lives: " );
            lives = EditorGUILayout.DelayedIntField( lives );
            PlayerPrefs.SetInt( "lives", lives );
        }
        GUILayout.EndHorizontal();

        if ( GUILayout.Button( "Reset" ) ) {
            PlayerPrefs.SetInt( "lives", serializedObject.FindProperty( "m_playerLives" ).intValue );
            PlayerPrefs.SetInt( "spawner index", 0 );
            PlayerPrefs.SetInt( "deaths", 0 );
            PlayerPrefs.SetInt( "round", 1 );
            PlayerPrefs.SetInt( "best time", -1 );
            PlayerPrefs.SetInt( "highest round", 1 );
        }

        PlayerPrefs.Save();
    }
}

#endif // UNITY_EDITOR


public class LavaPlayer : MonoBehaviour
{
    [Header("Health/Lives")]

    [SerializeField]
    private int m_initialMaxHealth = 30;

    [SerializeField]
    private int m_playerLives = 3;

    [Header("Display")]

    [SerializeField]
    TextMeshPro m_bestTimeText = null;

    [SerializeField]
    TextMeshPro m_deathsText = null;

    [SerializeField]
    TextMeshPro m_livesText = null;

    [SerializeField]
    TextMeshPro m_timerText = null;

    [SerializeField]
    TextMeshPro m_roundText = null;

    [SerializeField]
    TextMeshPro m_roundTextTitle = null;

    [SerializeField]
    TextMeshPro m_pauseText = null;

    [Header( "Debug" )]

    [SerializeField]
    private bool m_spawnAtPlayerPosition = false;

    private float m_timeElapsed = 0.0f;
    bool m_timerRunning = false;

    public void OnDeath() {
        var lives = GetComponent<Health>().Lives;

        if ( lives <= 0 ) {
            lives = m_playerLives;
            PlayerPrefs.SetInt( "spawner index", 0 );
            PlayerPrefs.SetInt( "round", 1 );
            PlayerPrefs.SetFloat( "elapsed time", 0 );
        }else PlayerPrefs.SetFloat( "elapsed time", m_timeElapsed );

        PlayerPrefs.SetInt( "deaths", PlayerPrefs.GetInt( "deaths", 0 ) + 1 );
        PlayerPrefs.SetInt( "lives", lives );
        PlayerPrefs.Save();

        SceneManager.LoadScene( SceneManager.GetActiveScene().name );
    }

    public void NextRound() {
        var bestTime = PlayerPrefs.GetFloat( "best time", -1 );
        if ( bestTime < 0 || m_timeElapsed < bestTime )
            PlayerPrefs.SetFloat( "best time", m_timeElapsed );

        var round = PlayerPrefs.GetInt( "round", 0 ) + 1;
        var highestRound = PlayerPrefs.GetInt( "highest round", 1 );

        PlayerPrefs.SetInt( "round", round );
        if( round > highestRound ) PlayerPrefs.SetInt( "highest round", round );

        ResetTimer();
        PlayerPrefs.SetInt( "lives", GetComponent<Health>().Lives + 1 );
        PlayerPrefs.SetInt( "spawner index", 0 );
        PlayerPrefs.Save();

        SceneManager.LoadScene( SceneManager.GetActiveScene().name );
    }

    public void StartTimer() {
        if ( m_timerRunning ) return;

        m_timeElapsed = PlayerPrefs.GetFloat( "elapsed time", 0.0f );
        m_timerRunning = true;
    }

    public void TogglePause() {
        StartCoroutine( Pause() );
    }

    public IEnumerator Pause() {
        // backwards because we haven't toggled yet
        if ( !InputManager.instance.IsPaused ) {
            SongManager.instance.Pause();
            CameraManager.instance.FadeOut( 0.1f );
            yield return new WaitForSeconds( 0.1f );
            m_pauseText.text = "PAUSED\n\nESC/START: RESUME\nQ: QUIT";
        } else {
            CameraManager.instance.FadeIn( 0.3f );
            SongManager.instance.Resume();
            m_pauseText.text = "";
        }
        InputManager.instance.TogglePause();
    }

    public void ResetTimer() {
        PlayerPrefs.SetFloat( "elapsed time", 0 );
        PlayerPrefs.Save();
    }

    public void ResetData() {
        PlayerPrefs.SetInt( "lives", m_playerLives );
        PlayerPrefs.SetInt( "spawner index", 0 );
        PlayerPrefs.SetInt( "deaths", 0 );
        PlayerPrefs.SetInt( "round", 1 );
        PlayerPrefs.SetInt( "best time", -1 );
        PlayerPrefs.SetInt( "highest round", 1 );
        ResetTimer();
        PlayerPrefs.Save();

        SceneManager.LoadScene( SceneManager.GetActiveScene().name );
    }

    public void SetCurrentSpawner( RespawnPoint a_spawner ) {
        var i = PlayerSpawnManager.instance.GetSpawnerIndex( a_spawner );
        if ( i < 0 ) return;

        PlayerPrefs.SetInt( "spawner index", i );
        PlayerPrefs.Save();
    }

    int cheatKeys = 0;
    public bool GodMode = false;

    private void Update() {
        if ( Input.GetKeyDown( KeyCode.Q ) && InputManager.instance.IsPaused ) Application.Quit();

        if ( cheatKeys == 4 && Input.GetKeyDown( KeyCode.D ) ) {
            Debug.Log( "Cheat activated" );
            GodMode = true;
            GetComponent<Health>().IsInvincible = true;
            GetComponentInChildren<HeatSuit>().GodMode = true;
        }
        if ( cheatKeys == 3 && Input.GetKeyDown( KeyCode.Q ) ) ++cheatKeys;
        if ( cheatKeys == 2 && Input.GetKeyDown( KeyCode.D ) ) ++cheatKeys;
        if ( cheatKeys == 1 && Input.GetKeyDown( KeyCode.D ) ) ++cheatKeys;
        if ( cheatKeys == 0 && Input.GetKeyDown( KeyCode.I ) ) ++cheatKeys;

        Debug.Log( cheatKeys + "cheat keys" );

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        if ( !m_timerRunning ) return;

        m_timeElapsed += Time.deltaTime;
        var minutes = Mathf.Floor( m_timeElapsed / 60.0f );
        var seconds = Mathf.Floor( ( m_timeElapsed - minutes * 60.0f ) * 100.0f ) / 100.0f;
        m_timerText.text = string.Format( "{0:00}:{1:00.00}", minutes, seconds );
    }

    void Start() {
        Debug.Log( "Lava player start" );
        InputManager.instance.CurPage.AddListenerDown( KeyCode.F9, ResetData );

        var lives = PlayerPrefs.GetInt( "lives", m_playerLives );
        if ( lives > 0 ) GetComponent<Health>().Lives = lives;

        var round = PlayerPrefs.GetInt( "round", 1 );
        var health = m_initialMaxHealth;
        for ( int i = 1; i < round; ++i ) {
            health = Mathf.CeilToInt( 0.5f * health );
            Debug.LogFormat( "Round {0}: (health {1})", i, health );
        }
        health = Mathf.Clamp( health, 1, m_initialMaxHealth );
        GetComponent<Health>().SetRange( 0, health );

        m_deathsText.text = "D" + PlayerPrefs.GetInt( "deaths" );
        m_livesText.text = "L" + PlayerPrefs.GetInt( "lives" );
        m_roundText.text = "R" + round;

        m_bestTimeText.text = "";
        var bestTime = PlayerPrefs.GetFloat( "best time", -1 );
        if ( bestTime > 0 ) {
            var minutes = Mathf.Floor( bestTime / 60.0f );
            var seconds = Mathf.Floor( ( bestTime - minutes * 60.0f ) * 100.0f ) / 100.0f;

            m_bestTimeText.text = string.Format( "Best Time: {0:00}:{1:00.00}", minutes, seconds );
        } 

        var highestRound = PlayerPrefs.GetInt( "highest round", 1 );
        m_bestTimeText.text += "\nHighest Round: " + highestRound;

        // spawn the player if we have a room
        Debug.Log( "Spawn player" );
        if ( !m_spawnAtPlayerPosition ) {
            var spawnerIndex = PlayerPrefs.GetInt( "spawner index", 0 );
            var spawner = PlayerSpawnManager.instance[spawnerIndex];
            if ( spawner == null ) {
                Debug.LogError( "Unknown spawner #{0}" );
                return;
            }
            spawner.Spawn();
        }

        if ( round == 1 ) m_roundTextTitle.text = "";
        else m_roundTextTitle.text = "ROUND " + round + " START";

        // HACK
        GetComponentInChildren<Jumper>().SnapToGround();
    }
}
