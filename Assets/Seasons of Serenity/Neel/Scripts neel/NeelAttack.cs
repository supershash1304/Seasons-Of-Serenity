using UnityEngine;
using System.Collections;

public class NeelAttack : MonoBehaviour
{
    public Animator animator;
    public Transform attackSpawnPoint;
    public GameObject[] beamPrefabs;

    public float attackDelay = 1f;          // beam fire timing
    public float soundDelay = 0.5f;          // 🔊 NEW: sound delay
    public float attackRange = 20f;
    public int attackDamage = 25;

    public LayerMask enemyLayer;

    private int selectedAttack = 0;
    private bool isAttacking = false;
    private bool TryHit(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, attackRange, enemyLayer);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedAttack = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedAttack = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedAttack = 3;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedAttack = 4;

        if (Input.GetMouseButtonDown(0) && selectedAttack > 0 && !isAttacking)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        isAttacking = true;

        animator.SetTrigger("AttackTrigger");

        // 🔊 play sound after 0.5 seconds
        StartCoroutine(PlayBeamSoundDelayed());

        // wait for beam fire timing
        yield return new WaitForSeconds(attackDelay);

        // VFX
        if (selectedAttack > 0 && selectedAttack <= beamPrefabs.Length)
        {
            GameObject beam = Instantiate(
                beamPrefabs[selectedAttack - 1],
                attackSpawnPoint.position,
                Quaternion.LookRotation(transform.forward)
            );
            Destroy(beam, 3f);
        }

        // Damage
        Vector3 origin = attackSpawnPoint.position;

        // directions
        Vector3 centerDir = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -10f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, 10f, 0) * transform.forward;

        RaycastHit hit;

        // Try center
        if (TryHit(origin, centerDir, out hit) ||
            TryHit(origin, leftDir, out hit) ||
            TryHit(origin, rightDir, out hit))
        {
            MonsterHealth mh = hit.collider.GetComponent<MonsterHealth>()
                             ?? hit.collider.GetComponentInParent<MonsterHealth>();

            if (mh != null)
                mh.TakeDamage(attackDamage);
        }


        isAttacking = false;
    }

    // ----------------------------
    // 🔊 SOUND DELAY HANDLER
    // ----------------------------
    private IEnumerator PlayBeamSoundDelayed()
    {
        yield return new WaitForSeconds(soundDelay);
        AudioManager.Instance?.PlayBeamAttack();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackSpawnPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(attackSpawnPoint.position,
            attackSpawnPoint.position + transform.forward * attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(attackSpawnPoint.position,
            attackSpawnPoint.position + (Quaternion.Euler(0, -10f, 0) * transform.forward) * attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(attackSpawnPoint.position,
            attackSpawnPoint.position + (Quaternion.Euler(0, 10f, 0) * transform.forward) * attackRange);
    }

}
