

using Internal.Scripts.System;
using UnityEngine;

namespace Internal.Scripts
{
    public class ManageInput : Singleton<ManageInput>
    {
        private Vector2 _mousePos;
        private Vector2 _delta;
        private bool _down;
        private Camera _camera;

        public delegate void MouseEvents(Vector2 pos);

        public static event MouseEvents OnDown, OnMove, OnUp, OnDownRight;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                MouseDown(GetMouseWorldPos());
                _down = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                MouseUp(GetMouseWorldPos());
                _down = false;
            }

            if (Input.GetMouseButtonDown(0))
                MouseDownRight(GetMouseWorldPos());
            if (Input.GetMouseButton(0) && _down)
            {
                MouseMove(GetMouseWorldPos());
            }
        }

        private Vector3 GetMouseWorldPos()
        {
            return _camera.ScreenToWorldPoint(Input.mousePosition);
        }

        public void MouseDown(Vector2 pos)
        {
            _mousePos = pos;
            OnDown?.Invoke(_mousePos);
        }

        public void MouseUp(Vector2 pos)
        {
            _mousePos = pos;
            OnUp?.Invoke(_mousePos);
        }

        public void MouseMove(Vector2 pos)
        {
            _delta = _mousePos - pos;
            _mousePos = pos;
            OnMove?.Invoke(_mousePos);
        }

        public void MouseDownRight(Vector2 pos)
        {
            _mousePos = pos;
            OnDownRight?.Invoke(_mousePos);
        }

        public Vector2 GetMousePosition() => _mousePos;
        public Vector2 GetMouseDelta() => _delta;
    }
}