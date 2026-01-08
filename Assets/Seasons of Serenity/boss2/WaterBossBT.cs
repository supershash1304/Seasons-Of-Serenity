using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaterBossBT : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("Ranges")]
    public float detectionRadius = 15f;
    public float meleeRange = 2.7f;
    public float spellRange = 10f; // cast when player is within this, but not in melee

    [Header("Combat")]
    public float attackCooldown = 1.5f;
    public float spellCooldown = 4f;
    public int meleeDamage = 12;
    public int spellDamage = 18;

    [Header("Raycast Damage")]
    public Transform attackOrigin;          // set to hand / chest / mouth
    public float rayDistance = 3f;
    public LayerMask playerLayer;

    [Header("Patrol")]
    public float patrolRadius = 6f;
    public float patrolWait = 2f;

    [Header("Animator Params (match your Animator)")]
    public string speedParam = "Speed";          // float
    public string attackRTrigger = "AttackR";    // trigger
    public string attackLTrigger = "AttackL";    // trigger
    public string castTrigger = "CastSpell";     // trigger
    public string hitTrigger = "Hit";            // trigger (optional)
    public string deathTrigger = "Death";        // trigger

    [Header("Debug")]
    public bool debug = true;

    // ✅ Boss health comes from EnemyHealth
    private EnemyHealth health;

    private float nextAttackTime;
    private float nextSpellTime;

    private Vector3 patrolTarget;
    private float patrolWaitTimer;

    // ---------------- BT ----------------
    private Node root;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (health == null)
            Debug.LogWarning("[WaterBossBT] EnemyHealth missing on boss. Add EnemyHealth to Water Boss root.");

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        BuildTree();
    }

    private void Update()
    {
        if (health != null && health.IsDead()) return;

        if (root == null) return;
        root.Tick();

        // drive locomotion animation by actual movement
        if (agent != null && animator != null)
        {
            float spd01 = agent.velocity.magnitude > 0.05f ? 1f : 0f;
            animator.SetFloat(speedParam, spd01);
        }
    }

    // ---------------- Called by EnemyHealth via SendMessage ----------------
    public void OnHit()
    {
        if (health != null && health.IsDead()) return;

        if (animator != null && !string.IsNullOrEmpty(hitTrigger))
            animator.SetTrigger(hitTrigger);

        if (debug) Debug.Log("[WaterBossBT] OnHit()");
    }

    public void OnDeath()
    {
        if (debug) Debug.Log("[WaterBossBT] OnDeath()");

        if (agent != null) agent.isStopped = true;

        if (animator != null && !string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);

        // Disable AI
        enabled = false;
    }

    // ---------------- Behavior Tree ----------------
    private void BuildTree()
    {
        // Priority:
        // 1) Dead
        // 2) Melee if close + cooldown ready
        // 3) Cast spell if in spell range + cooldown ready
        // 4) Chase if detected
        // 5) Patrol/Idle

        root = new Selector(
            new Sequence(
                new Condition(IsDead),
                new ActionNode(StopAll)
            ),

            new Sequence(
                new Condition(PlayerInMeleeRange),
                new Condition(MeleeReady),
                new ActionNode(DoMeleeAttack)
            ),

            new Sequence(
                new Condition(PlayerInSpellRange),
                new Condition(SpellReady),
                new ActionNode(DoCastSpell)
            ),

            new Sequence(
                new Condition(PlayerDetected),
                new ActionNode(ChasePlayer)
            ),

            new ActionNode(Patrol)
        );
    }

    // ---------------- Conditions ----------------
    private bool IsDead()
    {
        return health != null && health.IsDead();
    }

    private bool PlayerDetected()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= detectionRadius;
    }

    private bool PlayerInMeleeRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= meleeRange;
    }

    private bool PlayerInSpellRange()
    {
        if (player == null) return false;
        float d = Vector3.Distance(transform.position, player.position);
        return d <= spellRange && d > meleeRange;
    }

    private bool MeleeReady() => Time.time >= nextAttackTime;
    private bool SpellReady() => Time.time >= nextSpellTime;

    // ---------------- Actions ----------------
    private NodeState StopAll()
    {
        if (agent != null) agent.isStopped = true;
        return NodeState.Success;
    }

    private NodeState ChasePlayer()
    {
        if (player == null || agent == null) return NodeState.Failure;
        if (!agent.isOnNavMesh) return NodeState.Failure;

        agent.isStopped = false;
        agent.stoppingDistance = meleeRange - 0.2f;
        agent.SetDestination(player.position);

        FacePlayerSmooth();

        return NodeState.Running;
    }

    private NodeState DoMeleeAttack()
    {
        if (agent != null) agent.isStopped = true;

        FacePlayerSnap();

        nextAttackTime = Time.time + attackCooldown;

        bool right = UnityEngine.Random.value > 0.5f;
        if (animator != null)
            animator.SetTrigger(right ? attackRTrigger : attackLTrigger);

        ApplyRaycastDamage(meleeDamage);

        if (debug) Debug.Log($"[WaterBossBT] MELEE {(right ? "R" : "L")}");

        return NodeState.Success;
    }

    private NodeState DoCastSpell()
    {
        if (agent != null) agent.isStopped = true;

        FacePlayerSnap();

        nextSpellTime = Time.time + spellCooldown;

        if (animator != null)
            animator.SetTrigger(castTrigger);

        ApplyRaycastDamage(spellDamage);

        if (debug) Debug.Log("[WaterBossBT] CAST SPELL");

        return NodeState.Success;
    }

    private NodeState Patrol()
    {
        if (agent == null) return NodeState.Failure;
        if (!agent.isOnNavMesh) return NodeState.Failure;

        if (PlayerDetected()) return NodeState.Failure;

        agent.isStopped = false;
        agent.stoppingDistance = 0f;

        if (patrolTarget == Vector3.zero || Vector3.Distance(transform.position, patrolTarget) < 1f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolWait)
            {
                patrolWaitTimer = 0f;
                patrolTarget = GetRandomNavPoint(transform.position, patrolRadius);
                agent.SetDestination(patrolTarget);

                if (debug) Debug.Log("[WaterBossBT] Patrol -> " + patrolTarget);
            }
            return NodeState.Running;
        }

        if (!agent.hasPath)
            agent.SetDestination(patrolTarget);

        return NodeState.Running;
    }

    // ---------------- Helpers ----------------
    private void FacePlayerSmooth()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
    }

    private void FacePlayerSnap()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private void ApplyRaycastDamage(int dmg)
    {
        if (player == null) return;

        Vector3 origin = (attackOrigin != null) ? attackOrigin.position : (transform.position + Vector3.up * 1.6f);
        Vector3 dir = (player.position - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, playerLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(dmg);
            }
        }
    }

    private Vector3 GetRandomNavPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 rand = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 p = new Vector3(center.x + rand.x, center.y, center.z + rand.y);

            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }
        return center;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0.6f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, spellRange);
    }

    // ---------------- Minimal BT framework ----------------
    private enum NodeState { Success, Failure, Running }

    private abstract class Node
    {
        public abstract NodeState Tick();
    }

    private class Selector : Node
    {
        private readonly List<Node> children;
        public Selector(params Node[] nodes) => children = new List<Node>(nodes);

        public override NodeState Tick()
        {
            foreach (var c in children)
            {
                var s = c.Tick();
                if (s == NodeState.Success) return NodeState.Success;
                if (s == NodeState.Running) return NodeState.Running;
            }
            return NodeState.Failure;
        }
    }

    private class Sequence : Node
    {
        private readonly List<Node> children;
        public Sequence(params Node[] nodes) => children = new List<Node>(nodes);

        public override NodeState Tick()
        {
            foreach (var c in children)
            {
                var s = c.Tick();
                if (s == NodeState.Failure) return NodeState.Failure;
                if (s == NodeState.Running) return NodeState.Running;
            }
            return NodeState.Success;
        }
    }

    private class Condition : Node
    {
        private readonly Func<bool> predicate;
        public Condition(Func<bool> predicate) => this.predicate = predicate;
        public override NodeState Tick() => predicate() ? NodeState.Success : NodeState.Failure;
    }

    private class ActionNode : Node
    {
        private readonly Func<NodeState> action;
        public ActionNode(Func<NodeState> action) => this.action = action;
        public override NodeState Tick() => action();
    }
}
