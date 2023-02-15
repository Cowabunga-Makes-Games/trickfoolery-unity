using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyState
{
    Passive,     // 0 - patrolling, "looking" for player
    Tracking,    // 1 - "aggro", trying to get into attack range of player
    Startup,     // 2 - in startup of attack
    Active,      // 3 - attacking, hitbox active
    Recovery,    // 4 - finishing attack
    Stunned,     // 5 - hit by attack, trap, etc. and stunned - cannot move, attack
    Dead         // 6 - rip
}

public abstract class Enemy: MonoBehaviour
{
    [SerializeField]
    int _maxHealth;
    [SerializeField]
    float _moveSpeed;
    [SerializeField]
    int _maxAnger;
    [SerializeField]
    Attack _basicAttack;
    [SerializeField]
    Attack _angyAttack;

    protected int maxHealth { get; set; }
    protected Attack basicAttack { get; set; }
    protected Attack angyAttack { get; set; }
    protected float moveSpeed { get; set; }
    protected int maxAnger { get; set; }

    protected int health { get; set; }
    protected int anger { get; set; }
    protected EnemyState state;

    protected GameObject player;
    protected FieldOfView fow;

    // for debugging
    protected void GetEnemyStatus(string name = "Enemy")
    {
        Debug.Log(System.String.Format("{0}(health: {1}/{2}, anger: {3}/{4}, state: {5})", name, health.ToString(), maxHealth.ToString(), anger.ToString(), maxAnger.ToString(), state));
    }

    // invoked when colliding with attack hitbox
    public virtual void TakeHit(int damage, float stunTime)
    {
        if (state == EnemyState.Dead)
        {
            return;
        }

        health -= damage;
        if (health <= 0)
        {
            StartCoroutine(Die());
        }
        else
        {
            StartCoroutine(GetStunned(stunTime));
        }
    }

    // puts enemy in Stunned state for stunTime seconds
    public virtual IEnumerator GetStunned(float stunTime)
    {
        // replace with animation change for entering hitstun here - turns red for now
        Color originalColor = GetComponent<MeshRenderer>().material.color;
        GetComponent<MeshRenderer>().material.color = Color.red;

        state = EnemyState.Stunned;
        yield return new WaitForSeconds(stunTime);  // waits for stunTime seconds before continuing

        // add animation change for entering patrol/passive state here - returns to original color for now
        GetComponent<MeshRenderer>().material.color = originalColor;

        state = EnemyState.Passive;
    }

    // invoked when health falls to/below 0
    public virtual IEnumerator Die()
    {
        fow.active = false;
        basicAttack.Deactivate();  // deactivate attack collider

        // the following is just for fun
        GetComponent<MeshRenderer>().material.color = Color.black;
        Rigidbody rigidBody = gameObject.GetComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.None;

        state = EnemyState.Dead;
        yield return new WaitForSeconds(3);  // waits for 3 seconds before destroying object

        Destroy(gameObject);
    }

    // invoked when player taunts and enemy is in taunt radius
    public virtual void GetTaunted(int tauntValue = 1)
    {
        anger = anger + tauntValue;
    }

    protected IEnumerator Attack(Attack attackObj) {
        state = EnemyState.Startup;
        // do startup animation
        yield return new WaitForSeconds(attackObj.startupTime);
        
        if (state == EnemyState.Dead || state == EnemyState.Stunned) {
            yield break;
        }

        state = EnemyState.Active;
        // do active attacking animation
        attackObj.Activate();  // activate attack collider
        yield return new WaitForSeconds(attackObj.activeTime);

        if (state == EnemyState.Dead || state == EnemyState.Stunned) {
            yield break;
        }

        state = EnemyState.Recovery;
        // do cooldown animation
        attackObj.Deactivate();  // deactivate attack collider
        yield return new WaitForSeconds(attackObj.recoveryTime);

        if (state == EnemyState.Dead || state == EnemyState.Stunned) {
            yield break;
        }
        
        state = EnemyState.Passive;
    }

    protected virtual void PlayerFound()
    {
        // animation for finding player?
        state = EnemyState.Tracking;
    }

    protected virtual void Move()
    {
        return;
    }

    protected virtual void Start()
    {
        maxHealth = _maxHealth;
        health = _maxHealth;
        maxAnger = _maxAnger;
        anger = 0;
        state = EnemyState.Passive;
        moveSpeed = _moveSpeed;
        basicAttack = _basicAttack;
        angyAttack = _angyAttack;
        player = GameObject.FindWithTag("Player");
        fow = gameObject.GetComponent<FieldOfView>();
    }
}