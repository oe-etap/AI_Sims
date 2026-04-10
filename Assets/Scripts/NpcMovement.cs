using GLTFast.Schema;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AiSims
{
    public class NpcMovement : MonoBehaviour
    {
        private Transform currentTarget;
        protected Vector3 positionLastTarget;
        public Transform player;
        public bool automaticStart = true;
        private NavMeshAgent navMeshAgent = null;
        public bool isFollowingPlayer = true;
        public bool goClose = false;
        public List<Transform> targets = new List<Transform>();
        private float RunAwayTimer = 0f;
        private float RunAwayInterval = 20f;
        private float navSpeed = 0f;

        public string endAnimation;
        private string walkingAnimation = "isWalking";
        public string target;

        private Animator animator = null;
        private bool running = false;

        private string startAnimationName;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public void Start()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>(); // Use Stop() and Resume()

            if (automaticStart)
            {
                running = true;
                StartMovement();
            }
        }

        public virtual void StartMovement()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();

            running = true;

            navSpeed = navMeshAgent.speed;

            animator.SetBool(walkingAnimation, true);
            animator.SetBool(endAnimation, false);

            if (startAnimationName != string.Empty)
            {
                animator.SetBool(startAnimationName, false);
            }

            chooseTarget();
        }


        public void SetAnimation(string animationName)
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            animator.SetBool(animationName, true);
            startAnimationName = animationName;
        }

        // Update is called once per frame
        public virtual void Update()
        {
            if (!running) return;

            if (player != null && goClose == true)
            {
                MoveAgentNearPlayer(navMeshAgent, player, 1f);
            }
            else
            {
                navMeshAgent.SetDestination(currentTarget.position);
            }
        }

        public void CheckUpdate()
        {
            if (Vector3.Distance(positionLastTarget, player.position) > 1f)
            {
                StartMovement();
                StartChasingTarget();
                MoveAgentNearPlayer(navMeshAgent, player, 1f);
            }
        }

        public virtual void StartChasingTarget(int speed = 2)
        {
            navMeshAgent.speed = speed;
        }

        public void chooseTarget()
        {
            if (targets.Count <= 1)
            {
                currentTarget = targets[0];
                positionLastTarget = currentTarget.position;
                return;
            }

            int newTargetIndex = UnityEngine.Random.Range(0, targets.Count);
            Transform NewTarget = targets[newTargetIndex];
            if (NewTarget == currentTarget)
            {
                chooseTarget();                
            }
            else
            {
                currentTarget = NewTarget;
            }
            StartChasingTarget();
            positionLastTarget = currentTarget.position;
        }

        public void MoveAgentNearPlayer(NavMeshAgent agent, Transform player, float stopDistance = 1f)
        {
            // Direction from agent to player (ignore vertical differences)
            Vector3 direction = player.position - agent.transform.position;
            direction.y = 0f;
            direction.Normalize();

            // Target point 1 meter away from player
            Vector3 targetPos = player.position - direction * stopDistance;

            agent.SetDestination(targetPos);
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag(target) && isFollowingPlayer == false)
            {
                navMeshAgent.speed = 0;
                animator.SetBool(endAnimation, true);
                animator.SetBool(walkingAnimation, false);
            }
            else
            {
                chooseTarget();
            }
        }
    }
}