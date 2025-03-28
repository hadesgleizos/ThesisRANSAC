using UnityEngine;
using UnityEngine.InputSystem;

public class playerMovement : MonoBehaviour
{
    public float speed = 10f;
    private Rigidbody rb;
    private Vector2 moveVector;

    public float attackRange = 2.0f;
    public LayerMask zombieLayer;
    public Transform playerCamera;

    private PlayerPerformance playerPerformance;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerPerformance = FindObjectOfType<PlayerPerformance>();
    }

    private void FixedUpdate()
    {
        // Move the movement code to FixedUpdate for more consistent physics
        Vector3 movement = new Vector3(moveVector.x, 0.0f, moveVector.y);
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    private void Update()
    {
        // Keep the attack input check in Update
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //Debug.Log("Attacking");
            AttackZombie();
        }
    }

    public void OnMove(InputValue movementValue)
    {
        moveVector = movementValue.Get<Vector2>();
        //Debug.Log("Move Input: " + moveVector); // Add this debug log
    }

    // Method to destroy zombies within a certain radius of the player
    void AttackZombie()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, zombieLayer);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Zombie"))
            {
                // Notify the spawner to remove this zombie from its list
                Spawner spawner = FindObjectOfType<Spawner>();
                if (spawner != null)
                {
                    spawner.RemoveZombie(hitCollider.gameObject); // Remove the zombie from the spawner's list
                }

                Destroy(hitCollider.gameObject); // Destroy the zombie
                //Debug.Log("Zombie destroyed!");

                // Increment the kill count
                if (playerPerformance != null) // Ensure the PlayerPerformance component was found
                {
                    playerPerformance.ZombieKilled(); // Call the method to increment the kill count
                }
            }
        }
    }

    // Gizmos for visualizing the attack range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; // Set the color of the gizmo
        Gizmos.DrawWireSphere(transform.position, attackRange); // Draw a wire sphere
    }
}
