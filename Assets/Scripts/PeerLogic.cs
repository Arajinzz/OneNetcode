using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeerLogic : MonoBehaviour
{
    enum ControlsSchema
    {
        Player1,
        Player2
    }

    /* 
     * The general idea is my script will calculate his mouvements or physics,
     * then sends the result to the net player, the net player will simulate
     * my player and displaySimulationOnNetPlayer will show us if our simulation
     * is in sync, displaySimulationOnNetPlayer is how my local player moved
     * on the networked player.
     */

    [SerializeField]
    PeerLogic otherPeer;

    [SerializeField]
    GameObject localPlayer; // My local player

    [SerializeField]
    GameObject displaySimulationOnNetPlayer; /* How local player moved on the other peer. */

    [SerializeField]
    GameObject otherPlayerOnMyMachine; /* Other player on my machine. */

    [SerializeField]
    GameObject proxyPlayer;

    [SerializeField]
    ControlsSchema whatControls;

    // to run at fixed tick rate
    private float timer;
    private uint currentTick;
    private float minTimeBetweenTicks;
    private float PEER_TICK_RATE = 60f;

    public Queue<Structs.StateMessage> peerReceivedInputs;

    void Start()
    {
        Physics.autoSimulation = false;
        Application.targetFrameRate = 60;

        timer = 0.0f;
        currentTick = 0;
        minTimeBetweenTicks = 1 / PEER_TICK_RATE;
        peerReceivedInputs = new Queue<Structs.StateMessage>();
    }


    void Update()
    {
        timer += Time.deltaTime;

        while (timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;

            // Handle a tick

            // We start by getting the input
            Structs.Inputs inputs = GetInput();

            // Simulate physics on local player
            localPlayer.GetComponent<Player>().PhysicsStep(inputs, minTimeBetweenTicks);
            Physics.Simulate(minTimeBetweenTicks);

            // Send simulation result to other player
            Structs.StateMessage stateMessage;
            stateMessage.delivery_time = Time.time + 0.1f; // time + lag(to simulate ping)
            stateMessage.tick_number = currentTick;
            stateMessage.position = localPlayer.transform.position;
            stateMessage.rotation = localPlayer.transform.rotation;
            stateMessage.velocity = localPlayer.GetComponent<Rigidbody>().velocity;
            stateMessage.angular_velocity = localPlayer.GetComponent<Rigidbody>().angularVelocity;

            otherPeer.peerReceivedInputs.Enqueue(stateMessage);

            // Now simulate what happens when we receive a state from another player

            /*
             * First check if we have some states in the queue that needs to simulated,
             * and if latest state received is in the past (means should be simulated),
             * this is a bad phrasing i should explain this more
             */
            while(peerReceivedInputs.Count > 0 &&
                  Time.time >= peerReceivedInputs.Peek().delivery_time)
            {
                Structs.StateMessage stateMsg = peerReceivedInputs.Dequeue();

                otherPlayerOnMyMachine.transform.position = stateMsg.position;
                otherPlayerOnMyMachine.transform.rotation = stateMsg.rotation;

                // Simulate physics on the imaginary displaySimulationOnNetPlayer
                displaySimulationOnNetPlayer.transform.position = otherPlayerOnMyMachine.transform.position;
                displaySimulationOnNetPlayer.transform.rotation = otherPlayerOnMyMachine.transform.rotation;
            }

            currentTick++;
        }
    }


    Structs.Inputs GetInput()
    {
        Structs.Inputs inputs;
        if (ControlsSchema.Player1 == whatControls)
        {
            inputs.up = Input.GetKey(KeyCode.W);
            inputs.down = Input.GetKey(KeyCode.S);
            inputs.left = Input.GetKey(KeyCode.A);
            inputs.right = Input.GetKey(KeyCode.D);
            inputs.jump = Input.GetKey(KeyCode.Space);
        } else
        {
            inputs.up = Input.GetKey(KeyCode.UpArrow);
            inputs.down = Input.GetKey(KeyCode.DownArrow);
            inputs.left = Input.GetKey(KeyCode.LeftArrow);
            inputs.right = Input.GetKey(KeyCode.RightArrow);
            inputs.jump = Input.GetKey(KeyCode.P);
        }

        return inputs;
    }

}
