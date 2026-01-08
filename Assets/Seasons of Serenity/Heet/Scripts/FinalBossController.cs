using UnityEngine;
using UnityEngine.AI;

public class FinalBossController : MonoBehaviour
{
    [Header("Player & Detection")]
    public Transform player;
    public float detectionRadius = 10f;
    public float attackRange = 3f;

    [Header("NavMesh Movement")]
    public float repathRate = 0.1f;
    private float repathTimer;

    [Header("Attack Cooldown")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime;

    [Header("4 Attacks (ONE animation, different damage)")]
    public int attack1Damage = 5;
    public int attack2Damage = 8;
    public int attack3Damage = 12;
    public int attack4Damage = 16;

    [Header("Raycast")]
    public LayerMask playerLayer;

    [Header("Animator Params")]
    public string speedParam = "Speed";
    public string attackTrigger = "Attack";
    public string dieTrigger = "Die"; // change if your animator uses a different trigger

    [Header("Debug")]
    public bool debugLogs = true;
    public float repeatWarnSeconds = 3f;

    private Animator animator;
    private NavMeshAgent agent;

    // ✅ Use EnemyHealth as the ONLY health system
    private EnemyHealth enemyHealth;
    private bool deathHandled = false;

    // Matrix + RL
    private DecisionNode[,] matrix;
    private DecisionNode currentNode;
    private DecisionEdge lastAction;

    // Debug trackers
    private string lastAttackName = "";
    private float sameAttackAccumTime = 0f;

    // Debug accessors (for visualizer)
    public string LastChosenAttackNameForDebug { get; private set; } = "none";
    public Vector2Int PreviousVertexForDebug { get; private set; } = new Vector2Int(-1, -1);
    public Vector2Int CurrentVertexForDebug => currentNode != null ? currentNode.Position : new Vector2Int(-1, -1);

    public float[] GetCurrentNodeWeightsForDebug()
    {
        if (currentNode == null || currentNode.Edges == null) return null;

        float w1 = GetWeightFromNode(currentNode, "Attack1");
        float w2 = GetWeightFromNode(currentNode, "Attack2");
        float w3 = GetWeightFromNode(currentNode, "Attack3");
        float w4 = GetWeightFromNode(currentNode, "Attack4");

        return new float[] { w1, w2, w3, w4 };
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null) LogError("Player not found. Tag player as 'Player'.");
        if (agent == null) LogError("NavMeshAgent missing.");
        if (animator == null) LogError("Animator missing.");
        if (enemyHealth == null) LogError("EnemyHealth missing on boss! Add EnemyHealth component.");

        if (agent != null)
            agent.stoppingDistance = Mathf.Max(attackRange - 0.5f, 0.1f);

        // Ensure the boss is on NavMesh
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Log($"Warped boss onto NavMesh at {hit.position}");
            }
            else
            {
                LogError("No NavMesh found near boss spawn!");
            }
        }

        InitializeMatrix();
        currentNode = matrix[0, 0];
        PreviousVertexForDebug = currentNode.Position;

        Log($"Initialized matrix. Starting vertex = {currentNode.Position}");
    }

    private void Update()
    {
        if (player == null || agent == null || animator == null || enemyHealth == null) return;

        // ✅ Death is driven by EnemyHealth
        if (enemyHealth.IsDead())
        {
            HandleDeathOnce();
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > detectionRadius)
        {
            Idle();
            return;
        }

        if (dist <= attackRange)
            StopAndAttack();
        else
            ChasePlayerContinuous();
    }

    private void HandleDeathOnce()
    {
        if (deathHandled) return;
        deathHandled = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetFloat(speedParam, 0f);
            animator.SetTrigger(dieTrigger);
        }

        Log("Boss died (EnemyHealth) -> stopping AI + triggering Die");

        // Stop this controller so it won’t keep running
        enabled = false;
    }

    private void ChasePlayerContinuous()
    {
        if (!agent.isOnNavMesh)
        {
            LogError("Boss is NOT on NavMesh. Move boss onto baked NavMesh.");
            return;
        }

        agent.isStopped = false;
        animator.SetFloat(speedParam, 1f);

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathRate;
            agent.SetDestination(player.position);

            Log($"CHASE: dist={Vector3.Distance(transform.position, player.position):0.00} " +
                $"hasPath={agent.hasPath} status={agent.pathStatus} vel={agent.velocity.magnitude:0.00}");
        }
    }

    private void StopAndAttack()
    {
        agent.isStopped = true;
        animator.SetFloat(speedParam, 0f);

        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        if (currentNode != null)
            PreviousVertexForDebug = currentNode.Position;

        DecisionNode fromNode = currentNode;
        lastAction = currentNode.GetRandomEdge();
        LastChosenAttackNameForDebug = lastAction != null ? lastAction.ActionName : "none";

        if (lastAction == null)
        {
            LogError($"No edges available at vertex {currentNode.Position}");
            return;
        }

        DecisionNode toNode = lastAction.TargetNode;
        currentNode = toNode;

        Log($"DECISION: {fromNode.Position} -> '{lastAction.ActionName}' (w={lastAction.Weight:0.00}) -> {toNode.Position}");

        if (lastAction.ActionName == lastAttackName)
        {
            sameAttackAccumTime += attackCooldown;
            if (sameAttackAccumTime >= repeatWarnSeconds)
                Log($"⚠ REPEAT: same attack '{lastAttackName}' for ~{sameAttackAccumTime:0.0}s");
        }
        else
        {
            if (!string.IsNullOrEmpty(lastAttackName))
                Log($"SWITCH: '{lastAttackName}' -> '{lastAction.ActionName}'");

            lastAttackName = lastAction.ActionName;
            sameAttackAccumTime = 0f;
        }

        animator.SetTrigger(attackTrigger);
        DoRaycastDamage();
    }

    private void Idle()
    {
        agent.isStopped = true;
        animator.SetFloat(speedParam, 0f);
    }

    private void DoRaycastDamage()
    {
        string atk = lastAction != null ? lastAction.ActionName : "Attack1";

        int dmg = attack1Damage;
        switch (atk)
        {
            case "Attack1": dmg = attack1Damage; break;
            case "Attack2": dmg = attack2Damage; break;
            case "Attack3": dmg = attack3Damage; break;
            case "Attack4": dmg = attack4Damage; break;
        }

        Vector3 origin = transform.position + Vector3.up * 1.6f;
        Vector3 dir = (player.position - origin).normalized;

        bool hitPlayer = Physics.Raycast(origin, dir, out RaycastHit hit, attackRange, playerLayer) &&
                         hit.collider.CompareTag("Player");

        if (hitPlayer)
        {
            hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(dmg);
            ApplyFeedback(true);
            Log($"HIT: {atk} dealt {dmg} damage");
        }
        else
        {
            ApplyFeedback(false);
            Log($"MISS: {atk}");
        }
    }

    // ✅ IMPORTANT: external damage should go through EnemyHealth
    public void ReceiveDamage(int dmg)
    {
        if (enemyHealth == null || enemyHealth.IsDead()) return;
        enemyHealth.TakeDamage(dmg);
    }

    // EnemyHealth.SendMessage("OnHit") will call this if you want
    private void OnHit()
    {
        // Optional: play hit reaction logic here (or just animator triggers in another script)
    }

    // EnemyHealth.SendMessage("OnDeath") will call this too,
    // but Update() already handles death. Keeping it is harmless.
    private void OnDeath()
    {
        HandleDeathOnce();
    }

    private void ApplyFeedback(bool hitSuccess)
    {
        if (lastAction == null) return;

        float before = lastAction.Weight;
        float delta = hitSuccess ? 0.5f : -0.3f;

        lastAction.AdjustWeight(delta);

        Log($"RL: {lastAction.ActionName} {(hitSuccess ? "HIT ✅" : "MISS ❌")} | w {before:0.00} -> {lastAction.Weight:0.00}");
    }

    private void InitializeMatrix()
    {
        int size = 4;
        matrix = new DecisionNode[size, size];

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                matrix[x, y] = new DecisionNode(x, y);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                DecisionNode node = matrix[x, y];

                if (x < size - 1) node.AddEdge(Direction.Right, new DecisionEdge("Attack1", 1f, matrix[x + 1, y]));
                if (x > 0) node.AddEdge(Direction.Left, new DecisionEdge("Attack2", 1f, matrix[x - 1, y]));
                if (y < size - 1) node.AddEdge(Direction.Up, new DecisionEdge("Attack3", 1f, matrix[x, y + 1]));
                if (y > 0) node.AddEdge(Direction.Down, new DecisionEdge("Attack4", 1f, matrix[x, y - 1]));
            }
        }
    }

    private float GetWeightFromNode(DecisionNode node, string actionName)
    {
        foreach (var e in node.Edges.Values)
            if (e.ActionName == actionName) return e.Weight;
        return 0f;
    }

    private void Log(string msg)
    {
        if (!debugLogs) return;
        Debug.Log($"[BOSS AI] {msg}");
    }

    private void LogError(string msg)
    {
        Debug.LogError($"[BOSS AI] {msg}");
    }
}
