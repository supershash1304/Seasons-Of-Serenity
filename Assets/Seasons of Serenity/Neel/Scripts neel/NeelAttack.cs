using UnityEngine;
using System.Collections;

public class NeelAttack : MonoBehaviour
{
    public Animator animator;
    public Transform attackSpawnPoint;
    public GameObject[] beamPrefabs;

    public float attackDelay = 1f;        // beam fire timing
    public float soundDelay = 0.5f;       // sound delay
    public float attackRange = 20f;
    public int attackDamage = 25;

    public LayerMask enemyLayer;

    [Header("Spread")]
    public float spreadAngle = 10f;

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

        // Play sound after delay
        StartCoroutine(PlayBeamSoundDelayed());

        // Wait for beam firing moment
        yield return new WaitForSeconds(attackDelay);

        // VFX
        if (selectedAttack > 0 && selectedAttack <= beamPrefabs.Length && beamPrefabs[selectedAttack - 1] != null)
        {
            GameObject beam = Instantiate(
                beamPrefabs[selectedAttack - 1],
                attackSpawnPoint.position,
                Quaternion.LookRotation(transform.forward)
            );
            Destroy(beam, 3f);
        }

        // ---------- DAMAGE (3-ray spread) ----------
        Vector3 origin = attackSpawnPoint.position;

        Vector3 centerDir = transform.forward;
        Vector3 leftDir = Quaternion.Euler(0, -spreadAngle, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, spreadAngle, 0) * transform.forward;

        RaycastHit hit;

        if (TryHit(origin, centerDir, out hit) ||
            TryHit(origin, leftDir, out hit) ||
            TryHit(origin, rightDir, out hit))
        {
            // ✅ Monsters
            MonsterHealth mh = hit.collider.GetComponentInParent<MonsterHealth>();
            if (mh != null)
            {
                mh.TakeDamage(attackDamage);
                isAttacking = false;
                yield break;
            }

            // ✅ Bosses
            EnemyHealth eh = hit.collider.GetComponentInParent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(attackDamage);
                isAttacking = false;
                yield break;
            }

            Debug.LogWarning("Raycast hit " + hit.collider.name + " but no MonsterHealth/EnemyHealth found.");
        }

        isAttacking = false;
    }

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
            attackSpawnPoint.position + (Quaternion.Euler(0, -spreadAngle, 0) * transform.forward) * attackRange);

        Gizmos.DrawLine(attackSpawnPoint.position,
            attackSpawnPoint.position + (Quaternion.Euler(0, spreadAngle, 0) * transform.forward) * attackRange);
    }
}
