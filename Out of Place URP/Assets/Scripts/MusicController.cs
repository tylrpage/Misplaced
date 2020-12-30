using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicController : MonoBehaviour
{
    private AudioSource _musicPlayer;
    private bool _playing = false;
    
    // Start is called before the first frame update
    void Start()
    {
        _musicPlayer = GetComponent<AudioSource>();
    }

    public void Play()
    {
        _musicPlayer.Play();
    }

    public void Pause()
    {
        _musicPlayer.Pause();
    }
}
