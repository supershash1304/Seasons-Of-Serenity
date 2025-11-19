using UnityEngine;

public class NeelAttack : MonoBehaviour
{
    public Animator animator;
    public Transform attackSpawnPoint;
    public GameObject[] beamPrefabs; // Beam1 to Beam4

    public float attackDelay = 1f;        // Delay after animation trigger
    public float attackRange = 5f;        // Raycast range
    public int attackDamage = 10;         // Damage per beam

    public LayerMask enemyLayer;          // Assign "Boss" layer in Inspector
    private int selectedAttack = 0;       // 0 = none, 1–4 = active beam
    private bool isAttacking = false;

    void Update()
    {
        // Switch beam type
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedAttack = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedAttack = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedAttack = 3;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedAttack = 4;

        // Left-click triggers beam cast if one is selected
        if (Input.GetMouseButtonDown(0) && selectedAttack > 0 && !isAttacking)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        isAttacking = true;

        // Trigger attack animation
        animator.SetTrigger("AttackTrigger");

        // Wait for attack animation wind-up
        yield return new WaitForSeconds(attackDelay);

        // Instantiate beam VFX
        GameObject beam = Instantiate(
            beamPrefabs[selectedAttack - 1],
            attackSpawnPoint.position,
            Quaternion.LookRotation(transform.forward)
        );
        Destroy(beam, 3f);

        // Raycast for hitting boss
        if (Physics.Raycast(attackSpawnPoint.position, transform.forward, out RaycastHit hit, attackRange, enemyLayer))
        {
            FinalBossController boss = hit.collider.GetComponent<FinalBossController>();
            if (boss != null)
            {
                boss.ReceiveDamage(attackDamage);
                Debug.Log("Boss hit with beam. Damage applied.");
            }
        }

        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize raycast range
        if (attackSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(attackSpawnPoint.position, attackSpawnPoint.position + transform.forward * attackRange);
        }
    }
}
