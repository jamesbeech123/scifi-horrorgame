using UnityEngine;
using UnityEngine.AI;

public class MonsterAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource footstepSource;
    public AudioSource actionSource;

    [Header("Audio Clips")]
    public AudioClip[] footstepSounds;   // Different footstep sounds
    public AudioClip attackSound;        // Sound when attacking
    public AudioClip[] idleSounds;       // Random growls or roars

    [Header("Settings")]
    public float footstepInterval = 0.5f; // Time between footsteps
    public float idleSoundIntervalMin = 5f;
    public float idleSoundIntervalMax = 15f;

    private NavMeshAgent agent;
    private Animator animator;
    private float footstepTimer;
    private float idleSoundTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Set a random idle sound timer
        idleSoundTimer = Random.Range(idleSoundIntervalMin, idleSoundIntervalMax);
    }

    void Update()
    {
        HandleFootsteps();
        HandleIdleSounds();
    }

    private void HandleFootsteps()
    {
        if (agent.velocity.magnitude > 0.1f) // If moving
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayRandomFootstep();
                footstepTimer = footstepInterval; // Reset timer
            }
        }
    }

    private void HandleIdleSounds()
    {
        idleSoundTimer -= Time.deltaTime;
        if (idleSoundTimer <= 0f)
        {
            PlayRandomIdleSound();
            idleSoundTimer = Random.Range(idleSoundIntervalMin, idleSoundIntervalMax); // Reset timer
        }
    }

    public void PlayAttackSound()
    {
        if (attackSound && actionSource)
        {
            actionSource.PlayOneShot(attackSound);
        }
    }

    private void PlayRandomFootstep()
    {
        if (footstepSounds.Length > 0 && footstepSource)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            footstepSource.PlayOneShot(clip);
        }
    }

    private void PlayRandomIdleSound()
    {
        if (idleSounds.Length > 0 && actionSource)
        {
            AudioClip clip = idleSounds[Random.Range(0, idleSounds.Length)];
            actionSource.PlayOneShot(clip);
        }
    }
}
