using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

public class CarAgent : Agent
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float maxTimeWithoutCheckpoint = 25f; // Erhöht für bessere Lernphase!

    [Header("Grip & Drift")]
    [Range(0f, 1f)]
    [SerializeField] private float driftFactor = 0.1f; // 0 = Klebt am Boden, 1 = Eisbahn

    [Header("Track Setup")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private List<Transform> checkpoints = new List<Transform>();

    private Rigidbody rb;
    private int currentCheckpointIndex = 0;
    private float timeSinceLastCheckpoint = 0f;

    private float startTime;
    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currentCheckpointIndex = 0;
        timeSinceLastCheckpoint = 0f;
        startTime = Time.time;
    }

    /// <summary>
    /// NEU: Gibt der KI direkte Beobachtungen über ihren eigenen Zustand.
    /// WICHTIG: Setze "Space Size" in den Behavior Parameters im Inspector auf 2!
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Vorwärtsgeschwindigkeit (1 Float)
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        sensor.AddObservation(forwardSpeed);

        // 2. Ausrichtung zum nächsten Checkpoint (1 Float: 1.0 = Schaut direkt drauf, -1.0 = Schaut weg)
        if (checkpoints != null && currentCheckpointIndex < checkpoints.Count && checkpoints[currentCheckpointIndex] != null)
        {
            Vector3 dirToNextCheckpoint = (checkpoints[currentCheckpointIndex].position - transform.position).normalized;
            float directionDot = Vector3.Dot(transform.forward, dirToNextCheckpoint);
            sensor.AddObservation(directionDot);
        }
        else
        {
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float steer = actions.ContinuousActions[0];
        float accel = actions.ContinuousActions[1];

        // 1. Lenkung
        transform.Rotate(Vector3.up * steer * turnSpeed * Time.fixedDeltaTime);

        // 2. Antrieb (Rückwärtsfahren auf 40% reduzieren, um Falschfahren zu verhindern)
        float currentMotorPower = accel > 0f ? accel * moveSpeed : accel * (moveSpeed * 0.4f);
        rb.AddForce(transform.forward * currentMotorPower, ForceMode.Acceleration);

        // 3. Seitliches Rutschen neutralisieren (Grip erzeugen)
        ApplyGrip();

        // --- BELOHNUNGS-SYSTEM ---

        // A) Mini-Belohnung für Vorwärtsfahrt
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (forwardSpeed > 0f)
        {
            AddReward(forwardSpeed * 0.001f);
        }

        // B) Leichte Zeitstrafe pro Step
        AddReward(-0.001f);

        // C) Timer-Prüfung (Reset, wenn das Auto zu lange braucht)
        timeSinceLastCheckpoint += Time.fixedDeltaTime;
        if (timeSinceLastCheckpoint > maxTimeWithoutCheckpoint)
        {
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    private void ApplyGrip()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        rb.linearVelocity = forwardVelocity + (rightVelocity * driftFactor);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Sicherheits-Check: Gibt es überhaupt Checkpoints in der Liste?
        if (checkpoints == null || checkpoints.Count == 0) return;

        // 1. Ist das getroffene Objekt die ZIELLINIE (der allerletzte Checkpoint in der Liste)?
        if (other.transform == checkpoints[checkpoints.Count - 1])
        {
            // Hat das Auto auch wirklich alle vorherigen Checkpoints abgefahren?
            if (currentCheckpointIndex == checkpoints.Count - 1)
            {
                float rundenZeit = Time.time - startTime;
                Debug.Log("Rundenzeit: " + rundenZeit + " Sek");
                AddReward(10.0f); // Großer Bonus für die komplette Runde
                EndEpisode();
            }
            else
            {
                // Schummel-Versuch erkannt! Liste erstellen, welche Checkpoints verpasst wurden
                List<string> fehlende = new List<string>();
                for (int i = currentCheckpointIndex; i < checkpoints.Count - 1; i++)
                {
                    fehlende.Add(checkpoints[i].name);
                }
                
                Debug.Log("❌ ZIEL ERREICHT, ABER UNGÜLTIG! Verpasste Checkpoints: " + string.Join(", ", fehlende));
                
                AddReward(-2.0f); // Strafe fürs Abkürzen
                EndEpisode();
            }
            return; // Bricht die Methode hier ab, damit der Code für die normalen Checkpoints ignoriert wird
        }

        // 2. Normaler Ablauf für alle anderen Checkpoints davor
        if (currentCheckpointIndex < checkpoints.Count - 1 && other.transform == checkpoints[currentCheckpointIndex])
        {
            AddReward(1.0f);
            currentCheckpointIndex++;
            timeSinceLastCheckpoint = 0f; // Timer zurücksetzen
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Debug.Log("Kollision nach: " + (Time.time - startTime) + " Sek");
            AddReward(-1.0f);
            EndEpisode();
        }
    }
}
