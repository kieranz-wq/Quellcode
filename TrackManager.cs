using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    [Header("Checkpoint Setup")]
    [SerializeField] private List<Transform> checkpointTransforms = new List<Transform>();

    private List<CheckpointSingle> checkpointList = new List<CheckpointSingle>();
    private int nextCheckpointIndex = 0;

    private void Awake()
    {
        checkpointList.Clear();
        foreach (Transform checkpointTransform in checkpointTransforms)
        {
            CheckpointSingle checkpointSingle = checkpointTransform.GetComponent<CheckpointSingle>();
            if (checkpointSingle == null)
            {
                checkpointSingle = checkpointTransform.gameObject.AddComponent<CheckpointSingle>();
            }
            checkpointSingle.SetTrackManager(this);
            checkpointList.Add(checkpointSingle);
        }
    }

    public void CarThroughCheckpoint(CheckpointSingle checkpointSingle, CarAgent agent)
    {
        int passedIndex = checkpointList.IndexOf(checkpointSingle);

        if (passedIndex == nextCheckpointIndex)
        {
            nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointList.Count;
            agent.AddReward(1.0f);
        }
        else
        {
            agent.AddReward(-0.5f);
        }
    }

    public Transform GetNextCheckpointTransform()
    {
        if (checkpointList.Count == 0) return transform;
        return checkpointList[nextCheckpointIndex].transform;
    }

    public Transform GetSpawnPoint()
    {
        return checkpointTransforms.Count > 0 ? checkpointTransforms[0] : transform;
    }

    public void ResetTrack()
    {
        nextCheckpointIndex = 0;
    }
}