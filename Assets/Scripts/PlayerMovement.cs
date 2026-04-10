using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame.Practice1
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private PlayerNetwork playerNetwork;

        private CharacterController characterController;
        private float verticalVelocity;
        private Vector2 currentMoveInput;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!IsSpawned || playerNetwork == null)
            {
                return;
            }

            if (IsOwner)
            {
                currentMoveInput = playerNetwork.IsAlive.Value ? ReadMoveInput() : Vector2.zero;
                SubmitMoveInputServerRpc(currentMoveInput);
            }

            if (!IsServer || !playerNetwork.IsAlive.Value)
            {
                return;
            }

            Vector2 moveInput = currentMoveInput;
            Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            move *= speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = 0f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            characterController.Move(move * Time.deltaTime);
        }

        [ServerRpc]
        private void SubmitMoveInputServerRpc(Vector2 moveInput)
        {
            currentMoveInput = Vector2.ClampMagnitude(moveInput, 1f);
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
