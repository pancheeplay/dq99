using Dq99.Prototype.Domain;
using UnityEngine;

namespace Dq99.Prototype.Unity
{
    public sealed class ActorPresentation : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";
        [SerializeField] private string isMovingParameter = "IsMoving";
        [SerializeField] private float rotationSpeed = 720f;

        private static readonly int FallbackMoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int FallbackIsMovingHash = Animator.StringToHash("IsMoving");
        private int _moveSpeedHash;
        private int _isMovingHash;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            _moveSpeedHash = string.IsNullOrEmpty(moveSpeedParameter) ? FallbackMoveSpeedHash : Animator.StringToHash(moveSpeedParameter);
            _isMovingHash = string.IsNullOrEmpty(isMovingParameter) ? FallbackIsMovingHash : Animator.StringToHash(isMovingParameter);
        }

        public void Apply(ActorSnapshot snapshot, float deltaTime)
        {
            if (snapshot == null)
            {
                return;
            }

            transform.position = new Vector3(snapshot.Position.X, transform.position.y, snapshot.Position.Y);

            var facing = new Vector3(snapshot.Facing.X, 0f, snapshot.Facing.Y);
            if (facing.sqrMagnitude > 0.0001f && visualRoot != null)
            {
                var targetRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation, targetRotation, rotationSpeed * deltaTime);
            }

            if (animator == null)
            {
                return;
            }

            animator.SetBool(_isMovingHash, snapshot.IsMoving);
            animator.SetFloat(_moveSpeedHash, snapshot.IsMoving ? 1f : 0f);
        }
    }
}
