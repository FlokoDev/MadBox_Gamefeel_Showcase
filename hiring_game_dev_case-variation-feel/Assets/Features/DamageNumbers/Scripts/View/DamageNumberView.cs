using System;
using TMPro;
using UnityEngine;

namespace Game.DamageNumbers
{
	public class DamageNumberView : MonoBehaviour
	{
		[SerializeField] private TMP_Text label;
		[SerializeField] private Color color = Color.white;
		[SerializeField, Min(0.01f)] private float duration = 0.75f;
		[SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);
		[SerializeField] private float riseDistance = 1f;
		[SerializeField, Min(0f)] private float horizontalSpread = 0.3f;
		[SerializeField] private AnimationCurve scaleCurve = new AnimationCurve(
			new Keyframe(0f, 0.65f), new Keyframe(0.15f, 1.2f),
			new Keyframe(0.35f, 1f), new Keyframe(1f, 0.9f));
		[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve(
			new Keyframe(0f, 1f), new Keyframe(0.55f, 1f), new Keyframe(1f, 0f));

		private Vector3 _baseScale;
		private Vector3 _origin;
		private float _elapsed;
		private Camera _camera;
		private Action<DamageNumberView> _onFinished;
		private bool _isPlaying;

		private void Awake()
		{
			_baseScale = transform.localScale;
		}

		public void Play(Vector3 position, int damage, Camera camera, Action<DamageNumberView> onFinished)
		{
			_camera = camera;
			_onFinished = onFinished;
			_elapsed = 0f;
			_origin = position + offset;
			if (_camera != null)
				_origin += _camera.transform.right * UnityEngine.Random.Range(-horizontalSpread, horizontalSpread);
			label.SetText("{0}", damage);
			_isPlaying = true;
			UpdateAppearance(0f);
		}

		private void LateUpdate()
		{
			if (!_isPlaying) return;
			_elapsed += Time.deltaTime;
			float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, duration));
			UpdateAppearance(progress);
			if (progress < 1f) return;

			_isPlaying = false;
			Action<DamageNumberView> callback = _onFinished;
			_onFinished = null;
			callback?.Invoke(this);
		}

		private void UpdateAppearance(float progress)
		{
			transform.position = _origin + Vector3.up * (riseDistance * progress);
			if (_camera != null) transform.rotation = _camera.transform.rotation;
			transform.localScale = _baseScale * scaleCurve.Evaluate(progress);
			Color currentColor = color;
			currentColor.a *= Mathf.Clamp01(alphaCurve.Evaluate(progress));
			label.color = currentColor;
		}

		private void OnDisable()
		{
			_isPlaying = false;
			_onFinished = null;
		}
	}
}
