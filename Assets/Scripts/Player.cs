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

    public void PhysicsStep(Structs.Inputs inputs, float deltatime)
    {
        if (cameraTransform == null)
            return;

        if (inputs.up)
        {
            rigidbody.AddForce(cameraTransform.forward * movementImpulse * deltatime, ForceMode.Impulse);
        }

        if (inputs.down)
        {
            rigidbody.AddForce(-cameraTransform.forward * movementImpulse * deltatime, ForceMode.Impulse);
        }

        if (inputs.left)
        {
            rigidbody.AddForce(-cameraTransform.right * movementImpulse * deltatime, ForceMode.Impulse);
        }

        if (inputs.right)
        {
            rigidbody.AddForce(cameraTransform.right * movementImpulse * deltatime, ForceMode.Impulse);
        }

        if (transform.position.y <= jumpThreshold && inputs.jump)
        {
            rigidbody.AddForce(cameraTransform.up * movementImpulse * deltatime, ForceMode.Impulse);
        }
    }
}
