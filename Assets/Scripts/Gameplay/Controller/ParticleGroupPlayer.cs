using Gameplay.Tool;
using System.Collections;
using UnityEngine;

public class ParticleGroupPlayer : MonoBehaviour {
    [Header("Options")]
    [Tooltip("Tự động Play khi OnEnable với thời gian mặc định")]
    [SerializeField] private bool _autoPlayOnEnable = false;

    [Tooltip("Thời gian mặc định nếu gọi tự động")]
    [SerializeField] private float _defaultLifetime = 2f;

    [Tooltip("Tìm cả các ParticleSystem đang inactive trong children")]
    [SerializeField] private bool _includeInactive = true;

    private ParticleSystem[] _systems;

    void Awake() {
        CacheSystems();
    }

    void OnEnable() {
        if (_autoPlayOnEnable)
            Play();
    }

    /// <summary>
    /// Gọi hàm này để chạy tất cả ParticleSystem và hủy object sau 'lifetime' giây.
    /// </summary>
    public void Play() {
        if (_systems == null || _systems.Length == 0)
            CacheSystems();

        // Đảm bảo sạch trước khi chạy (kể cả sub-emitters)
        for (int i = 0; i < _systems.Length; i++) {
            var ps = _systems[i];
            if (ps == null) continue;

            // Nếu ParticleSystem đang nằm trên GameObject inactive mà bạn muốn chạy,
            // cần bật nó lên:
            if (_includeInactive && !ps.gameObject.activeInHierarchy)
                ps.gameObject.SetActive(true);

            ps.Clear(true);
            ps.Play(true);
        }

        // Đếm thời gian và hủy
        StopAllCoroutines();
        StartCoroutine(DestroyAfter(_defaultLifetime));
    }

    /// <summary>
    /// Dừng tất cả (nếu cần).
    /// </summary>
    public void StopAll() {
        if (_systems == null) return;
        for (int i = 0; i < _systems.Length; i++)
            if (_systems[i]) _systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void CacheSystems() {
        _systems = GetComponentsInChildren<ParticleSystem>(_includeInactive);
    }

    private IEnumerator DestroyAfter(float t) {
        if (t > 0f) yield return new WaitForSeconds(t);
        ObjectPool.Instance.Return(gameObject, true);
    }
}
