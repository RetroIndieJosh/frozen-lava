using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(AiRedShell))]
public class AiRedShellScriptEditor : ScriptEditor
{
    public override void OnInspectorGUI() {
        LinkComponent<Jumper>( "m_jumper" );
        LinkComponent<BodyMover>( "m_mover" );

        DrawDefaultScriptEditor();
    }
}
#endif // UNITY_EDITOR


public class AiRedShell : MonoBehaviour {
    [SerializeField]
    private Jumper m_jumper = null;

    [SerializeField]
    private BodyMover m_mover = null;

    [SerializeField]
    private bool m_startRight = false;

    bool m_movingRight = true;
    private bool m_falling = true;

    private void Start() {
        Utility.RequireComponent( this, m_mover );
        m_movingRight = m_startRight;

        //if ( m_movingRight ) m_mover.SetDirection( Vector3.right );
        //else m_mover.SetDirection( Vector3.left );
    }

    private void Update() {
        if ( Mathf.Abs( GetComponent<Rigidbody2D>().velocity.y ) < Mathf.Epsilon ) m_falling = false;
        if ( m_falling ) return;

        if ( m_movingRight ) {
            m_mover.MoveInDirection( Vector3.right );
            if ( m_mover.IsAgainstWallRight ) m_movingRight = false;
            if ( m_jumper != null && m_jumper.RightGroundHit == false ) m_movingRight = false;
        } else {
            m_mover.MoveInDirection( Vector3.left );
            if ( m_mover.IsAgainstWallLeft ) m_movingRight = true;
            if ( m_jumper != null && m_jumper.LeftGroundHit == false ) m_movingRight = true;
        }
    }
}
