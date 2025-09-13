// SetCheckpointOnPress.cs
using UnityEngine;

public class SetCheckpointOnPress : MonoBehaviour
{
    public Transform spawnPoint;     // jos tyhj�, k�ytet��n t�m�n objektin sijaintia
    public bool refillCorpses = true; // jos k�yt�ss� CorpseManagerin �t�ytt��

    public void SetCheckpoint()
    {
        var d = FindFirstObjectByType<DeathAndRespawn>();
        //if (d) d.SetSpawnPoint(spawnPoint ? spawnPoint.position : transform.position);

        if (refillCorpses)
        {
            var cm = FindFirstObjectByType<CorpseManager>();
            if (cm) cm.SendMessage("RefillAtCheckpoint", SendMessageOptions.DontRequireReceiver);
        }
        SoundManager.PlaySFX("checkpoint", 0.02f);
    }
}
