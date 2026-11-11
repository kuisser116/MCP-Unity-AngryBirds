using UnityEngine;

/// <summary>Camara que sigue al ave en vuelo, con limites para no perder la escena.</summary>
public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 idleOffset = new Vector3(0.8f, 2.4f, -10f);
    public float minX = -1.2f;
    public float maxX = 6.6f;
    public float minY = 2.2f;
    public float maxY = 3.6f;
    public float smoothTime = 0.35f;

    Vector3 velocity;

    void LateUpdate()
    {
        Vector3 desired = idleOffset;
        if (target != null)
        {
            Vector3 p = target.position;
            desired = new Vector3(
                Mathf.Clamp(p.x + 1.9f, minX, maxX),
                Mathf.Clamp(p.y + 2.0f, minY, maxY),
                -10f);
        }
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
