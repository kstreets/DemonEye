using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DemonEye {
    public List<EyeModifier> modifers = new();
    public CoreAttack coreAttack;
}

public static class EyeFunctions {

    public static DemonEye equipedEye;
    private static GameManager gm;
    private static Limitter attackLimitter;

    public static void Init(GameManager gameManager) {
        gm = gameManager;    
    }

    public static void Update() {
        switch (equipedEye.coreAttack.attackType) {
            case CoreAttack.AttackType.Laser:
                UpdateLaser();
                break;
        }
    }

    public static bool CanShootPrimary() {
        float attackDelay = equipedEye.coreAttack.attackDelay;
        if (equipedEye.modifers.ContainsCount(gm.fireRateModifier, out int fireRateCount)) {
            for (int i = 0; i < fireRateCount; i++) {
                attackDelay -= 0.03f;
            }
            attackDelay = Mathf.Clamp(attackDelay, equipedEye.coreAttack.cappedMinAttackDelay, equipedEye.coreAttack.attackDelay);
        }
        return attackLimitter.TimeHasPassed(attackDelay);
    }

    public static void ShootPrimary() {
        switch (equipedEye.coreAttack.attackType) {
            case CoreAttack.AttackType.Projectile:
                ProjectilePrimaryShoot();
                break;
            case CoreAttack.AttackType.Laser:
                LaserPrimaryShoot();
                break;
        }
    }

    private static void ProjectilePrimaryShoot() {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = gm.mainCamera.ScreenToWorldPoint(mousePos);
        
        Vector2 velocity = (mouseWorldPos - gm.player.PositionV2()).normalized * equipedEye.coreAttack.projectileSpeed;
        SpawnProjectile(velocity);

        if (UseModifier(gm.triShotModifier)) {
            const float baseTriShotAngle = 8f;
            Vector2 secondShotVelocity = Quaternion.AngleAxis(baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(secondShotVelocity);
            Vector2 thirdShotVelocity = Quaternion.AngleAxis(-baseTriShotAngle, Vector3.forward) * velocity;
            SpawnProjectile(thirdShotVelocity);
        }
    }
    
    private static void SpawnProjectile(Vector2 velocity) {
        float angle = Vector2.SignedAngle(Vector2.right, velocity.normalized);
        Quaternion projectileRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        GameObject projectile = GameObject.Instantiate(equipedEye.coreAttack.projectilePrefab, gm.player.position + new Vector3(0f, 0.1f, 0f), projectileRotation);
        
        gm.projectiles.Add(new() {
            timeAlive = 0f,
            trans = projectile.transform,
            velocity = velocity,
            eyeSpawnedFrom = equipedEye,
        });
    }


    private static Timer laserTimer;
    private static Limitter laserDamageLimitter;
    private static LineRenderer laserRenderer;

    private static void LaserPrimaryShoot() {
        if (!laserRenderer) {
            laserTimer.SetTime(equipedEye.coreAttack.laserDuration);
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
        Vector3 endPos = startPos + (mouseWorldPos.ToVector3() - startPos).normalized * equipedEye.coreAttack.range;
        RaycastHit2D hit = Physics2D.Linecast(startPos, endPos, Masks.DamagableMask);
        
        laserRenderer.positionCount = 2;
        laserRenderer.SetPosition(0, startPos);
        laserRenderer.SetPosition(1, hit ? hit.point : endPos);
        
        if (laserDamageLimitter.TimeHasPassed(equipedEye.coreAttack.laserDamageTickDelay)) {
            gm.HandleDamage(equipedEye, hit.collider);
        }
    }


    private static bool UseModifier(EyeModifier modifier) {
        if (!equipedEye.modifers.Contains(modifier)) {
            return false;
        }
        
        if (modifier.alwaysActive) {
            return true;
        }
        
        float probability = 0f;
        foreach (EyeModifier mod in equipedEye.modifers) {
            probability += mod.activationProbability;
        }
        return Random.value <= Mathf.Clamp01(probability);
    }

}
