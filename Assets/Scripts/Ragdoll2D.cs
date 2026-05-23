using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Full 2D skeletal ragdoll system for Stick Archers Battle.
/// Replaces the sprite with physics-based body parts on death.
/// </summary>
public class Ragdoll2D : MonoBehaviour
{
    [Header("Ragdoll Parts")]
    public Rigidbody2D head;
    public Rigidbody2D torso;
    public Rigidbody2D leftArm;
    public Rigidbody2D rightArm;
    public Rigidbody2D leftLeg;
    public Rigidbody2D rightLeg;
    
    [Header("Visuals")]
    public Sprite headSprite;
    public Sprite torsoSprite;
    public Sprite limbSprite;
    
    [Header("Physics Settings")]
    public float limbMass = 0.5f;
    public float headMass = 1f;
    public float torsoMass = 2f;
    public float jointDamping = 0.5f;
    public float jointFrequency = 8f;
    
    [Header("Death Settings")]
    public float fadeOutDelay = 3f;
    public float fadeOutDuration = 1f;
    public bool autoDestroy = true;
    
    private List<SpriteRenderer> partRenderers = new List<SpriteRenderer>();
    private List<HingeJoint2D> joints = new List<HingeJoint2D>();
    private bool isActive = false;
    private Vector3 impactForce;
    private Vector3 impactPoint;
    
    void Awake()
    {
        // Build ragdoll if parts not assigned
        if (torso == null)
            BuildRagdoll();
    }
    
    void BuildRagdoll()
    {
        // Create torso as root
        GameObject torsoObj = new GameObject("Torso");
        torsoObj.transform.SetParent(transform, false);
        torso = torsoObj.AddComponent<Rigidbody2D>();
        torso.mass = torsoMass;
        torso.gravityScale = 1f;
        
        var torsoCol = torsoObj.AddComponent<BoxCollider2D>();
        torsoCol.size = new Vector2(0.4f, 0.5f);
        
        var torsoSr = torsoObj.AddComponent<SpriteRenderer>();
        torsoSr.sprite = torsoSprite;
        torsoSr.sortingOrder = 10;
        partRenderers.Add(torsoSr);
        
        // Create head
        head = CreateLimb("Head", torso, new Vector2(0, 0.4f), 0.25f, headMass, true);
        
        // Create arms
        leftArm = CreateLimb("LeftArm", torso, new Vector2(-0.3f, 0.15f), 0.15f, limbMass);
        rightArm = CreateLimb("RightArm", torso, new Vector2(0.3f, 0.15f), 0.15f, limbMass);
        
        // Create legs
        leftLeg = CreateLimb("LeftLeg", torso, new Vector2(-0.15f, -0.4f), 0.15f, limbMass);
        rightLeg = CreateLimb("RightLeg", torso, new Vector2(0.15f, -0.4f), 0.15f, limbMass);
        
        // Disable initially
        SetRagdollEnabled(false);
    }
    
    Rigidbody2D CreateLimb(string name, Rigidbody2D parent, Vector2 offset, float size, float mass, bool isCircle = false)
    {
        GameObject limbObj = new GameObject(name);
        limbObj.transform.SetParent(transform, false);
        limbObj.transform.localPosition = offset;
        
        var rb = limbObj.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.gravityScale = 1f;
        
        // Collider
        if (isCircle)
        {
            var circle = limbObj.AddComponent<CircleCollider2D>();
            circle.radius = size;
        }
        else
        {
            var box = limbObj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(size * 0.6f, size * 2f);
        }
        
        // Sprite
        var sr = limbObj.AddComponent<SpriteRenderer>();
        sr.sprite = isCircle ? headSprite : limbSprite;
        sr.sortingOrder = 10;
        partRenderers.Add(sr);
        
        // Joint to parent
        if (parent != null)
        {
            var joint = limbObj.AddComponent<HingeJoint2D>();
            joint.connectedBody = parent;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector2.zero;
            joint.connectedAnchor = -offset;
            joint.useLimits = true;
            
            // Joint limits based on body part
            JointAngleLimits2D limits = new JointAngleLimits2D();
            if (name.Contains("Arm"))
            {
                limits.min = -120f;
                limits.max = 60f;
            }
            else if (name.Contains("Leg"))
            {
                limits.min = -30f;
                limits.max = 90f;
            }
            else if (name.Contains("Head"))
            {
                limits.min = -45f;
                limits.max = 45f;
            }
            joint.limits = limits;
            
            // Spring settings
            JointMotor2D motor = new JointMotor2D();
            motor.motorSpeed = 0f;
            motor.maxMotorTorque = 1000f;
            
            joints.Add(joint);
        }
        
        return rb;
    }
    
    /// <summary>
    /// Activate the ragdoll and apply impact force.
    /// </summary>
    public void Activate(Vector3 force, Vector3 hitPoint)
    {
        if (isActive) return;
        isActive = true;
        
        impactForce = force;
        impactPoint = hitPoint;
        
        // Position ragdoll at character position
        if (torso != null)
        {
            torso.transform.position = transform.position;
            torso.transform.rotation = transform.rotation;
        }
        
        // Enable physics
        SetRagdollEnabled(true);
        
        // Apply impact force to nearest limb
        Rigidbody2D hitLimb = GetNearestLimb(hitPoint);
        if (hitLimb != null)
        {
            hitLimb.AddForce(force, ForceMode2D.Impulse);
            hitLimb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
        }
        
        // Also apply to torso for overall movement
        if (torso != null)
        {
            torso.AddForce(force * 0.5f, ForceMode2D.Impulse);
        }
        
        // Start fade out
        if (autoDestroy)
            StartCoroutine(FadeOutAndDestroy());
    }
    
    Rigidbody2D GetNearestLimb(Vector3 point)
    {
        Rigidbody2D nearest = null;
        float nearestDist = float.MaxValue;
        
        Rigidbody2D[] limbs = { head, leftArm, rightArm, leftLeg, rightLeg };
        
        foreach (var limb in limbs)
        {
            if (limb == null) continue;
            float dist = Vector3.Distance(limb.transform.position, point);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = limb;
            }
        }
        
        return nearest ?? torso;
    }
    
    void SetRagdollEnabled(bool enabled)
    {
        // Enable/disable all rigidbodies
        Rigidbody2D[] allBodies = { head, torso, leftArm, rightArm, leftLeg, rightLeg };
        
        foreach (var rb in allBodies)
        {
            if (rb != null)
            {
                rb.isKinematic = !enabled;
                rb.simulated = enabled;
            }
        }
        
        // Enable/disable joints
        foreach (var joint in joints)
        {
            if (joint != null)
                joint.enabled = enabled;
        }
        
        // Show/hide renderers
        foreach (var sr in partRenderers)
        {
            if (sr != null)
                sr.enabled = enabled;
        }
    }
    
    IEnumerator FadeOutAndDestroy()
    {
        yield return new WaitForSeconds(fadeOutDelay);
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);
            
            foreach (var sr in partRenderers)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Check if ragdoll is currently active.
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
}
