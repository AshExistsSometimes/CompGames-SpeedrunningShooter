using UnityEngine;
using UnityEngine.Events;

public class Switch : MonoBehaviour, IDamagable
{
    public bool SwitchON = false;

    public UnityEvent SwitchOnEvent;
    public UnityEvent SwitchOffEvent;

    public Color OnColour = Color.green;
    public Color OffColour = Color.red;

    private Renderer renderer;
    private Color originalColour;
    private float switchChangeCooldown = 0.1f;
    private bool canBeSwitched = true;

    private void Start()
    {
        renderer = GetComponentInChildren<Renderer>();
        originalColour = renderer.material.color;

        UpdateColour(SwitchON);
    }
    public void TakeDamage(int damage)
    {
        // Do nothing with damage

        if (SwitchON && canBeSwitched) { SwitchON = false; canBeSwitched = false; SwitchAction(); Cooldown(); }// Turn off Switch
        if (!SwitchON && canBeSwitched) { SwitchON = true; canBeSwitched = false; SwitchAction(); Cooldown(); }// Turn on Switch

        UpdateColour(SwitchON);
    }

    public void UpdateColour(bool SwitchState)
    {
        if (SwitchState) { renderer.material.color = OnColour; }
        else { renderer.material.color = OffColour; }
    }

    public void SwitchAction()
    {
        if (SwitchON) { SwitchOnEvent.Invoke(); }
        if (!SwitchON) { SwitchOffEvent.Invoke(); }
    }

    public void Die()
    {
        // lol no
    }

    public void Cooldown()
    {
        Invoke("FinishCooldown", switchChangeCooldown);
    }

    public void FinishCooldown()
    {
        canBeSwitched = true;
    }
}
