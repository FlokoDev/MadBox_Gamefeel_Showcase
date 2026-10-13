using UnityEngine;

namespace Game.Generic.Tweening
{
	// Vector interpolation evaluated by TweenService through a TweenHandle.
	public struct Tween
	{
		private Vector3 _start;
		private Vector3 _offset;
		private float _duration;
		private float _elapsed;
		private bool _isPunch;

		public bool IsComplete => _elapsed >= _duration;

		public static Tween DOMove(Vector3 start, Vector3 end, float duration)
		{
			return new Tween { _start = start, _offset = end - start, _duration = Mathf.Max(0f, duration) };
		}

		public static Tween DOPunchScale(Vector3 initialScale, Vector3 strength, float duration)
		{
			return new Tween
			{
				_start = initialScale, _offset = Vector3.Scale(initialScale, strength),
				_duration = Mathf.Max(0f, duration), _isPunch = true
			};
		}

		public Vector3 Tick(float deltaTime)
		{
			_elapsed = Mathf.Min(_duration, _elapsed + Mathf.Max(0f, deltaTime));
			float t = _duration > 0f ? _elapsed / _duration : 1f;
			if (_isPunch)
			{
				if (IsComplete) return _start;
				return _start + _offset * (Mathf.Sin(t * Mathf.PI * 3f) * (1f - t));
			}
			return _start + _offset * (1f - (1f - t) * (1f - t));
		}
	}
}
