using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

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
    public Vector2 ChargeDrumSpin;
    public Slider ChargeSlider;

    [Header("Damage Settings")]
    public float DefaultDamage = 5f;
    private float DamageToDeal = 5f;
    private int explosionDamage;


    [Header("Raycast & Visual Settings")]
    public LineRenderer BeamLine;                    // persistent LineRenderer assigned in inspector
    public float SingleShotLineWidth = 0.02f;        // width for single shot beam
    public float ChargeShotLineWidth = 0.06f;        // width for charge shot beam
    public float SingleShotLineDuration = 0.05f;    // visible time for single shot beam
    public float ChargeShotLineDuration = 0.1f;     // visible time for charge shot beam
    public LayerMask ShootableLayers;

    [Header("Explosion Settings")]
    public GameObject ExplosionPrefab;               // prefab to spawn at hit point for charged shots
    public float ExplosionExpandSpeed = 5f;          // speed used when expanding the explosion
    public AnimationCurve ExplosionFadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    public float ExplosionRadiusModifier = 17f;

    [Header("Fire Timing")]
    public float HoldThreshold = 0.2f;               // how long until we consider the hold a charge

    // Internal state tracking
    private float lastFiredTime = 0f;
    public bool isOverheated = false;
    private bool isCharging = false;
    private bool overheatedDuringCharge = false;
    private Coroutine recoilRoutine;
    private Coroutine drumRoutine;
    private Coroutine overheatReturnRoutine;

    // Internal for timing holds and beam/explosion coroutines
    private float mouseDownTime = 0f;
    private Coroutine beamRoutine;

    public PlayerMovement player;

    void Update()
    {
        HeatSlider.value = Heat / 100;
        ChargeSlider.value = Charge / 100;

        // Handle firing input when not overheated or mid-charge
        if (Input.GetMouseButtonDown(0) && !player.isDead)
        {
            if (!isOverheated)
            {
                isCharging = true;
                mouseDownTime = Time.time; // start timing for hold threshold
            }
        }

        // While charging, accumulate charge and heat
        if (Input.GetMouseButton(0) && isCharging && !player.isDead)
        {
            Charge = Mathf.Min(Charge + ChargeSpeed * Time.deltaTime, MaxCharge);
            Heat = Mathf.Min(Heat + ChargeSpeed * Time.deltaTime, MaxHeat);
            SpinDrum();

            // If overheated while charging, note it but allow shot to finish
            if (Heat >= MaxHeat && !overheatedDuringCharge)
            {
                Heat = MaxHeat;
                overheatedDuringCharge = true;
            }
        }

        // On release, decide between primary or charge based on hold time, then fire
        if (Input.GetMouseButtonUp(0) && isCharging && !player.isDead)
        {
            float heldFor = Time.time - mouseDownTime;

            // If held less than threshold -> single shot
            if (heldFor < HoldThreshold)
            {
                isCharging = false;
                StandardFire();
                PrimaryFireLogic(); // visual + raycast for primary
            }
            else // held long enough -> charged shot
            {
                // capture charge before ChargeFire resets it
                float capturedCharge = Charge;
                isCharging = false;
                ChargeFire();
                ChargeShotLogic(capturedCharge); // visual + raycast + explosion using captured charge
            }
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


    #region Firing Logic

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

    // Handles charge fire logic (run on mouse up)
    public void ChargeFire()
    {
        DamageToDeal = DefaultDamage + (Charge / 2f);
        Heat = Mathf.Min(Heat + OverheatSpeed, MaxHeat);// Applies heat after charge fire released
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

    #endregion

    #region Overheat logic
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
    #endregion

    #region Animations
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
        float spinSpeed = Mathf.Lerp(ChargeDrumSpin.x, ChargeDrumSpin.y, Charge / MaxCharge);
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
    #endregion


    #region Raycast / Beam / Explosion Logic

    // Performs a raycast from Camera.main and draws a short-lived beam for a regular (primary) shot.
    // Also logs the hit GameObject (if any).
    public void PrimaryFireLogic()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        Vector3 hitPoint;

        if (Physics.Raycast(ray, out hit, 1000f, ShootableLayers))
        {
            hitPoint = hit.point;
            int dealtDamage = Mathf.RoundToInt(DamageToDeal);


            Debug.Log("[PrimaryFire] Hit: " + hit.collider.gameObject.name);
            if (hit.transform.gameObject.TryGetComponent(out IDamagable damageable))
            {
                damageable.TakeDamage(dealtDamage);
            }
        }
        else
        {
            hitPoint = ray.origin + ray.direction * 1000f;
            Debug.Log("[PrimaryFire] Hit: None");
        }

        // Show beam
        if (beamRoutine != null) StopCoroutine(beamRoutine);
        beamRoutine = StartCoroutine(ShowBeam(FirePoint.position, hitPoint, SingleShotLineWidth, SingleShotLineDuration));
    }

    public void ChargeShotLogic(float capturedCharge)
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        Vector3 hitPoint;

        if (Physics.Raycast(ray, out hit, 1000f, ShootableLayers))
        {
            hitPoint = hit.point;
            Debug.Log("[ChargeShot] Hit: " + hit.collider.gameObject.name);
        }
        else
        {
            hitPoint = ray.origin + ray.direction * 1000f;
            Debug.Log("[ChargeShot] Hit: None");
        }

        // Show beam
        if (beamRoutine != null) StopCoroutine(beamRoutine);
        beamRoutine = StartCoroutine(ShowBeam(FirePoint.position, hitPoint, ChargeShotLineWidth, ChargeShotLineDuration));

        // Calculate Explosion Damage
        float explosionDmg = DamageToDeal;
        explosionDamage = Mathf.FloorToInt(explosionDmg);
        Debug.Log("[ChargeShot] Unrounded Damage: " + DamageToDeal + ", Damage Dealt: " + explosionDmg + "  -  Charge at fire time: " + Charge);

        // Spawn explosion at hit point if prefab assigned
        if (ExplosionPrefab != null)
        {
            GameObject e = Instantiate(ExplosionPrefab, hitPoint, Quaternion.identity);
            Explosion explosion = e.GetComponent<Explosion>();
            if (explosion == null) { return; }
            Debug.Log(hitPoint);

            explosion.Setup(explosionDamage);

            StartCoroutine(ExpandExplosion(e.transform, capturedCharge));
        }
        else
        {
            Debug.LogWarning("[ChargeShotLogic] ExplosionPrefab not assigned.");
        }
    }

    // Shows the persistent LineRenderer between start and end for 'duration' seconds then disables it.
    private IEnumerator ShowBeam(Vector3 start, Vector3 end, float width, float duration)
    {
        if (BeamLine == null)
            yield break;

        BeamLine.enabled = true;
        BeamLine.positionCount = 2;
        BeamLine.SetPosition(0, start);
        BeamLine.SetPosition(1, end);
        BeamLine.startWidth = width;
        BeamLine.endWidth = width;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // keep the start tied to FirePoint in case gun moves while beam exists
            BeamLine.SetPosition(0, FirePoint.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        BeamLine.enabled = false;
    }

    // Expands and fades explosion using an AnimationCurve for alpha
    private IEnumerator ExpandExplosion(Transform explTransform, float capturedCharge)
    {
        Renderer renderer = explTransform.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("[ExpandExplosion] Explosion prefab has no Renderer.");
            yield break;
        }

        Material material = renderer.material;
        Color startColour = material.color;

        Vector3 startScale = explTransform.localScale;
        Vector3 targetScale = Vector3.one * (1 + (capturedCharge / Mathf.Max(1f, MaxCharge)) * ExplosionRadiusModifier);

        float t = 0f;

        while (t < 1f)
        {
            t += ExplosionExpandSpeed * Time.deltaTime;
            float lerpT = Mathf.Clamp01(t);

            // Scale growth
            explTransform.localScale = Vector3.Lerp(startScale, targetScale, lerpT);

            // Alpha from animation curve (curve defines the alpha over time)
            float curveValue = ExplosionFadeCurve.Evaluate(lerpT);
            Color c = startColour;
            c.a = curveValue;
            material.color = c;

            yield return null;
        }

        // Ensure fully faded
        Color final = material.color;
        final.a = 0f;
        material.color = final;

        Destroy(explTransform.gameObject);
    }


    #endregion
}
