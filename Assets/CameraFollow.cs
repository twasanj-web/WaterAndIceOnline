using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10);
    private bool targetSet = false;

    public void SetTarget(Transform t)
    {
        if (targetSet) return; // لا تغيّر الهدف بعد ما يُضبط
        target = t;
        targetSet = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }
}