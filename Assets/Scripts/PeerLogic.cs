using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    ServerLogic server;

    [SerializeField]
    GameObject localPlayer; // My local player

    [SerializeField]
    GameObject myPlayerOnServer; // My player on the server

    [SerializeField]
    GameObject displayMyPlayerOnServer; // To display how my player behave on the server

    // to run at fixed tick rate
    private float peerTimer;
    private uint currentTick;
    private float minTimeBetweenTicks;
    private const float PEER_TICK_RATE = 60f;
    private const float latency = 0.15f;

    private float packetLossChance = 0.05f;

    private const int peerBufferSize = 1024;
    private Structs.PlayerState[] peerBufferStates; // Holds position of local player at a given tick

    public Queue<Structs.InputMessage> inputMessagesToSend;
    public Queue<Structs.StateMessage> peerReceivedStates;

    void Start()
    {
        Physics.autoSimulation = false;
        Application.targetFrameRate = 60;

        peerTimer = 0.0f;
        currentTick = 0;
        minTimeBetweenTicks = 1 / PEER_TICK_RATE;
        inputMessagesToSend = new Queue<Structs.InputMessage>();

        peerReceivedStates = new Queue<Structs.StateMessage>();
        peerBufferStates = new Structs.PlayerState[peerBufferSize];
    }


    void Update()
    {
        peerTimer += Time.deltaTime;

        while (peerTimer >= minTimeBetweenTicks)
        {
            peerTimer -= minTimeBetweenTicks;

            // Handle a tick

            // We start by getting the input
            Structs.Inputs inputs = GetInput();

            // Before simulation save my state
            uint bufferSlot = currentTick % peerBufferSize;
            peerBufferStates[bufferSlot].position = localPlayer.transform.position;
            peerBufferStates[bufferSlot].rotation = localPlayer.transform.rotation;
            peerBufferStates[bufferSlot].inputs = inputs;

            // Simulate physics on local player
            localPlayer.GetComponent<Player>().PhysicsStep(inputs, minTimeBetweenTicks);
            Physics.Simulate(minTimeBetweenTicks);

            // Send input message to server
            Structs.InputMessage inputMsg;
            inputMsg.delivery_time = Time.time + latency;
            inputMsg.tick_number = currentTick;
            inputMsg.inputs = inputs;

            if (Random.value > packetLossChance)
            {
                inputMessagesToSend.Enqueue(inputMsg);
            }

            /* Simulate received states. */
            while (peerReceivedStates.Count > 0)
            {
                Structs.StateMessage stateMsg = peerReceivedStates.Dequeue();

                // Correction
                uint buffer_slot = stateMsg.tick_number % peerBufferSize;

                // Check if there's a big error
                if ((stateMsg.position - peerBufferStates[buffer_slot].position).sqrMagnitude > 0.01f ||
                     Quaternion.Dot(stateMsg.rotation, peerBufferStates[buffer_slot].rotation) < 0.99f)
                {
                    localPlayer.transform.position = stateMsg.position;
                    localPlayer.transform.rotation = stateMsg.rotation;
                    localPlayer.GetComponent<Rigidbody>().velocity = stateMsg.velocity;
                    localPlayer.GetComponent<Rigidbody>().angularVelocity = stateMsg.angular_velocity;

                    // How many ticks we're gonna rewind
                    uint rewindTick = stateMsg.tick_number;

                    while (rewindTick < currentTick)
                    {
                        buffer_slot = rewindTick % peerBufferSize;

                        peerBufferStates[buffer_slot].position = localPlayer.transform.position;
                        peerBufferStates[buffer_slot].rotation = localPlayer.transform.rotation;

                        localPlayer.GetComponent<Player>().PhysicsStep(peerBufferStates[buffer_slot].inputs, minTimeBetweenTicks);
                        Physics.Simulate(minTimeBetweenTicks);

                        ++rewindTick;
                    }
                }
            }

            /* Simulate sending a message to a server with latency. */
            while (inputMessagesToSend.Count > 0 &&
                   Time.time >= inputMessagesToSend.Peek().delivery_time)
            {
                Structs.InputMessage input_msg = inputMessagesToSend.Dequeue();
                //Debug.Log("Send Input message of tick " + input_msg.tick_number + " at tick " + currentTick + " While server tick is at " + server.serverTick);
                server.inputMessagesReceived.Enqueue(input_msg);
            }

            currentTick++;

        }
    }


    Structs.Inputs GetInput()
    {
        Structs.Inputs inputs;
        inputs.up = Input.GetKey(KeyCode.W);
        inputs.down = Input.GetKey(KeyCode.S);
        inputs.left = Input.GetKey(KeyCode.A);
        inputs.right = Input.GetKey(KeyCode.D);
        inputs.jump = Input.GetKey(KeyCode.Space);

        return inputs;
    }

}
