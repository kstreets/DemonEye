using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class Eye {

    public static List<EyeModifier> modifers = new();
    public static BaseAttack baseAttack;

    private static GameManager gm;
    private static Limitter attackLimitter;

    public static void Init(GameManager gameManager) {
        gm = gameManager;    
    }

    public static void Update() {
        switch (baseAttack.attackType) {
            case BaseAttack.AttackType.Laser:
                UpdateLaser();
                break;
        }
    }

    public static bool CanShootPrimary() {
        float attackDelay = baseAttack.attackDelay;
        if (modifers.ContainsCount(gm.fireRateModifier, out int fireRateCount)) {
            for (int i = 0; i < fireRateCount; i++) {
                attackDelay -= 0.03f;
            }
            attackDelay = Mathf.Clamp(attackDelay, baseAttack.cappedMinAttackDelay, baseAttack.attackDelay);
        }
        return attackLimitter.TimeHasPassed(attackDelay);
    }

    public static void ShootPrimary() {
        switch (baseAttack.attackType) {
            case BaseAttack.AttackType.Projectile:
                ProjectilePrimaryShoot();
                break;
            case BaseAttack.AttackType.Laser:
                LaserPrimaryShoot();
                break;
        }
    }

    private static void ProjectilePrimaryShoot() {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = gm.mainCamera.ScreenToWorldPoint(mousePos);
        
        Vector2 velocity = (mouseWorldPos - gm.player.PositionV2()).normalized * baseAttack.projectileSpeed;
        SpawnProjectile(velocity);

        if (modifers.ContainsCount(gm.triShotModifier, out int triShotCount)) {
            const float baseTriShotAngle = 8f;
            for (int i = 0; i < triShotCount; i++) {
                float curAngle = baseTriShotAngle * (i + 1);
                Vector2 secondShotVelocity = Quaternion.AngleAxis(curAngle, Vector3.forward) * velocity;
                SpawnProjectile(secondShotVelocity);
                Vector2 thirdShotVelocity = Quaternion.AngleAxis(-curAngle, Vector3.forward) * velocity;
                SpawnProjectile(thirdShotVelocity);
            }
        }
    }
    
    private static void SpawnProjectile(Vector2 velocity) {
        float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
        Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        GameObject projectile = GameObject.Instantiate(gm.projectilePrefab, gm.player.position + new Vector3(0f, 0.1f, 0f), projectileRotation);
        
        gm.projectiles.Add(new() {
            timeAlive = 0f,
            trans = projectile.transform,
            velocity = velocity 
        });
    }


    private static Timer laserTimer;
    private static Limitter laserDamageLimitter;
    private static LineRenderer laserRenderer;

    private static void LaserPrimaryShoot() {
        if (!laserRenderer) {
            laserTimer.SetTime(baseAttack.laserDuration);
            laserRenderer = GameObject.Instantiate(gm.laserPrefab, gm.player.position, Quaternion.identity).GetComponent<LineRenderer>();
        }
    }

    private static void UpdateLaser() {
        if (!laserRenderer) return;
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = gm.mainCamera.ScreenToWorldPoint(mousePos);

        bool laserIsFinished = !laserTimer.IsFinished && laserTimer.Tick();
        
        if (laserIsFinished) {
            attackLimitter.MakeCurrent();
            if (laserRenderer) {
                GameObject.Destroy(laserRenderer.gameObject);
            }
            return;
        }
        
        Vector3 startPos = gm.player.position + new Vector3(0f, 0.1f, 0f);
        Vector3 endPos = startPos + (mouseWorldPos.ToVector3() - startPos).normalized * baseAttack.range;
        RaycastHit2D hit = Physics2D.Linecast(startPos, endPos, Masks.DamagableMask);
        
        laserRenderer.positionCount = 2;
        laserRenderer.SetPosition(0, startPos);
        laserRenderer.SetPosition(1, hit ? hit.point : endPos);
        
        if (laserDamageLimitter.TimeHasPassed(baseAttack.laserDamageTickDelay)) {
            gm.HandleDamage(hit.collider);
        }
    }

}
