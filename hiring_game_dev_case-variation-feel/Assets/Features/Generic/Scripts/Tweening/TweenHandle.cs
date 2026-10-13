using System;
using UnityEngine;

namespace Game.Generic.Tweening
{
	internal enum TweenChannel { Value, Position, LocalScale }

	public sealed class TweenHandle
	{
		private Tween _value;
		private readonly Action<Vector3> _onUpdate;
		private readonly Action _onComplete;
		private readonly Action _onStop;

		internal Transform Target { get; }
		internal TweenChannel Channel { get; }
		public bool IsActive { get; private set; } = true;

		internal TweenHandle(Tween value, Action<Vector3> onUpdate, Action onComplete = null,
			Transform target = null, TweenChannel channel = TweenChannel.Value, Action onStop = null)
		{
			_value = value;
			_onUpdate = onUpdate;
			_onComplete = onComplete;
			_onStop = onStop;
			Target = target;
			Channel = channel;
		}

		public void Stop()
		{
			if (!IsActive) return;
			IsActive = false;
			_onStop?.Invoke();
		}

		internal void Tick(float deltaTime)
		{
			if (!IsActive) return;
			if (Channel != TweenChannel.Value && (Target == null || !Target.gameObject.activeInHierarchy))
			{
				Stop();
				return;
			}
			_onUpdate?.Invoke(_value.Tick(deltaTime));
			if (!IsActive || !_value.IsComplete) return;
			IsActive = false;
			_onComplete?.Invoke();
		}
	}
}
