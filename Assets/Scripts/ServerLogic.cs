using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerLogic : MonoBehaviour
{
    [SerializeField]
    PeerLogic peer;

    [SerializeField]
    GameObject player;

    [SerializeField]
    GameObject playerDisplay;

    // to run at fixed tick rate
    private float serverTimer;
    public uint serverTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 60f;
    private const float latency = 0.08f;

    private float packetLossChance = 0.05f;

    // To avoid calling Physics.Simulate a lot in one scene [in one tick]
    // it makes physics bad
    private Scene serverScene;
    private PhysicsScene serverPhysicsScene;

    public Queue<Structs.InputMessage> inputMessagesReceived;
    public Queue<Structs.StateMessage> statesToSend;

    void Start()
    {
        serverTimer = 0.0f;
        serverTick = 0;
        minTimeBetweenTicks = 1 / SERVER_TICK_RATE;

        Physics.autoSimulation = false;
        Application.targetFrameRate = 60;

        inputMessagesReceived = new Queue<Structs.InputMessage>();
        statesToSend = new Queue<Structs.StateMessage>();

        serverScene = SceneManager.LoadScene(
                "ServerScene",
                new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D }
            );

        serverPhysicsScene = serverScene.GetPhysicsScene();

        SceneManager.MoveGameObjectToScene(player, serverScene);
    }


    void Update()
    {

        serverTimer += Time.deltaTime;

        while (serverTimer >= minTimeBetweenTicks)
        {
            serverTimer -= minTimeBetweenTicks;

            // Handle a server tick

            // if server received an input
            while (inputMessagesReceived.Count > 0)
            {
                Structs.InputMessage input_msg = inputMessagesReceived.Dequeue();

                // Simulate input
                player.GetComponent<Player>().PhysicsStep(input_msg.inputs, minTimeBetweenTicks);
                serverPhysicsScene.Simulate(minTimeBetweenTicks);

                // To see how my player moves on the server
                playerDisplay.transform.position = player.transform.position;
                playerDisplay.transform.rotation = player.transform.rotation;

                // Send simulation result to the player
                Structs.StateMessage stateMessage;
                stateMessage.delivery_time = Time.time + latency; // time + lag(to simulate ping)
                stateMessage.tick_number = input_msg.tick_number + 1;
                stateMessage.position = player.transform.position;
                stateMessage.rotation = player.transform.rotation;
                stateMessage.velocity = player.GetComponent<Rigidbody>().velocity;
                stateMessage.angular_velocity = player.GetComponent<Rigidbody>().angularVelocity;

                if (Random.value > packetLossChance)
                {
                    statesToSend.Enqueue(stateMessage);
                }
            }

            /* Simulate sending a message to a client with latency. */
            while (statesToSend.Count > 0 &&
                   Time.time >= statesToSend.Peek().delivery_time)
            {
                Structs.StateMessage stateMessage = statesToSend.Dequeue();
                peer.peerReceivedStates.Enqueue(stateMessage);
            }

            serverTick++;
        }

    }
}
