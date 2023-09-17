using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class HealthStaminaSystem : MonoBehaviour
    {
        public UnityEngine.UI.Image healthBar;
        public UnityEngine.UI.Slider staminaBar;
        public float maxHealth = 100f;
        public float maxStamina = 100f;
        public float currentHealth;
        public float currentStamina;
        private float dValue = 5;
        public Bot bot;
        public GameOverScreen gameOverScreen;
        public GameObject gunHolder;
        private Shield shieldController;

        private float shieldHealth;
    public AudioClip hurtSound;
    //put more sound here
    void Start()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            staminaBar.maxValue = currentStamina;

            shieldController = GetComponent<Shield>();
            
            shieldHealth = shieldController.maxShieldHealth;
        }

        void Update()
        {
        bool isShieldActive = shieldController != null && shieldController.IsShieldActive();

        if (isShieldActive)
        {
            // Shield is active, deduct damage from shield health
            shieldHealth -= Time.deltaTime * 10f; // Example deduction rate
            if (shieldHealth <= 0)
            {
                shieldHealth = 0;
                shieldController.BreakShield();
            }
        }
        else
        {
            // Shield is not active, deduct health as usual
            healthBar.fillAmount = Mathf.Clamp(currentHealth / maxHealth, 0, 1);
            if (currentHealth <= 0)
            {
                gameObject.SetActive(false);
                gameOverScreen.GameOver();
                Debug.Log("Dead");
            }
        }

        //use stamina
        if (Input.GetButton("Sprint") && (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
            {
                DecreaseEnergy();
            }
            else if (currentStamina != maxStamina)
            {
                IncreaseEnergy();
            }
            staminaBar.value = currentStamina;

    }

        private void OnCollisionEnter(Collision collision) 
        {
        if (collision.gameObject.CompareTag("Enemy")) //if the player touch the enemies
        {
            Bot enemyBot = collision.gameObject.GetComponent<Bot>();
            if (enemyBot != null)
            {
                if (shieldController != null && shieldController.IsShieldActive())
                {
                    shieldController.TakeDamage(enemyBot.damageOnTouch); // Apply damage to shield
                }
                else
                {
                    TakeDamage(enemyBot.damageOnTouch); // Apply damage to health
                }
            }
        }
    }


    private void DecreaseEnergy()
    {
        if (currentStamina != 0)
        {
            currentStamina -= dValue * Time.deltaTime;
        }
        if (currentStamina <= -1)
        {
            currentStamina = 0;
        }
    }

    public void IncreaseEnergy()
    {
        currentStamina += dValue * Time.deltaTime;
        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
        }
    }

    public void TakeDamage(float damage) {
            currentHealth -= damage;
        SoundManager.Instance.PlaySoundByInterval(hurtSound,0.4f);
        //healthBar.SetHealth(currentHealth);
        if (currentHealth < 0f) {
                currentHealth = 0f;
            }
            Debug.Log("Player health: "+ currentHealth);
            
        }

    public void TakeShieldDamage(float damage)
    {
        shieldHealth -= damage;
        if (shieldHealth <= 0)
        {
            shieldHealth = 0;
            shieldController.BreakShield();
        }
    }

    public void RecoverHealth(float healing) {
            currentHealth += healing;
            //healthBar.SetHealth(currentHealth);
            if (currentHealth>100f) {
                currentHealth = 100f;
            }
            Debug.Log("Player health: "+ currentHealth);
        }
    }