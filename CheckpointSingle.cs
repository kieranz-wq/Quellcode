using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    private TrackManager trackManager;

    public void SetTrackManager(TrackManager trackManager)
    {
        this.trackManager = trackManager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CarAgent>(out CarAgent agent))
        {
            trackManager.CarThroughCheckpoint(this, agent);
        }
    }
}