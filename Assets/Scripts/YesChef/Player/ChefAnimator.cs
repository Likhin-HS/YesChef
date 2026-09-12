using UnityEngine;

namespace YesChef.Player
{
    /// <summary>
    /// Lightweight, zero-allocation procedural walk animator for the chibi chef.
    /// Handles alternating leg stride, foot lift, and subtle body bobbing/sway.
    /// Smoothly damps back to a clean flat-footed rest pose when idle.
    /// </summary>
    public sealed class ChefAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _controller;
        [SerializeField] private Transform _legPivotL;
        [SerializeField] private Transform _legPivotR;
        [SerializeField] private Transform _bodyRoot;

        [Header("Leg Walk Cycle")]
        [SerializeField] private float _walkFrequency = 16f;
        [SerializeField] private float _legSwingAngle = 30f;
        [SerializeField] private float _footLiftHeight = 0.035f;

        [Header("Body Dynamics")]
        [SerializeField] private float _bodyBobHeight = 0.02f;
        [SerializeField] private float _bodyWaddleAngle = 2f;
        [SerializeField] private float _dampingSpeed = 14f;

        private float _cycle;
        private Vector3 _basePosLegL;
        private Vector3 _basePosLegR;
        private Vector3 _basePosBody;
        private Quaternion _baseRotLegL = Quaternion.identity;
        private Quaternion _baseRotLegR = Quaternion.identity;
        private Quaternion _baseRotBody = Quaternion.identity;
        private bool _isInitialized;

        public Transform LegPivotL { get => _legPivotL; set => _legPivotL = value; }
        public Transform LegPivotR { get => _legPivotR; set => _legPivotR = value; }
        public Transform BodyRoot { get => _bodyRoot; set => _bodyRoot = value; }

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponent<PlayerController>();

            InitializeBaseTransforms();
        }

        public void InitializeBaseTransforms()
        {
            if (_legPivotL != null)
            {
                _basePosLegL = _legPivotL.localPosition;
                _baseRotLegL = _legPivotL.localRotation;
            }

            if (_legPivotR != null)
            {
                _basePosLegR = _legPivotR.localPosition;
                _baseRotLegR = _legPivotR.localRotation;
            }

            if (_bodyRoot != null)
            {
                _basePosBody = _bodyRoot.localPosition;
                _baseRotBody = _bodyRoot.localRotation;
            }

            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
                InitializeBaseTransforms();

            bool isMoving = _controller != null && _controller.IsMoving;

            if (isMoving)
            {
                float speedMultiplier = _controller != null && _controller.MoveInput.sqrMagnitude > 0.01f
                    ? _controller.MoveInput.magnitude
                    : 1f;

                _cycle += Time.deltaTime * _walkFrequency * speedMultiplier;

                // Alternating stride angle
                float swingL = Mathf.Sin(_cycle) * _legSwingAngle;
                float swingR = -swingL;

                // Foot lift during forward swing phase
                float liftL = Mathf.Max(0f, Mathf.Sin(_cycle)) * _footLiftHeight;
                float liftR = Mathf.Max(0f, -Mathf.Sin(_cycle)) * _footLiftHeight;

                // Rhythmic body bobbing (double frequency) and subtle chibi waddle roll
                float bob = Mathf.Abs(Mathf.Sin(_cycle)) * _bodyBobHeight;
                float waddle = Mathf.Sin(_cycle) * _bodyWaddleAngle;

                if (_legPivotL != null)
                {
                    _legPivotL.localRotation = _baseRotLegL * Quaternion.Euler(swingL, 0f, 0f);
                    _legPivotL.localPosition = _basePosLegL + new Vector3(0f, liftL, 0f);
                }

                if (_legPivotR != null)
                {
                    _legPivotR.localRotation = _baseRotLegR * Quaternion.Euler(swingR, 0f, 0f);
                    _legPivotR.localPosition = _basePosLegR + new Vector3(0f, liftR, 0f);
                }

                if (_bodyRoot != null)
                {
                    _bodyRoot.localRotation = _baseRotBody * Quaternion.Euler(0f, 0f, waddle);
                    _bodyRoot.localPosition = _basePosBody + new Vector3(0f, bob, 0f);
                }
            }
            else
            {
                // Smooth damping back to idle neutral pose
                float step = Time.deltaTime * _dampingSpeed;

                if (_legPivotL != null)
                {
                    _legPivotL.localRotation = Quaternion.Slerp(_legPivotL.localRotation, _baseRotLegL, step);
                    _legPivotL.localPosition = Vector3.Lerp(_legPivotL.localPosition, _basePosLegL, step);
                }

                if (_legPivotR != null)
                {
                    _legPivotR.localRotation = Quaternion.Slerp(_legPivotR.localRotation, _baseRotLegR, step);
                    _legPivotR.localPosition = Vector3.Lerp(_legPivotR.localPosition, _basePosLegR, step);
                }

                if (_bodyRoot != null)
                {
                    _bodyRoot.localRotation = Quaternion.Slerp(_bodyRoot.localRotation, _baseRotBody, step);
                    _bodyRoot.localPosition = Vector3.Lerp(_bodyRoot.localPosition, _basePosBody, step);
                }

                // Reset walk cycle phase when nearly settled
                if (Mathf.Abs(Quaternion.Angle(_legPivotL != null ? _legPivotL.localRotation : Quaternion.identity, _baseRotLegL)) < 0.5f)
                {
                    _cycle = 0f;
                }
            }
        }
    }
}
