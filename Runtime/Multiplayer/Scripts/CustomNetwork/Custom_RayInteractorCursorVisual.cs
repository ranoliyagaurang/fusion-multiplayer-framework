/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Fusion;
using Oculus.Interaction;
using UnityEngine;

namespace PTTI_Multiplayer
{
    public class Custom_RayInteractorCursorVisual : NetworkBehaviour
    {
        public RayInteractor _rayInteractor;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private float _offsetAlongNormal = 0.005f;

        [Tooltip("Players head transform, used to maintain the same cursor size on screen as it is moved in the scene.")]
        public Transform _playerHead;
        private Vector3 _startScale;

        #region Properties

        public Transform PlayerHead
        {
            get
            {
                return _playerHead;
            }
            set
            {
                _playerHead = value;
                if (_started && value == null)
                {
                    transform.localScale = _startScale;
                }
            }
        }

        #endregion

        protected bool _started = false;

        protected virtual void Start()
        {
            if (!Object.HasInputAuthority) return;

            _startScale = transform.localScale;

            this.BeginStart(ref _started);
            this.AssertField(_rayInteractor, nameof(_rayInteractor));
            this.AssertField(_renderer, nameof(_renderer));

            UpdateVisual();

            this.EndStart(ref _started);
            
            if (_started)
            {
                _rayInteractor.WhenPostprocessed += UpdateVisual;
                _rayInteractor.WhenStateChanged += UpdateVisualState;
                UpdateVisual();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed -= UpdateVisual;
                _rayInteractor.WhenStateChanged -= UpdateVisualState;
            }
        }

        private void UpdateVisual()
        {
            if (_rayInteractor.State == InteractorState.Disabled)
            {
                if (_renderer.enabled)
                {
                    _renderer.enabled = false;
                    transform.localScale = Vector3.zero;
                }    
                return;
            }

            if (_rayInteractor.CollisionInfo == null)
            {
                _renderer.enabled = false;
                transform.localScale = Vector3.zero;
                return;
            }

            if (!_renderer.enabled)
            {
                _renderer.enabled = true;
            }

            Vector3 collisionNormal = _rayInteractor.CollisionInfo.Value.Normal;
            transform.SetPositionAndRotation(_rayInteractor.End + collisionNormal * _offsetAlongNormal, Quaternion.LookRotation(_rayInteractor.CollisionInfo.Value.Normal, Vector3.up));

            if (PlayerHead != null)
            {
                float distance = Vector3.Distance(transform.position, PlayerHead.position);
                transform.localScale = _startScale * distance;
            }
        }

        private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

        #region Inject

        public void InjectAllRayInteractorCursorVisual(RayInteractor rayInteractor,
            Renderer renderer)
        {
            InjectRayInteractor(rayInteractor);
            InjectRenderer(renderer);
        }

        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        #endregion
    }
}