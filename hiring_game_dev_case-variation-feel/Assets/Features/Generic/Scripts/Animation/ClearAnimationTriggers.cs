using UnityEngine;

namespace Game.Generic.Animation
{
	public class ClearAnimationTriggers : StateMachineBehaviour
	{
		[SerializeField]
		[Tooltip("Names of the Animator triggers to clear when entering, updating and leaving this state.")]
		private string[] triggersToClear = new string[0];

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			ClearTriggers(animator);
		}

		public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			ClearTriggers(animator);
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			ClearTriggers(animator);
		}

		private void ClearTriggers(Animator animator)
		{
			if (triggersToClear == null) return;
			foreach (string trigger in triggersToClear)
			{
				if (!string.IsNullOrWhiteSpace(trigger)) animator.ResetTrigger(trigger);
			}
		}
	}
}
