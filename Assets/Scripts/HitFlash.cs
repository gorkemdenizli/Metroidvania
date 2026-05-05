using System.Collections;
using UnityEngine;

// Attach to enemy root. Flashes all child SpriteRenderers to a tint color then fades back.
// Call Flash() from EnemyHealthController or BossHealthController on damage.
public class HitFlash : MonoBehaviour
{
    [Tooltip("Tint color applied on hit. Default: opaque red.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Tooltip("Seconds to stay at flash color before fading back.")]
    [SerializeField] private float holdDuration = 0.05f;

    [Tooltip("Seconds to lerp back to original color.")]
    [SerializeField] private float fadeDuration = 0.1f;

    private SpriteRenderer[] _renderers;
    private Color[]           _originalColors;
    private Coroutine         _routine;

    void Awake()
    {
        _renderers      = GetComponentsInChildren<SpriteRenderer>(true);
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].color;
    }

    public void Flash()
    {
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        SetAll(flashColor);

        yield return new WaitForSeconds(holdDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null)
                    _renderers[i].color = Color.Lerp(flashColor, _originalColors[i], t);
            yield return null;
        }

        RestoreAll();
        _routine = null;
    }

    void SetAll(Color c)
    {
        foreach (var sr in _renderers)
            if (sr != null) sr.color = c;
    }

    void RestoreAll()
    {
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null)
                _renderers[i].color = _originalColors[i];
    }
}
