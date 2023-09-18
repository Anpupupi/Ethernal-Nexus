using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bot : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public int damageOnTouch;
    public bool willDie = true;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0 && willDie)
        {
            Die();
        }

    }
    private void Die()
    {
        // Perform any death-related actions here (e.g., play death animation, drop items, etc.)
        Destroy(transform.parent.gameObject); // Destroy the bot GameObject when it dies
        
    }
    public int getHealth()
    {
        return this.currentHealth;
    }
}
