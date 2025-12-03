using Gameplay.Manager;
using Gameplay.Tool;
using UnityEngine;

public class Projectile : MonoBehaviour {
    [SerializeField] GameObject _explosionPrefab;
    private Pool _explosionPool;

    private Transform _target;
    private float _speed;
    private int _damage;
    private float _life = 5f;

    public void Launch(Transform target, float speed, int damage) {
        _target = target;
        _speed = speed;
        _damage = damage;

        transform.rotation = Quaternion.LookRotation((target.position - transform.position), Vector3.up);
    }

    private void Update() {
        _life -= Time.deltaTime;
        if (_life <= 0f) { Destroy(gameObject); return; }

        if (_target == null) { Destroy(gameObject); return; }

        Vector3 dir = (_target.position - transform.position);
        float distThisFrame = _speed * Time.deltaTime;

        if (dir.magnitude <= distThisFrame) {
            HitTarget();
            return;
        }

        transform.position += dir.normalized * distThisFrame;
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private void HitTarget() {
        var dmg = _target.GetComponentInParent<Gameplay.Controller.Enemy.CrocodileAI>();
        if (dmg != null) {
            dmg.TakeDamage(_damage);
        }

        if (_explosionPool == null)
            _explosionPool = ObjectPool.Instance.CheckAddPool(_explosionPrefab);

        GameObject explosion = ObjectPool.Instance.Get(_explosionPool);
        Vector3 pos = transform.position;
        pos.y = 4;
        explosion.transform.position = pos;
        explosion.GetComponent<ParticleGroupPlayer>().Play();

        SoundManager.Instance.PlaySoundFX(SoundType.Explosion);

        ObjectPool.Instance.Return(gameObject, true);
    }
}
