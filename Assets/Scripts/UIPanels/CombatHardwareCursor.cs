using UnityEngine;

/// <summary>
/// Sets the hardware cursor during combat from turn state and <see cref="PlayerController"/> tile hover.
/// Textures are loaded from <c>Resources/ScavengerCursors/</c> (copies of Kenney PNGs).
/// </summary>
[DefaultExecutionOrder(500)]
public sealed class CombatHardwareCursor : MonoBehaviour
{
    static readonly Vector2 Hotspot = new Vector2(4f, 4f);

    /// <summary>Used to clear stale combat <see cref="Cursor.SetCursor"/> once when the pointer enters blocking UITK UI, not every frame (which would fight USS cursor).</summary>
    bool _wasPointerOverBlockingUiLastFrame;

    Texture2D waitTexture;
    Texture2D movementTexture;
    Texture2D rangedTexture;
    Texture2D meleeTexture;
    Texture2D groundTexture;
    Texture2D disabledTexture;

    void Awake()
    {
        waitTexture = LoadCursorTexture("ScavengerCursors/busy_hourglass");
        movementTexture = LoadCursorTexture("ScavengerCursors/boot");
        rangedTexture = LoadCursorTexture("ScavengerCursors/target_round_a");
        meleeTexture = LoadCursorTexture("ScavengerCursors/tool_sword_a");
        groundTexture = LoadCursorTexture("ScavengerCursors/tool_bomb");
        disabledTexture = LoadCursorTexture("ScavengerCursors/cursor_disabled");
    }

    static Texture2D LoadCursorTexture(string resourcePath)
    {
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex != null) return tex;
        Sprite sp = Resources.Load<Sprite>(resourcePath);
        return sp != null ? sp.texture : null;
    }

    void OnDisable()
    {
        Clear();
    }

    void LateUpdate()
    {
        bool overBlockingUi = UIClickBlocker.IsPointerOverBlockingUI();
        if (overBlockingUi)
        {
            // Combat uses Cursor.SetCursor; UITK USS cursor also drives the OS cursor. If we only
            // "return" here, the last combat texture (boot, sword, etc.) stays forever — UITK does
            // not paint on top of an existing custom hardware cursor. Clear once on enter blocking UI
            // so the panel/default cursor can show; avoid Clear every frame so USS hover can stick.
            if (!_wasPointerOverBlockingUiLastFrame)
                Clear();
            _wasPointerOverBlockingUiLastFrame = true;
            return;
        }

        _wasPointerOverBlockingUiLastFrame = false;

        TurnManager turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Exclude);
        if (turnManager != null && !turnManager.IsPlayerTurn())
        {
            Apply(waitTexture);
            return;
        }

        PlayerController active = null;
        var pcs = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < pcs.Length; i++)
        {
            PlayerController pc = pcs[i];
            if (pc != null && pc.isTurn && !pc.isActing)
            {
                active = pc;
                break;
            }
        }

        if (active == null)
        {
            Clear();
            return;
        }

        active.ApplyHardwareCursor(this);
    }

    public void Clear()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void ApplyWait() => Apply(waitTexture);
    public void ApplyMovement() => Apply(movementTexture);
    public void ApplyRanged() => Apply(rangedTexture);
    public void ApplyMelee() => Apply(meleeTexture);
    public void ApplyGround() => Apply(groundTexture);
    public void ApplyDisabled() => Apply(disabledTexture);

    void Apply(Texture2D tex)
    {
        if (tex == null)
        {
            Clear();
            return;
        }
        Cursor.SetCursor(tex, Hotspot, CursorMode.Auto);
    }
}
