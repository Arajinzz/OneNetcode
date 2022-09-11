using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float movementImpulse;
    public float jumpThreshold;

    [SerializeField]
    GameObject toIgnore;

    private Transform cameraTransform;
    new private Rigidbody rigidbody;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform;

        // This is to make a more realistic peer simulation
        // Will instatiate another object which represents a player in other peer environement
        // This way the lag will be simulated correctly
        Physics.IgnoreCollision(toIgnore.GetComponentInChildren<Collider>(), GetComponentInChildren<Collider>());
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
