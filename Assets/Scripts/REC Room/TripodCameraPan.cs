using UnityEngine;

public class TripodCameraPan : MonoBehaviour
{
    public float horizontalLimit = 15f;
    public float verticalLimit = 2f;
    public float sensitivity = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private Quaternion startingRotation;

    private void Awake()
    {
        startingRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        rotationX = 0f;
        rotationY = 0f;
        transform.localRotation = startingRotation;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        rotationY += mouseX;
        rotationX -= mouseY;

        rotationY = Mathf.Clamp(rotationY, -horizontalLimit, horizontalLimit);
        rotationX = Mathf.Clamp(rotationX, -verticalLimit, verticalLimit);

        transform.localRotation = startingRotation * Quaternion.Euler(rotationX, rotationY, 0f);
    }
}