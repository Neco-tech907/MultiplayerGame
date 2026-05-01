using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame.Practice1
{
    public struct MoveData : IReplicateData
    {
        public Vector2 MoveInput;

        private uint _tick;

        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public float VerticalVelocity;

        private uint _tick;

        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : TickNetworkBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private PlayerNetwork playerNetwork;

        private CharacterController characterController;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);
        }

        protected override void TimeManager_OnTick()
        {
            if (!base.IsClientInitialized && !base.IsServerInitialized)
            {
                return;
            }

            RunMove(BuildMoveData());
        }

        protected override void TimeManager_OnPostTick()
        {
            if (!base.IsServerInitialized && !base.IsClientInitialized)
            {
                return;
            }

            CreateReconcile();
        }

        public override void CreateReconcile()
        {
            SendReconcile(new ReconcileData
            {
                Position = transform.position,
                VerticalVelocity = verticalVelocity
            });
        }

        private MoveData BuildMoveData()
        {
            if (!base.IsOwner || playerNetwork == null || !playerNetwork.IsAlive.Value)
            {
                return default;
            }

            return new MoveData
            {
                MoveInput = Vector2.ClampMagnitude(ReadMoveInput(), 1f)
            };
        }

        [Replicate]
        private void RunMove(
            MoveData data,
            ReplicateState state = ReplicateState.Invalid,
            Channel channel = Channel.Unreliable)
        {
            if (characterController == null || playerNetwork == null || !playerNetwork.IsAlive.Value)
            {
                return;
            }

            float delta = (float)base.TimeManager.TickDelta;
            Vector2 input = Vector2.ClampMagnitude(data.MoveInput, 1f);
            Vector3 move = new Vector3(input.x, 0f, input.y) * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }

            verticalVelocity += gravity * delta;
            move.y = verticalVelocity;
            characterController.Move(move * delta);
        }

        [Reconcile]
        private void SendReconcile(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            verticalVelocity = data.VerticalVelocity;

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.position = data.Position;

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        private Vector2 ReadMoveInput()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.wKey.isPressed) vertical += 1f;

            return new Vector2(horizontal, vertical);
        }

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
            playerNetwork = GetComponent<PlayerNetwork>();
        }

        private void OnValidate()
        {
            if (playerNetwork == null)
            {
                playerNetwork = GetComponent<PlayerNetwork>();
            }
        }
    }
}
