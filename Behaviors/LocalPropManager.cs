using Hkmp.Api.Client.Networking;
using PropHunt.HKMP;
using PropHunt.Input;
using PropHunt.Util;
using UnityEngine;

using HKMPVector2 = Hkmp.Math.Vector2;

namespace PropHunt.Behaviors
{
    internal enum PropState
    {
        Free,
        TranslateXY,
        TranslateZ,
        Rotate,
        Scale,
    }

    internal class LocalPropManager : MonoBehaviour
    {
        private const float TRANSLATE_XY_SPEED = 1f;
        private const float TRANSLATE_Z_SPEED = 1f;
        private const float ROTATE_SPEED = 20f;
        private const float SCALE_SPEED = 0.5f;

        private const float XY_MAX_MAGNITUDE = 0.65f;
        private const float MIN_Z = -0.5f;
        private const float MAX_Z = 0.5f;
        private const float MIN_SCALE = 0.75f;
        private const float MAX_SCALE = 1.5f;

        private const float PROP_SPEED_MIN = 5;
        private const float PROP_SPEED_MAX = 15;
        private const int PROP_HEALTH_MIN = 1;
        private const int PROP_HEALTH_MAX = 11;

        private int _origMaxHealth;
        private int _origHealth;
        private float _origRunSpeed;

        private HeroController _hc;
        private HeroActions _heroInput;
        private PropActions _propInput;
        private MeshRenderer _meshRend;
        private PlayerData _pd;
        private GameObject _username;
        public GameObject Prop { get; private set; }
        private IClientAddonNetworkSender<FromClientToServerPackets> _sender;
        private SpriteRenderer _propSprite;
        public Sprite PropSprite => _propSprite.sprite;
        private PropState _propState = PropState.Free;

        private void Awake()
        {
            _hc = HeroController.instance;
            _heroInput = GameManager.instance.inputHandler.inputActions;
            _propInput = PropInputHandler.Instance.InputActions;
            _meshRend = GetComponent<MeshRenderer>();
            _pd = PlayerData.instance;
            _username = transform.Find("Username").gameObject;  
            Prop = new GameObject("Prop");
            Prop.transform.SetParent(transform);
            Prop.transform.localPosition = Vector3.zero;
            _propSprite = Prop.AddComponent<SpriteRenderer>();
            _sender = PropHuntClientAddon.Instance.PropHuntClientAddonApi.NetClient.GetNetworkSender<FromClientToServerPackets>(PropHuntClientAddon.Instance);

            _origHealth = _pd.health;
            _origMaxHealth = _pd.maxHealth;
            _origRunSpeed = _hc.RUN_SPEED;

            EnableInput(false);
        }

        private void Update()
        {
            ReadPropStateInputs();
            ReadMovementInputs(); 
        }

        private void OnEnable()
        {
            EnableInput(false);
        }

        private void OnDisable()
        {
            ClearProp();
            EnableInput(true);
        }

        private void EnableInput(bool enable)
        {
            Modding.Logger.Log("Enabling input: " + enable);
            _heroInput.attack.Enabled = enable;
            _heroInput.cast.Enabled = enable;
            _heroInput.dreamNail.Enabled = enable;
            _heroInput.focus.Enabled = enable;
            _heroInput.quickCast.Enabled = enable;

            Modding.Logger.Log("Attack enabled? " + _heroInput.attack.EnabledInHierarchy);
            Modding.Logger.Log("Cast enabled? " + _heroInput.cast.EnabledInHierarchy);
            Modding.Logger.Log("DreamNail enabled? " + _heroInput.dreamNail.EnabledInHierarchy);
            Modding.Logger.Log("Focus enabled? " + _heroInput.focus.EnabledInHierarchy);
            Modding.Logger.Log("QuickCast enabled? " + _heroInput.quickCast.EnabledInHierarchy);
        }

