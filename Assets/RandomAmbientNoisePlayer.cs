using UnityEngine;

public class RandomAmbientNoisePlayer : MonoBehaviour
{
    public AudioClip[] ambientSounds; // Array to hold different ambient sound clips
    public float minTimeBetweenSounds = 5f; // Minimum time between sounds
    public float maxTimeBetweenSounds = 15f; // Maximum time between sounds
    public float soundVolume = 0.5f; // Volume of the sound

    private AudioSource audioSource;

    void Start()
    {
        // Set up the AudioSource if it's not already attached
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.volume = soundVolume;

        // Start playing ambient sounds
        PlayRandomAmbientSound();
    }

    void PlayRandomAmbientSound()
    {
        // Check if there are any sounds to play
        if (ambientSounds.Length == 0)
        {
            Debug.LogWarning("No ambient sounds assigned!");
            return;
        }

        // Randomly pick an ambient sound clip from the array
        int randomIndex = Random.Range(0, ambientSounds.Length);
        AudioClip randomSound = ambientSounds[randomIndex];

        // Play the selected sound
        audioSource.clip = randomSound;
        audioSource.Play();

        // Wait for a random duration before playing the next sound
        float randomWaitTime = Random.Range(minTimeBetweenSounds, maxTimeBetweenSounds);
        Invoke("PlayRandomAmbientSound", randomWaitTime);
    }
}
