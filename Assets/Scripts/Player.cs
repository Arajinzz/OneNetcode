using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movementImpulse;
    public float jumpThreshold;

    private Transform cameraTransform;
    new private Rigidbody rigidbody;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null)
            return;

        if (Input.GetKey(KeyCode.W))
        {
            rigidbody.AddForce(cameraTransform.forward * movementImpulse * Time.fixedDeltaTime, ForceMode.Impulse);
        }

        if (Input.GetKey(KeyCode.S))
        {
            rigidbody.AddForce(-cameraTransform.forward * movementImpulse * Time.fixedDeltaTime, ForceMode.Impulse);
        }

        if (Input.GetKey(KeyCode.A))
        {
            rigidbody.AddForce(-cameraTransform.right * movementImpulse * Time.fixedDeltaTime, ForceMode.Impulse);
        }

        if (Input.GetKey(KeyCode.D))
        {
            rigidbody.AddForce(cameraTransform.right * movementImpulse * Time.fixedDeltaTime, ForceMode.Impulse);
        }

        if (transform.position.y <= jumpThreshold && Input.GetKey(KeyCode.Space))
        {
            rigidbody.AddForce(cameraTransform.up * movementImpulse * Time.fixedDeltaTime, ForceMode.Impulse);
        }
    }
}
