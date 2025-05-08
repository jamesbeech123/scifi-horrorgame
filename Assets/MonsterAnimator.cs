using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAnimator : MonoBehaviour
{
    private Vector3 monsterPosition;
    private UnityEngine.AI.NavMeshAgent agent;
    private Animator animator;
    private MonsterAudio monsterAudio;

    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        monsterPosition = gameObject.transform.position;
        agent = gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    void FixedUpdate()
    {
        float speed = agent.velocity.magnitude;

        // Update the animation parameter
        animator.SetFloat("Speed", speed);
    }

    public void Scream()
    {
        animator.SetTrigger("Scream");
        monsterAudio.PlayAttackSound();
    }
}