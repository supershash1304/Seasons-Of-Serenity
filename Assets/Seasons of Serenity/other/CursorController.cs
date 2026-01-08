using UnityEngine;

public class CursorController : MonoBehaviour
{
    void Start()
    {
        // Hide and lock cursor when game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Optional: press ESC to release cursor (for menus / debugging)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
