using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{

    const int DIR_NORTH = 0;
    const int DIR_EAST = 1;
    const int DIR_SOUTH = 2;
    const int DIR_WEST = 3;

    int dir = 0;
    bool rotationFrozen = false;

    private float updateTimeSeconds = 0.25f;

    const float MIN_SCROLL = -0.5f;
    const float MAX_SCROLL_BUFFER = 0.5f;

    public void RotateLeft()
    {
        if (rotationFrozen) return;
        StartCoroutine(GradualizeRotation(90, .025f));
        dir = (dir + 3) % 4;
    }

    public void RotateRight()
    {
        if (rotationFrozen) return;
        StartCoroutine(GradualizeRotation(-90, .025f));
        dir = (dir + 1) % 4;
    }

    void CameraSpeedCalc(ref Vector3 cameraSpeed, int edge)
    {
        switch ((dir + edge) % 4)
        {
            case DIR_NORTH:
                cameraSpeed.x = Time.deltaTime * 10f;
                break;
            case DIR_EAST:
                cameraSpeed.y = Time.deltaTime * 10f;
                break;
            case DIR_SOUTH:
                cameraSpeed.x = Time.deltaTime * -10f;
                break;
            case DIR_WEST:
                cameraSpeed.y = Time.deltaTime * -10f;
                break;
        }
    }

    void MouseScroll()
    {
        Vector3 cameraSpeed = new Vector3(0f, 0f, 0f);
        Vector3 currentMousePosition = Input.mousePosition;
        if (currentMousePosition.x >= Screen.width - 20)
        {
            CameraSpeedCalc(ref cameraSpeed, DIR_NORTH);
        }
        if (currentMousePosition.x <= 20)
        {
            CameraSpeedCalc(ref cameraSpeed, DIR_SOUTH);
        }
        if (currentMousePosition.y >= Screen.height - 20)
        {
            CameraSpeedCalc(ref cameraSpeed, DIR_EAST);
        }
        if (currentMousePosition.y <= 20)
        {
            CameraSpeedCalc(ref cameraSpeed, DIR_WEST);
        }
        transform.Translate(cameraSpeed, Space.World);

        // Clamp the camera position so it doesn't go too far away from the grid.
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, MIN_SCROLL, Globals.COMBAT_HEIGHT + MAX_SCROLL_BUFFER),
            Mathf.Clamp(transform.position.y, MIN_SCROLL, Globals.COMBAT_WIDTH + MAX_SCROLL_BUFFER),
            transform.position.z
        );
    }

    private IEnumerator GradualizeRotation(int delta, float duration)
    {
        rotationFrozen = true;
        float initial = transform.eulerAngles.z;
        float elapsed = 0f;
        Vector3 tmp = transform.eulerAngles;
        while (elapsed < updateTimeSeconds)
        {
            elapsed += Time.deltaTime;
            tmp.z = Mathf.SmoothStep(initial, initial + delta, elapsed / updateTimeSeconds);
            transform.eulerAngles = tmp;
            yield return null;
        }
        tmp.z = Mathf.SmoothStep(initial, initial + delta, elapsed / updateTimeSeconds);
        transform.eulerAngles = tmp;
        rotationFrozen = false;
        yield break;
    }

    public bool IsFacingEastOrWest()
    {
        return dir == 1 || dir == 3;
    }

    void Update()
    {
        if (Input.GetKeyDown("q"))
        {
            RotateLeft();
        }
        if (Input.GetKeyDown("e"))
        {
            RotateRight();
        }
        MouseScroll();
    }

    private IEnumerator GradualizeMove(Vector3 loc)
    {
        Vector3 initial = transform.position;
        float elapsed = 0f;
        while (elapsed < updateTimeSeconds)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.SmoothStep(initial.x, loc.x, elapsed / updateTimeSeconds);
            float y = Mathf.SmoothStep(initial.y, loc.y, elapsed / updateTimeSeconds);
            transform.position = new Vector3(x, y, initial.z) ;
            yield return null;
        }
        transform.position = loc;
        yield break;
    }

    //public void ZoomNear(CombatController target)
    //{
    //    Vector3 newTarget = new Vector3(
    //        Mathf.Clamp(target.transform.position.x, MIN_SCROLL, MAX_SCROLL),
    //        target.transform.position.y,
    //        Mathf.Clamp(target.transform.position.z, MIN_SCROLL, MAX_SCROLL)
    //    );
    //    newTarget.y = transform.position.y;
    //    StartCoroutine(GradualizeMove(newTarget));
    //}

}