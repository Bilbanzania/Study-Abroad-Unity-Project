// SimpleCameraController.cs
using UnityEngine;
using UnityEngine.InputSystem; // Requires Input System package

public class SimpleCameraController : MonoBehaviour
{
    public float moveSpeed = 10.0f;
    public bool lockCursorOnStart = false;
    private PlayerControls playerControls; // Assumes PlayerControls.cs is generated from an Input Actions asset
    private Vector2 moveInputXZ;
    private float moveInputY;

    void Awake()
    {
        playerControls = new PlayerControls();
        playerControls.Gameplay.MoveUp.performed += ctx => moveInputY = 1f;
        playerControls.Gameplay.MoveUp.canceled += ctx => moveInputY = 0f;
        playerControls.Gameplay.MoveDown.performed += ctx => moveInputY = -1f;
        playerControls.Gameplay.MoveDown.canceled += ctx => moveInputY = 0f;
        playerControls.Gameplay.ToggleCursorLock.performed += ctx => ToggleCursorState();
    }
    void OnEnable() { playerControls.Gameplay.Enable(); }
    void OnDisable() { playerControls.Gameplay.Disable(); }
    void Start()
    {
        if (lockCursorOnStart) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
    void Update()
    {
        moveInputXZ = playerControls.Gameplay.Move.ReadValue<Vector2>();
        Vector3 move = (Vector3.forward * moveInputXZ.y + Vector3.right * moveInputXZ.x + Vector3.up * moveInputY);
        if (move.sqrMagnitude > 1f) move.Normalize();
        transform.Translate(move * moveSpeed * Time.unscaledDeltaTime, Space.World);
        if (lockCursorOnStart && Cursor.lockState == CursorLockMode.None && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (playerControls.Gameplay.enabled) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
    }
    private void ToggleCursorState()
    {
        if (Cursor.lockState == CursorLockMode.Locked || Cursor.lockState == CursorLockMode.Confined)
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        else { if (lockCursorOnStart) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; } }
    }
}