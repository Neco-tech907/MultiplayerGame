using FishNet.Object;
using UnityEngine;

namespace MultiplayerGame.Practice1
{
    public class PlayerCamera : NetworkBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 8f, -6f);
        [SerializeField] private float followSmoothTime = 0.12f;
        [SerializeField] private float lookSmoothSpeed = 10f;

        private Camera playerCamera;
        private Vector3 followVelocity;

        public override void OnStartClient()
        {
            if (!base.IsOwner)
            {
                enabled = false;
                return;
            }

            playerCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    return;
                }
            }

            Vector3 targetPosition = transform.position + offset;
            playerCamera.transform.position = Vector3.SmoothDamp(
                playerCamera.transform.position,
                targetPosition,
                ref followVelocity,
                followSmoothTime);

            Quaternion targetRotation = Quaternion.LookRotation(transform.position - playerCamera.transform.position);
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation,
                targetRotation,
                lookSmoothSpeed * Time.deltaTime);
        }
    }
}
