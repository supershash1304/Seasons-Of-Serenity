using UnityEngine;
using System.Collections; // Required for IEnumerator

public class NeelAttack : MonoBehaviour
{
    public Animator animator;
    public Transform attackSpawnPoint;
    public GameObject[] beamPrefabs; // Beam1 to Beam4

    public float attackDelay = 1f;        // Delay after animation trigger
    public float attackRange = 5f;        // Raycast range
    public int attackDamage = 25;         // Damage per beam (25 points)

    public LayerMask enemyLayer;          // Assign the monster's layer in Inspector
    private int selectedAttack = 0;       // 0 = none, 1–4 = active beam
    private bool isAttacking = false;

    void Update()
    {
        // Switch beam type
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedAttack = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedAttack = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedAttack = 3;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedAttack = 4;

        // Left-click triggers beam cast if one is selected and not already attacking
        if (Input.GetMouseButtonDown(0) && selectedAttack > 0 && !isAttacking)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;

        // Trigger attack animation
        animator.SetTrigger("AttackTrigger");

        // Wait for attack animation wind-up (the moment the beam is supposed to fire)
        yield return new WaitForSeconds(attackDelay);

        // --- VFX Instantiate ---
        if (selectedAttack > 0 && selectedAttack <= beamPrefabs.Length)
        {
            GameObject beam = Instantiate(
                beamPrefabs[selectedAttack - 1],
                attackSpawnPoint.position,
                Quaternion.LookRotation(transform.forward)
            );
            Destroy(beam, 3f);
        }

        // --- Raycast for Hitting Monster and Applying Damage ---
        if (Physics.Raycast(attackSpawnPoint.position, transform.forward, out RaycastHit hit, attackRange, enemyLayer))
        {
            // The essential fix: Look for the dedicated MonsterHealth script
            MonsterHealth monsterHealth = hit.collider.GetComponent<MonsterHealth>();
            
            // If the health script wasn't found on the hit collider, check its parent (common for rigged models)
            if (monsterHealth == null)
            {
                monsterHealth = hit.collider.GetComponentInParent<MonsterHealth>();
            }

            if (monsterHealth != null)
            {
                // Call the TakeDamage function on the monster
                monsterHealth.TakeDamage(attackDamage);
                Debug.Log("SUCCESS! Applied " + attackDamage + " damage to monster: " + hit.collider.gameObject.name);
            }
            else
            {
                // Critical Debug: Raycast hit something, but it's not the correct enemy script
                Debug.LogWarning("Raycast hit object: " + hit.collider.gameObject.name + 
                                 ", but NO MonsterHealth component was found.");
            }
        }
        
        // Ensure the attack state ends
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize raycast range (only visible when the player object is selected in the Editor)
        if (attackSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(attackSpawnPoint.position, attackSpawnPoint.position + transform.forward * attackRange);
        }
    }
}
