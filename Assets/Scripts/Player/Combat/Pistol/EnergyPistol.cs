using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnergyPistol : MonoBehaviour
{
    [Header("Gun Components")]
    public GameObject Pistol;
    public GameObject Drum;
    public Transform FirePoint;

    [Header("Heat Settings")]
    public float Heat = 0f;
    public float MaxHeat = 100f;
    public float OverheatSpeed = 10f;
    public float CooldownTime = 2f;
    public float CooldownRate = 10f;
    public Slider HeatSlider;

    [Header("Charge Settings")]
    public float Charge = 0f;
    public float MaxCharge = 100f;
    public float ChargeSpeed = 10f;

    [Header("Damage Settings")]
    public float DefaultDamage = 5f;
    private float DamageToDeal = 5f;

    // Internal state tracking
    private float lastFiredTime = 0f;
    public bool isOverheated = false;
    private bool isCharging = false;
    private bool overheatedDuringCharge = false;
    private Coroutine recoilRoutine;
    private Coroutine drumRoutine;
    private Coroutine overheatReturnRoutine;

    void Update()
    {
        HeatSlider.value = Heat / 100;

        // Handle firing input when not overheated or mid-charge
        if (Input.GetMouseButtonDown(0))
        {
            if (!isOverheated)
                isCharging = true;
        }

        // While charging, accumulate charge and heat
        if (Input.GetMouseButton(0) && isCharging)
        {
            Charge = Mathf.Min(Charge + ChargeSpeed * Time.deltaTime, MaxCharge);
            Heat = Mathf.Min(Heat + OverheatSpeed * Time.deltaTime, MaxHeat);
            SpinDrum();

            // If overheated while charging, note it but allow shot to finish
            if (Heat >= MaxHeat && !overheatedDuringCharge)
            {
                Heat = MaxHeat;
                overheatedDuringCharge = true;
            }
        }

        // On release, fire shot (even if overheated mid-charge)
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;
            ChargeFire();
        }

        // Passive cooldown when idle
        if (!isCharging && (Time.time - lastFiredTime) > CooldownTime)
        {
            Heat = Mathf.Max(Heat - CooldownRate * Time.deltaTime, 0f);
        }

        // Apply full overheat lockout only after charge fire completes
        if (!isCharging && !overheatedDuringCharge && Heat >= MaxHeat)
        {
            Heat = MaxHeat;
            isOverheated = true;
        }

        // Recover from overheat
        if (isOverheated)
        {
            CooldownOverheat();
        }
    }

    // Handles full overheat recovery and resets pistol rotation
    private void CooldownOverheat()
    {
        Heat = Mathf.Max(Heat - CooldownRate * Time.deltaTime, 0f);
        if (Pistol.transform.localRotation != Quaternion.identity)
        {
            if (overheatReturnRoutine != null) StopCoroutine(overheatReturnRoutine);
            overheatReturnRoutine = StartCoroutine(ReturnPistolToNeutral());
        }

        if (Heat <= 0f)
        {
            Heat = 0f;
            isOverheated = false;
        }
    }

    // Fires a single non-charged shot
    public void StandardFire()
    {
        if (isOverheated) return;

        if (drumRoutine != null) StopCoroutine(drumRoutine);
        drumRoutine = StartCoroutine(RevolveDrumOnce());

        Heat = Mathf.Min(Heat + OverheatSpeed, MaxHeat);
        lastFiredTime = Time.time;
        Charge = 0f;

        if (recoilRoutine != null) StopCoroutine(recoilRoutine);
        recoilRoutine = StartCoroutine(Recoil());
    }

    // Handles charge fire logic (executed on release)
    public void ChargeFire()
    {
        // Always allow shot if charge started, even if overheated during it
        DamageToDeal = DefaultDamage + (Charge / 2f);
        Heat = Mathf.Min(Heat + OverheatSpeed, MaxHeat);
        lastFiredTime = Time.time;

        if (drumRoutine != null) StopCoroutine(drumRoutine);
        drumRoutine = StartCoroutine(ReturnDrumToNearestSlot());

        if (recoilRoutine != null) StopCoroutine(recoilRoutine);
        recoilRoutine = StartCoroutine(Recoil());

        Charge = 0f;

        // Lock out after firing if overheated mid-charge
        if (overheatedDuringCharge)
        {
            overheatedDuringCharge = false;
            isOverheated = true;
        }
    }

    // Lerps drum +60 degrees around Z axis quickly
    private IEnumerator RevolveDrumOnce()
    {
        float duration = 0.1f;
        float elapsed = 0f;
        Vector3 startRot = Drum.transform.localEulerAngles;
        float targetZ = startRot.z + 60f;

        if (targetZ >= 360f)
            targetZ -= 360f;

        Vector3 targetRot = new Vector3(startRot.x, startRot.y, targetZ);

        while (elapsed < duration)
        {
            Drum.transform.localEulerAngles = Vector3.Lerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Drum.transform.localEulerAngles = targetRot;
    }

    // Spins the drum while charging, speed scales with charge amount
    public void SpinDrum()
    {
        float spinSpeed = Mathf.Lerp(100f, 600f, Charge / MaxCharge);
        Drum.transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime, Space.Self);
    }

    // Returns drum to nearest upper multiple of 60 degrees
    private IEnumerator ReturnDrumToNearestSlot()
    {
        float currentZ = Drum.transform.localEulerAngles.z;
        float nearest = Mathf.Ceil(currentZ / 60f) * 60f;
        if (nearest >= 360f) nearest = 0f;

        Quaternion startRot = Drum.transform.localRotation;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, nearest);
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            Drum.transform.localRotation = Quaternion.Lerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Drum.transform.localRotation = targetRot;
    }

    // Handles recoil animation (0 -> 25 -> 0 rotation on X)
    private IEnumerator Recoil()
    {
        Quaternion startRot = Pistol.transform.localRotation;
        Quaternion peakRot = Quaternion.Euler(25f, 0f, 0f);
        float durationUp = 0.1f;
        float durationDown = 0.25f;
        float elapsed = 0f;

        while (elapsed < durationUp)
        {
            Pistol.transform.localRotation = Quaternion.Lerp(startRot, peakRot, elapsed / durationUp);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Pistol.transform.localRotation = peakRot;

        elapsed = 0f;
        while (elapsed < durationDown)
        {
            Pistol.transform.localRotation = Quaternion.Lerp(peakRot, Quaternion.identity, elapsed / durationDown);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Pistol.transform.localRotation = Quaternion.identity;
    }

    // Smoothly returns pistol to neutral rotation after overheat
    private IEnumerator ReturnPistolToNeutral()
    {
        Quaternion startRot = Pistol.transform.localRotation;
        Quaternion targetRot = Quaternion.identity;
        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            Pistol.transform.localRotation = Quaternion.Lerp(startRot, targetRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Pistol.transform.localRotation = targetRot;
    }
}
