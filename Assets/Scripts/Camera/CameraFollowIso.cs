using UnityEngine;

public class CameraFollowIso : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(10f, 10f, -10f);
    public float followSpeed = 8f;

    private void LateUpdate()
    {
        if (!target) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);

        transform.LookAt(target.position);
    }
}
