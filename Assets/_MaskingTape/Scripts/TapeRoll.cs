using System.Collections.Generic;
using UnityEngine;

public class TapeRoll : MonoBehaviour
{
    [SerializeField] float tapeMoveSpeed = 1f;
    [SerializeField] FMODUnity.EventReference tapeRollSound;
    public GameObject roll; // The visual representation of the tape roll
    public Transform tapeSpawnLoc; //Location on the visual representation to spawn the new "jagged" tape piece from
    public int yLimit = -5; //The lowest y position the tape roll can go to
    public int yCounter = 0; //Represents how far up the tape roll has moved from its starting position
    private FMOD.Studio.EventInstance tapeRollSoundInstance;

    public void MoveTape(float moveDelta)
    {
        if(!tapeRollSound.IsNull)
        {
            if(!tapeRollSoundInstance.isValid())
            {
                tapeRollSoundInstance = FMODUnity.RuntimeManager.CreateInstance(tapeRollSound.Guid);
            }
            FMOD.Studio.PLAYBACK_STATE rollSoundState;
            tapeRollSoundInstance.getPlaybackState(out rollSoundState);
            if(rollSoundState != FMOD.Studio.PLAYBACK_STATE.PLAYING)
            {
                tapeRollSoundInstance.start();
            }
            else
            {
                tapeRollSoundInstance.setParameterByName("RollingState", 1);
            }

        }
        Vector3 newStartPoint = new Vector3(roll.transform.position.x, roll.transform.position.y + moveDelta * tapeMoveSpeed, roll.transform.position.z);
        roll.transform.position = newStartPoint;
    }

    public Transform GetTapeRollTopPieceLocation()
    {
        return tapeSpawnLoc;
    }
    private void OnDestroy()
    {
        if(tapeRollSoundInstance.isValid())
        {
            tapeRollSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            tapeRollSoundInstance.release();
        }

        
    }
}