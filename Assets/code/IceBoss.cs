using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(IceBoss))]
public class IceBossScriptEditor : ScriptEditor
{
    public override void OnInspectorGUI() {
        LinkComponent<Jumper>( "m_jumper" );
        LinkComponent<BodyMover>( "m_mover" );
        LinkComponent<Shooter>( "m_shooter" );
        LinkComponent<ShooterController>( "m_shooterController" );

        DrawDefaultScriptEditor();
    }
}
#endif // UNITY_EDITOR

public class IceBoss : MonoBehaviour {
    [Header("Components")]

    [SerializeField]
    private SpriteAnimator m_animator = null;

    [SerializeField]
    private Jumper m_jumper = null;

    [SerializeField]
    private Health m_health = null;

    [SerializeField]
    private BodyMover m_mover = null;

    [SerializeField]
    private Shooter m_shooter = null;

    [SerializeField]
    private ShooterController m_shooterController = null;

    [Header( "Jump" )]

    [SerializeField]
    private float m_jumpSpdMult = 3.0f;

    [SerializeField]
    private float m_jumpPlayerDist = 2.0f;

    [SerializeField]
    private float m_jumpTime = 2.0f;

    [SerializeField]
    private float m_minWallDistance = 1.0f;

    [Header("Shooting")]

    [SerializeField]
    private float m_timeBetweenShots = 2.5f;

    [SerializeField]
    private int m_burstCount = 3;

    [SerializeField]
    private float m_burstDelay = 0.5f;

    [SerializeField]
    private float m_throwerTime = 0.5f;

    [SerializeField]
    private float m_throwerAmplitude = 2.0f;

    [SerializeField]
    private float m_throwerFrequency = 10.0f;

    [SerializeField]
    [Tooltip("Out of 100")]
    private int m_throwerChance = 25;

    [Header( "Desperation" )]

    [SerializeField]
    private int m_desperationHealthPercent = 30;

    [SerializeField]
    private GameObject m_desperationGameObject = null;

    private GameObject m_player = null;
    private Jumper m_playerJumper = null;

    private bool m_isFiring = false;
    private bool m_isJumping = false;
    private float m_timeSinceLastShot = Mathf.Infinity;
    private float m_timeSinceLastJump = Mathf.Infinity;

    public void Jump() {
        if ( transform.position.x < 34.0f || transform.position.x > 46.0f ) return;

        m_jumper.Jump();
        OnJump();
    }

	private void Awake () {
        Utility.RequireComponent( this, m_jumper );
        Utility.RequireComponent( this, m_mover );
        Utility.RequireComponent( this, m_shooter );
        Utility.RequireComponent( this, m_shooterController );
	}

    private void OnDrawGizmosSelected() {
        if ( RoomManager.instance == null ) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere( RoomManager.instance.CurRoom.GetWallCenter( Direction.East ) - Vector2.right * m_minWallDistance, 0.5f );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere( RoomManager.instance.CurRoom.GetWallCenter( Direction.West ) + Vector2.right * m_minWallDistance, 0.5f );
    }

    private void Start() {
        m_player = GameObject.FindGameObjectWithTag( "Player" );
        m_playerJumper = m_player.GetComponentInChildren<Jumper>();
    }

    private void Update() {
        if ( m_mover.IsInKnockback ) return;

        if ( !m_desperationGameObject.activeSelf && m_health.Percent * 100.0f < m_desperationHealthPercent ) {
            m_desperationGameObject.SetActive( true );
            m_desperationGameObject.transform.position = transform.position;
        }

        var distanceToPlayer = Vector2.Distance( transform.position, m_player.transform.position );

        // jump when player is close
        m_timeSinceLastJump += Time.deltaTime;
        bool closeToPlayer = distanceToPlayer < m_jumpPlayerDist;
        if ( m_timeSinceLastJump > m_jumpTime && !m_isJumping && !m_playerJumper.IsJumping && closeToPlayer ) {
            Jump();
            return;
        }

        // when jumping
        m_mover.SpeedMultiplier = 1.0f;
        if ( m_isJumping ) {
            if ( transform.position.x < 34.0f || transform.position.x > 46.0f ) {
                m_mover.SpeedMultiplier = 1.0f;
                m_mover.Stop();
                m_jumper.StopJump( true );
            } else m_mover.SpeedMultiplier = m_jumpSpdMult;

            if ( m_jumper.IsGrounded ) OnLanded();

            return;
        }

        // when not jumping
        m_mover.MoveInDirection( m_player.transform.position - transform.position );
        if ( m_isFiring ) {
            m_mover.Stop();
            m_animator.SetAnimation( "shoot" );
        }

        // fire rapid every so often when not jumping or bursting
        m_timeSinceLastShot += Time.deltaTime;
        if ( m_timeSinceLastShot > m_timeBetweenShots ) {
            m_timeSinceLastShot = 0.0f;
            if ( !m_isFiring && !m_isJumping ) {
                if( Random.Range(0, 100) < m_throwerChance ) StartCoroutine( FireIceThrower() );
                else StartCoroutine( FireBurst() );
            }
        }
    }

    private void OnJump() {
        m_isJumping = true;
        m_timeSinceLastJump = 0.0f;
        m_mover.MoveInDirection( m_player.transform.position - transform.position );
        m_animator.SetAnimation( "jump" );
        Debug.Log( "Boss jump" );
    }

    private void OnLanded() {
        m_isJumping = false;
        if( !m_isFiring ) StartCoroutine( FireBurst() );
        Debug.Log( "Boss land" );
    }

    IEnumerator FireBurst() {
        m_isFiring = true;

        m_shooterController.CurFireMode = ShooterController.FireMode.Single;
        m_shooter.BulletLifeTime = 2.0f;

        m_shooter.AimMode = AimMode.Facing;

        for ( int i = 0; i < m_burstCount; ++i ) {
            m_shooterController.HandleFireDown();

            if ( i < m_burstCount - 1 ) {
                yield return new WaitForSeconds( m_burstDelay );
                m_shooterController.HandleFireUp();
            }
        }

        m_isFiring = false;
    }

    IEnumerator FireIceThrower() {
        m_isFiring = true;

        m_shooterController.CurFireMode = ShooterController.FireMode.Rapid;
        m_shooter.BulletLifeTime = 0.5f;

        m_shooter.AimMode = AimMode.Direction;

        var timeElapsed = 0.0f;
        m_shooterController.HandleFireDown();
        while( timeElapsed < m_throwerTime ) {
            timeElapsed += Time.deltaTime;
            var dir = m_player.transform.position - transform.position;
            dir.y = Mathf.Sin( Time.realtimeSinceStartup * m_throwerFrequency ) * m_throwerAmplitude;
            m_shooter.AimDirection = dir;

            yield return null;
        }
        m_shooterController.HandleFireUp();

        m_isFiring = false;
    }
}
