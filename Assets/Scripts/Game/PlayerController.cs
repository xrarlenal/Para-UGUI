using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform head;
        public float walkSpeed = 5.2f;
        public float sprintSpeed = 8.2f;
        public float jumpHeight = 1.15f;
        public float gravity = -18f;
        public float mouseSensitivity = 2.4f;
        public bool lookEnabled = true;
        public bool moveEnabled = true;

        CharacterController _controller;
        Vector3 _velocity;
        float _pitch;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (head == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) head = cam.transform;
            }
        }

        void Update()
        {
            if (lookEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

                transform.Rotate(Vector3.up * mx, Space.Self);
                _pitch = Mathf.Clamp(_pitch - my, -85f, 85f);
                if (head != null) head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            float x = 0f, z = 0f;
            if (moveEnabled)
            {
                x = Input.GetAxisRaw("Horizontal");
                z = Input.GetAxisRaw("Vertical");
            }

            var move = transform.right * x + transform.forward * z;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            if (_controller.isGrounded && _velocity.y < 0f) _velocity.y = -2f;

            if (moveEnabled && _controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move((move * speed + Vector3.up * _velocity.y) * Time.deltaTime);

            // 走路时轻微头部起伏，让第一人称不那么生硬
            if (head != null && _controller.isGrounded && move.sqrMagnitude > 0.01f)
            {
                float bob = Mathf.Sin(Time.time * (speed > walkSpeed + 0.1f ? 13f : 9f)) * 0.022f;
                var cam = head.GetComponent<Camera>();
                if (cam != null) cam.transform.localPosition = new Vector3(0f, bob, 0f);
            }
        }
    }
}