        private void ReadPropStateInputs()
        {
            if (PropSprite != null)
            {
                if (_propInput.TranslateXY.WasPressed)
                {
                    if (_propState != PropState.TranslateXY)
                    {
                        PropHunt.Instance.Log("Translate XY");
                        _propState = PropState.TranslateXY;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }

                if (_propInput.TranslateZ.WasPressed)
                {
                    if (_propState != PropState.TranslateZ)
                    {
                        PropHunt.Instance.Log("Translate Z");
                        _propState = PropState.TranslateZ;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }

                if (_propInput.Rotate.WasPressed)
                {
                    if (_propState != PropState.Rotate)
                    {
                        PropHunt.Instance.Log("Rotate");
                        _propState = PropState.Rotate;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }

                if (_propInput.Scale.WasPressed)
                {
                    if (_propState != PropState.Scale)
                    {
                        PropHunt.Instance.Log("Scale");
                        _propState = PropState.Scale;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                    }
                    else
                    {
                        _propState = PropState.Free;
                        _hc.AcceptInput();
                        _hc.RegainControl();
                    }
                }
            }

            if (!_propInput.Select.WasPressed) return;

            float shortestDistance = 1;
            Breakable closestBreakable = null;
            foreach (var breakable in FindObjectsOfType<Breakable>())
            {
                float distance = Vector2.Distance(breakable.transform.position, HeroController.instance.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestBreakable = breakable;
                }
            }

            _propState = PropState.Free;

            string spriteName = "";
            var breakSprite = closestBreakable?.GetComponentInChildren<SpriteRenderer>().sprite;
            _propSprite.sprite = breakSprite;
            if (breakSprite != null)
            {
                _heroInput.dash.Enabled = false;
                _heroInput.superDash.Enabled = false;

                var diagonalLength = breakSprite.bounds.size.magnitude;
                var healthRatio = _pd.health / _pd.maxHealth;
                _pd.maxHealth = (int)MathUtil.Map(
                    diagonalLength, 
                    PropHunt.Instance.SmallestSpriteDiagonalLength,
                    PropHunt.Instance.LargestSpriteDiagonalLength,
                    PROP_HEALTH_MIN,
                    PROP_HEALTH_MAX);
                _pd.health = Mathf.FloorToInt(healthRatio * _pd.maxHealth);
                _hc.RUN_SPEED = (int)MathUtil.Map(
                    diagonalLength, 
                    PropHunt.Instance.SmallestSpriteDiagonalLength,
                    PropHunt.Instance.LargestSpriteDiagonalLength,
                    PROP_SPEED_MAX,
                    PROP_SPEED_MIN);
                Modding.Logger.Log("New run speed: " + _hc.RUN_SPEED);

                spriteName = breakSprite.name;
                _propSprite.transform.localPosition = Vector3.zero;
                _propSprite.transform.rotation = Quaternion.identity;
                _propSprite.transform.localScale = Vector3.one;
                _meshRend.enabled = false;
                _username.SetActive(false);
            }
            else
            {
                ClearProp();
            }

            _sender.SendSingleData
            (
                FromClientToServerPackets.BroadcastPropSprite,
                new PropSpriteFromClientToServerData
                {
                    SpriteName = spriteName,
                }
            );
        }

        private void ReadMovementInputs()
        {
            switch (_propState)
            {
                case PropState.TranslateXY:
                    var moveXY = _heroInput.moveVector.Value * Time.deltaTime * TRANSLATE_XY_SPEED;
                    moveXY = new Vector3(moveXY.x * transform.localScale.x, moveXY.y);
                    Prop.transform.localPosition += (Vector3)moveXY;
                    var clampedVector = Vector2.ClampMagnitude(Prop.transform.localPosition, XY_MAX_MAGNITUDE);
                    var clampedPos = new Vector3(clampedVector.x, clampedVector.y, Prop.transform.localPosition.z);
                    Prop.transform.localPosition = clampedPos;
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropPositionXY,
                        new PropPositionXYFromClientToServerData
                        {
                            PositionXY = new Hkmp.Math.Vector2(Prop.transform.localPosition.x, Prop.transform.localPosition.y),
                        }
                    );
                    break;
                case PropState.TranslateZ:
                    var moveZ = Vector3.forward * _heroInput.moveVector.Value.y * Time.deltaTime * TRANSLATE_Z_SPEED;
                    Prop.transform.localPosition += moveZ;
                    clampedPos = new Vector3(Prop.transform.localPosition.x, Prop.transform.localPosition.y, Mathf.Clamp(Prop.transform.localPosition.z, MIN_Z, MAX_Z));
                    Prop.transform.localPosition = clampedPos;
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropPositionZ,
                        new PropPositionZFromClientToServerData
                        {
                            PositionZ = Prop.transform.localPosition.z,
                        }
                    );
                    break;
                case PropState.Rotate:
                    var rotateZ = _heroInput.moveVector.Value.x * Time.deltaTime * ROTATE_SPEED;
                    Prop.transform.Rotate(0, 0, rotateZ);
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropRotation,
                        new PropRotationFromClientToServerData
                        {
                            Rotation = Prop.transform.rotation.eulerAngles.z,
                        }
                    );
                    break;
                case PropState.Scale:
                    var scaleFactor = Prop.transform.localScale.x + _heroInput.moveVector.Value.y * Time.deltaTime * SCALE_SPEED;
                    scaleFactor = Mathf.Clamp(scaleFactor, MIN_SCALE, MAX_SCALE);
                    Prop.transform.localScale = Vector3.one * scaleFactor;
                    _sender.SendSingleData
                    (
                        FromClientToServerPackets.BroadcastPropScale,
                        new PropScaleFromClientToServerData
                        {
                            ScaleFactor = Prop.transform.localScale.x,
                        }
                    );
                    break;
            }
        }
        public void ClearProp()
        {
            _heroInput.dash.Enabled = true;
            _heroInput.superDash.Enabled = true;

            _pd.health = _origHealth;
            _pd.maxHealth = _origMaxHealth;
            _hc.RUN_SPEED = _origRunSpeed;

            _propState = PropState.Free;
            _propSprite.sprite = null;
            _meshRend.enabled = true;
            _hc.AcceptInput();
            _hc.RegainControl();
        }
    }
}