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
    /// <summary>
    /// A visual affordance designed to accompany <see cref="RayInteractor"/>s. This is used in most ray interaction prefabs,
    /// wizards, and example scenes provided by the Interaction SDK. Though this class includes a number of customization
    /// options and can be set up independently, you should usually start from an example (scene or prefab) rather than trying
    /// to add this visual from scratch as this type makes assumptions about certain of its dependencies, such as those added
    /// by <see cref="InjectRenderer(Renderer)"/> and <see cref="InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor)"/>.
    /// </summary>
    public class Custom_ControllerRayVisual : NetworkBehaviour
    {
        public RayInteractor _rayInteractor;

        [SerializeField]
        private Renderer _renderer;

        private float _maxRayVisualLength = 0.5f;

        [SerializeField]
        private bool _hideWhenNoInteractable = false;

        private bool _started;

        protected virtual void Start()
        {
            if (!Object.HasInputAuthority) return;

            _maxRayVisualLength = _rayInteractor.MaxRayLength;

            this.BeginStart(ref _started);
            this.AssertField(_rayInteractor, nameof(_rayInteractor));
            this.AssertField(_renderer, nameof(_renderer));
            this.EndStart(ref _started);

            if (_started)
            {
                _rayInteractor.WhenPostprocessed += UpdateVisual;
                _rayInteractor.WhenStateChanged += HandleStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _rayInteractor.WhenPostprocessed -= UpdateVisual;
                _rayInteractor.WhenStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_rayInteractor.State == InteractorState.Disabled ||
                (_hideWhenNoInteractable && _rayInteractor.Interactable == null))
            {
                _renderer.enabled = false;
                transform.localScale = new Vector3(
                transform.localScale.x,
                transform.localScale.y,
                0);
                return;
            }

            _renderer.enabled = true;
            transform.SetPositionAndRotation(_rayInteractor.Origin, _rayInteractor.Rotation);

            transform.localScale = new Vector3(
                transform.localScale.x,
                transform.localScale.y,
                Mathf.Min(_maxRayVisualLength, (_rayInteractor.End - transform.position).magnitude));
        }

        #region Inject

        /// <summary>
        /// Injects all required dependencies for a dynamically instantiated ControllerRayVisual; effectively wraps
        /// <see cref="InjectRayInteractor(RayInteractor)"/>, <see cref="InjectRenderer(Renderer)"/>, and
        /// <see cref="InjectMaterialPropertyBlockEditor(MaterialPropertyBlockEditor)"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectAllControllerRayVisual(RayInteractor rayInteractor,
            Renderer renderer,
            MaterialPropertyBlockEditor materialPropertyBlockEditor)
        {
            InjectRayInteractor(rayInteractor);
            InjectRenderer(renderer);
        }

        /// <summary>
        /// Sets the <see cref="RayInteractor"/> for a dynamically instantiated ControllerRayVisual. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRayInteractor(RayInteractor rayInteractor)
        {
            _rayInteractor = rayInteractor;
        }

        /// <summary>
        /// Sets the <see cref="Renderer"/> for a dynamically instantiated ControllerRayVisual. This method exists to support Interaction SDK's
        /// dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }
        #endregion
    }
}