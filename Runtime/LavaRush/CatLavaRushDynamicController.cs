using System;
using System.Threading.Tasks;
using ActionFit.LavaRush.UI;
using UnityEngine;

namespace ActionFit.Cat.App.LavaRush
{
    /// <summary>Identifies the existing Cat canvas selected for a Lava Rush controller root.</summary>
    public enum CatLavaRushCanvasType
    {
        Half,
    }

    /// <summary>Describes the preserved project-shell work needed for the canonical controller root.</summary>
    public readonly struct CatLavaRushDynamicControllerRequest
    {
        public CatLavaRushDynamicControllerRequest(
            string addressableKey,
            CatLavaRushCanvasType canvasType,
            bool ensureCamera,
            bool captureFonts)
        {
            AddressableKey = string.IsNullOrWhiteSpace(addressableKey)
                ? throw new ArgumentException("An Addressable key is required.", nameof(addressableKey))
                : addressableKey;
            CanvasType = canvasType;
            EnsureCamera = ensureCamera;
            CaptureFonts = captureFonts;
        }

        public string AddressableKey { get; }
        public CatLavaRushCanvasType CanvasType { get; }
        public bool EnsureCamera { get; }
        public bool CaptureFonts { get; }
    }

    /// <summary>Pairs one project-owned outer instance with its neutral package controller.</summary>
    public sealed class CatLavaRushDynamicControllerInstance
    {
        public CatLavaRushDynamicControllerInstance(
            GameObject root,
            global::UI_LavaRush controller)
        {
            Root = root;
            Controller = controller;
        }

        public GameObject Root { get; }
        public global::UI_LavaRush Controller { get; }
    }

    /// <summary>Defines only the existing Cat outer-load and instance-destruction facilities.</summary>
    public sealed class CatLavaRushDynamicControllerBinding
    {
        public CatLavaRushDynamicControllerBinding(
            Func<CatLavaRushDynamicControllerRequest, Task<CatLavaRushDynamicControllerInstance>>
                createAsync,
            Action<GameObject> destroyInstance)
        {
            CreateAsync = createAsync ?? throw new ArgumentNullException(nameof(createAsync));
            DestroyInstance = destroyInstance
                ?? throw new ArgumentNullException(nameof(destroyInstance));
        }

        public Func<
            CatLavaRushDynamicControllerRequest,
            Task<CatLavaRushDynamicControllerInstance>> CreateAsync { get; }
        public Action<GameObject> DestroyInstance { get; }
    }

    /// <summary>
    /// Owns the Cat product cache and gate without taking Addressable-handle or inner-host ownership.
    /// </summary>
    public sealed class CatLavaRushDynamicController
    {
        public const string AddressableKey = "UI_LavaRush";

        #region Fields

        private readonly CatLavaRushDynamicControllerBinding _binding;
        private readonly Action<global::UI_LavaRush> _initializeController;
        private readonly Action<Exception> _failureObserver;
        private global::UI_LavaRush _controller;
        private GameObject _root;
        private Task<global::UI_LavaRush> _loadingTask;

        #endregion

        public CatLavaRushDynamicController(
            CatLavaRushDynamicControllerBinding binding,
            Action<global::UI_LavaRush> initializeController,
            Action<Exception> failureObserver = null)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
            _initializeController = initializeController
                ?? throw new ArgumentNullException(nameof(initializeController));
            _failureObserver = failureObserver;
        }

        #region Properties

        public global::UI_LavaRush Controller
        {
            get
            {
                DropDestroyedReference();
                return _controller;
            }
        }

        public bool IsLoading => _loadingTask != null && !_loadingTask.IsCompleted;

        #endregion

        #region Public Methods

        /// <summary>Returns the cached controller or the one shared in-flight creation task.</summary>
        public Task<global::UI_LavaRush> GetAsync()
        {
            global::UI_LavaRush controller = Controller;
            if (controller != null)
            {
                return Task.FromResult(controller);
            }

            if (_loadingTask == null || _loadingTask.IsCompleted)
            {
                _loadingTask = LoadAsync();
            }

            return _loadingTask;
        }

        /// <summary>Uses the same cache and gate as ordinary access without creating another lifetime.</summary>
        public Task<global::UI_LavaRush> PrewarmAsync() => GetAsync();

        /// <summary>
        /// Drops only the product cache. The project shell retains instance and Addressable-handle ownership.
        /// </summary>
        public void Clear()
        {
            _controller = null;
            _root = null;
        }

        #endregion

        #region Private Methods

        private async Task<global::UI_LavaRush> LoadAsync()
        {
            CatLavaRushDynamicControllerInstance instance = null;
            try
            {
                var request = new CatLavaRushDynamicControllerRequest(
                    AddressableKey,
                    CatLavaRushCanvasType.Half,
                    true,
                    true);
                instance = await _binding.CreateAsync(request);
                if (!IsValid(instance))
                {
                    DestroyPartialInstance(instance);
                    return null;
                }

                _initializeController(instance.Controller);
                _root = instance.Root;
                _controller = instance.Controller;
                return _controller;
            }
            catch (Exception exception)
            {
                DestroyPartialInstance(instance);
                ObserveFailure(exception);
                return null;
            }
        }

        private static bool IsValid(CatLavaRushDynamicControllerInstance instance)
        {
            return instance != null
                && instance.Root != null
                && instance.Controller != null
                && instance.Controller.transform.IsChildOf(instance.Root.transform);
        }

        private void DestroyPartialInstance(CatLavaRushDynamicControllerInstance instance)
        {
            if (instance?.Root == null)
            {
                return;
            }

            try
            {
                _binding.DestroyInstance(instance.Root);
            }
            catch (Exception exception)
            {
                ObserveFailure(exception);
            }
        }

        private void DropDestroyedReference()
        {
            if (ReferenceEquals(_controller, null) || _controller != null)
            {
                return;
            }

            _controller = null;
            _root = null;
        }

        private void ObserveFailure(Exception exception)
        {
            try
            {
                _failureObserver?.Invoke(exception);
            }
            catch
            {
                // Diagnostics must not change the controller retry contract.
            }
        }

        #endregion
    }
}
