using System.Collections;
using Hkmp.Api.Client.Networking;
using PropHunt.HKMP;
using PropHunt.Input;
using PropHunt.Util;
using System.Collections.Generic;
using Modding;
using UnityEngine;
using HKMPVector2 = Hkmp.Math.Vector2;

namespace PropHunt.Behaviors
{
    [RequireComponent(typeof(HeroController))]
    [RequireComponent(typeof(MeshRenderer))]
    internal class LocalPropManager : MonoBehaviour
    {
        private const float SHORTEST_DISTANCE = 2;

        private const float TRANSLATE_XY_SPEED = 1f;
        private const float TRANSLATE_Z_SPEED = 1f;
        private const float ROTATE_SPEED = 20f;
        private const float SCALE_SPEED = 0.5f;

        private const float XY_MAX_MAGNITUDE = 0.65f;
        private const float MIN_Z = -0.5f;
        private const float MAX_Z = 0.5f;
        private const float MIN_SCALE = 0.75f;
        private const float MAX_SCALE = 1.5f;

        private const int PROP_HEALTH_MIN = 2;
        private const int PROP_HEALTH_MAX = 11;

        private const float LARGEST_SPRITE_AREA = 25;
        private const float SMALLEST_SPRITE_AREA = 0;

        private Vector2 _origColSize;
        private int _origMaxHealth;
        private int _origHealth;

        private readonly List<PlayMakerFSM> _healthDisplays = new();

        private BoxCollider2D _col;
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
            _col = GetComponent<BoxCollider2D>();
            _hc = GetComponent<HeroController>();
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

            _origColSize = _col.size;
            _origHealth = _pd.health;
            _origMaxHealth = _pd.maxHealth;
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => GameCameras.instance.hudCanvas.transform.Find("Health/Health 1").gameObject != null);

            var healthParent = GameCameras.instance.hudCanvas.transform.Find("Health");
            for (int healthNum = 1; healthNum <= 11; healthNum++)
            {
                var health = healthParent.Find($"Health {healthNum}").gameObject;
                _healthDisplays.Add(health.LocateMyFSM("health_display"));
            }
        }

        private void Update()
        {
            ReadPropStateInputs();
            ReadMovementInputs();
        }

        private void OnEnable()
        {
            EnableInput(false);
            ModHooks.BeforePlayerDeadHook += OnPlayerDeath;
        }

        private void OnDisable()
        {
            ClearProp();
            EnableInput(true);
            ModHooks.BeforePlayerDeadHook -= OnPlayerDeath;
        }

        private void OnPlayerDeath()
        {
            PropHunt.Instance.Log("You have died.");
            _sender.SendSingleData(FromClientToServerPackets.PlayerDeath, new PlayerDeathFromClientToServerData());
        }

        /// <summary>
        /// Enable or disable inputs of a player on the prop team.
        /// </summary>
        /// <param name="enable">Whether to enable or disable the set of inputs.</param>
        private void EnableInput(bool enable)
        {
            if (enable)
            {
                On.HeroController.CanAttack     -= RemoveAttack;
                On.HeroController.CanCast       -= RemoveCast;
                On.HeroController.CanDreamNail  -= RemoveDreamNail;
                On.HeroController.CanFocus      -= RemoveFocus;
                On.HeroController.CanNailCharge -= RemoveNailCharge;
            }
            else
            {
                On.HeroController.CanAttack     += RemoveAttack;
                On.HeroController.CanCast       += RemoveCast;
                On.HeroController.CanDreamNail  += RemoveDreamNail;
                On.HeroController.CanFocus      += RemoveFocus;
                On.HeroController.CanNailCharge += RemoveNailCharge;
            }
        }
        
        private bool RemoveAttack(On.HeroController.orig_CanAttack orig, HeroController self) => false;
        private bool RemoveCast(On.HeroController.orig_CanCast orig, HeroController self) => false;
        private bool RemoveDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self) => false;
        private bool RemoveFocus(On.HeroController.orig_CanFocus orig, HeroController self) => false;
        private bool RemoveNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self) => false;

        /// <summary>
        /// Change the prop's state based on input.
        /// </summary>
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

            float shortestDistance = SHORTEST_DISTANCE;
            Breakable closestBreakable = null;
            foreach (var breakable in FindObjectsOfType<Breakable>())
            {
                float distance = Vector2.Distance(breakable.transform.position, _hc.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestBreakable = breakable;
                }
            }
            
            _propState = PropState.Free;
            
            string spriteName = "";
            var breakSprite = closestBreakable?.GetComponentInChildren<SpriteRenderer>().sprite;
            var breakCol = closestBreakable?.GetComponentInChildren<BoxCollider2D>();
            _propSprite.sprite = breakSprite;
            if (breakSprite != null)
            {
                _col.size = breakCol.size;

                Vector2 breakSize = breakSprite.bounds.size;
                var area = breakSize.x * breakSize.y;
                var healthRatio = _pd.health / (float)_pd.maxHealth;
                float maxHealth = MathUtil.Map(
                    area,
                    SMALLEST_SPRITE_AREA,
                    LARGEST_SPRITE_AREA,
                    PROP_HEALTH_MIN,
                    PROP_HEALTH_MAX);
                maxHealth = Mathf.Clamp(maxHealth, PROP_HEALTH_MIN, PROP_HEALTH_MAX);
                _pd.maxHealth = (int)maxHealth;
                _pd.health = Mathf.FloorToInt(healthRatio * _pd.maxHealth);
                _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

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

        /// <summary>
        /// While the prop state is not free, read inputs to determine how to transform the prop.
        /// </summary>
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
                            PositionXY = new HKMPVector2(Prop.transform.localPosition.x, Prop.transform.localPosition.y),
                        }
                    );
                    break;
                case PropState.TranslateZ:
                    float inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0 ? _heroInput.moveVector.Value.y : _heroInput.moveVector.Value.x;
                    var moveZ = Vector3.forward * inputValue * Time.deltaTime * TRANSLATE_Z_SPEED;
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
                    inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0 ? _heroInput.moveVector.Value.y : _heroInput.moveVector.Value.x;
                    var rotateZ = inputValue * Time.deltaTime * ROTATE_SPEED;
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
                    inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0 ? _heroInput.moveVector.Value.y : _heroInput.moveVector.Value.x;
                    var scaleFactor = Prop.transform.localScale.x + inputValue * Time.deltaTime * SCALE_SPEED;
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

        /// <summary>
        /// Set the prop's sprite to empty and make the player visible again.
        /// </summary>
        public void ClearProp()
        {
            _col.size = _origColSize;

            _pd.health = _origHealth;
            _pd.maxHealth = _origMaxHealth;
            _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

            _propState = PropState.Free;
            _propSprite.sprite = null;
            _meshRend.enabled = true;
            _hc.AcceptInput();
            _hc.RegainControl();

            _sender.SendSingleData
            (
                FromClientToServerPackets.BroadcastPropSprite,
                new PropSpriteFromClientToServerData
                {
                    SpriteName = string.Empty,
                }
            );
        }
    }
}