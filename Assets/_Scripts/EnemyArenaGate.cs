using UnityEngine;

public sealed class EnemyArenaGate : MonoBehaviour
{
    private Camera mainCamera;
    private bool isInside;

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        float depth = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 minBounds = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        Vector3 maxBounds = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
        Vector3 currentPosition = transform.position;

        bool currentlyInsideBounds = currentPosition.x >= minBounds.x
            && currentPosition.x <= maxBounds.x
            && currentPosition.y >= minBounds.y
            && currentPosition.y <= maxBounds.y;

        if (!isInside && currentlyInsideBounds)
        {
            isInside = true;
        }

        if (!isInside)
        {
            return;
        }

        currentPosition.x = Mathf.Clamp(currentPosition.x, minBounds.x, maxBounds.x);
        currentPosition.y = Mathf.Clamp(currentPosition.y, minBounds.y, maxBounds.y);
        transform.position = currentPosition;
    }
}