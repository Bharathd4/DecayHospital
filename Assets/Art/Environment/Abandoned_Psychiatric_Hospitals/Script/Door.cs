
using UnityEngine;

public class Door : MonoBehaviour
{
    public float smooth = 2f;
    public float DoorOpenAngle = 90f;

    private bool open;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = transform.localRotation;

        openRotation = closedRotation *
            Quaternion.Euler(0f, DoorOpenAngle, 0f);
    }

    private void Update()
    {
        if (!open)
            return;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            openRotation,
            Time.deltaTime * smooth
        );
    }

    public void OpenAutomatically()
    {
        open = true;
    }

    public static void OpenAllDoors()
    {
        Door[] doors = FindObjectsByType<Door>();

        foreach (Door door in doors)
        {
            door.OpenAutomatically();
        }

        Debug.Log("All hospital doors opened: " + doors.Length);
    }
}
