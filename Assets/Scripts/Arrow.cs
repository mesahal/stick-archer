using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class Arrow : MonoBehaviourPun
{
    public float destroyAfterSeconds = 4f;

    private Rigidbody2D rb;
    [HideInInspector] public int ownerActorNumber;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Add trail if not present
        if (GetComponent<ArrowTrail>() == null)
            gameObject.AddComponent<ArrowTrail>();
    }

    public void Launch(Vector2 force, int shooterActorNumber)
    {
        ownerActorNumber = shooterActorNumber;
        rb.AddForce(force, ForceMode2D.Impulse);
        
        // Start trail effect
        var trail = GetComponent<ArrowTrail>();
        trail?.StartTrail();
        
        photonView.RPC("RPC_SyncLaunch", RpcTarget.OthersBuffered, force, shooterActorNumber);
        Destroy(gameObject, destroyAfterSeconds);
    }

    [PunRPC]
    void RPC_SyncLaunch(Vector2 force, int shooterActorNumber)
    {
        ownerActorNumber = shooterActorNumber;
        rb.AddForce(force, ForceMode2D.Impulse);
        Destroy(gameObject, destroyAfterSeconds);
    }

    void FixedUpdate()
    {
        WindSystem.Instance?.ApplyWind(rb);
    }

    void Update()
    {
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (!photonView.IsMine) return;

        Archer archer = other.GetComponent<Archer>();
        if (archer != null
            && archer.photonView.Owner.ActorNumber != ownerActorNumber
            && !archer.isDead)
        {
            hasHit = true;

            var trail = GetComponent<ArrowTrail>();
            trail?.StopTrail();

            Vector2 hitDir = rb.velocity.normalized;
            ImpactEffect.Spawn(transform.position, hitDir);

            // Pass velocity so the remote client can activate ragdoll with correct force
            Vector3 impactForce = rb.velocity * 0.5f;
            photonView.RPC("RPC_OnHit", RpcTarget.All,
                archer.photonView.ViewID, ownerActorNumber,
                impactForce, transform.position);
            AudioManager.Instance?.PlayArrowHit();
            PhotonNetwork.Destroy(gameObject);
        }
    }

    [PunRPC]
    void RPC_OnHit(int archerViewID, int shooterActorNumber, Vector3 impactForce, Vector3 hitPoint)
    {
        PhotonView view = PhotonView.Find(archerViewID);
        if (view == null) return;
        Archer archer = view.GetComponent<Archer>();
        if (archer == null) return;
        archer.SetLastHit(impactForce, hitPoint);
        archer.OnHitReceived(shooterActorNumber);
    }
}
