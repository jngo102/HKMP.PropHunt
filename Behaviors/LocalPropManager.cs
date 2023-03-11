using GlobalEnums;
using HkmpPouch;
using PropHunt.Events;
using PropHunt.Input;
using PropHunt.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PropHunt.Behaviors
{
    [RequireComponent(typeof(HeroController))]
    [RequireComponent(typeof(MeshRenderer))]
    internal class LocalPropManager : MonoBehaviour
    {
        private const float SHORTEST_DISTANCE = 2;

        private const float TRANSLATE_XY_SPEED = 1f;
        private const float TRANSLATE_Z_SPEED = 1f;
        private const float ROTATE_SPEED = 35f;
        private const float SCALE_SPEED = 0.5f;

        private const float XY_MAX_MAGNITUDE = 0.65f;
        private const float MIN_Z = -0.5f;
        private const float MAX_Z = 0.5f;
        private const float MIN_SCALE = 0.75f;
        private const float MAX_SCALE = 1.5f;

        private const int PROP_HEALTH_MIN = 2;
        private const int PROP_HEALTH_MAX = 11;

        private const float LARGEST_SPRITE_AREA = 25f;
        private const float SMALLEST_SPRITE_AREA = 0f;

        private Vector2 _origColSize;
        private int _origHealth;
        private int _origMaxHealth;
        private int _origMaxHealthBase;

        private readonly List<PlayMakerFSM> _healthDisplays = new();

        private HeroAnimationController _anim;
        private BoxCollider2D _col;
        private HeroController _hc;
        private HeroActions _heroInput;
        private PropActions _propInput;
        private MeshRenderer _meshRend;
        private PlayerData _pd;
        private GameObject _username;
        public GameObject Prop { get; private set; }
        private PipeClient _pipe;
        private SpriteRenderer _propSprite;
        public Sprite PropSprite => _propSprite.sprite;
        private SpriteRenderer _iconRenderer;
        private PropState _propState = PropState.Free;

        private object _chatBox;
        private Type _chatBoxType;

        private void Awake()
        {
            _anim = GetComponent<HeroAnimationController>();
            _col = GetComponent<BoxCollider2D>();
            _hc = GetComponent<HeroController>();
            _heroInput = GameManager.instance.inputHandler.inputActions;
            _propInput = PropHunt.Instance.Settings.Bindings;
            _meshRend = GetComponent<MeshRenderer>();
            _pd = PlayerData.instance;
            _username = transform.Find("Username").gameObject;

            Prop = new GameObject("Prop");
            Prop.transform.SetParent(transform);
            Prop.transform.localPosition = Vector3.zero;
            _propSprite = Prop.AddComponent<SpriteRenderer>();

            var icon = new GameObject("Prop Icon");
            icon.transform.SetParent(Prop.transform);
            icon.transform.localPosition = Vector3.zero;
            _iconRenderer = icon.AddComponent<SpriteRenderer>();
            _iconRenderer.sortingOrder = 100;

            _pipe = PropHunt.PipeClient;

            _origColSize = _col.size;
            _origHealth = _pd.health;
            _origMaxHealth = _pd.maxHealth;
            _origMaxHealthBase = _pd.maxHealthBase;

            var hkmpAssembly = AppDomain.CurrentDomain.GetAssemblyByName("HKMP");
            _chatBoxType = hkmpAssembly.GetType("Hkmp.Ui.Chat.ChatBox");
            _chatBox = Convert.ChangeType(PropHunt.PipeClient.ClientApi.UiManager.ChatBox, _chatBoxType);
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() =>
                GameCameras.instance.hudCanvas.transform.Find("Health/Health 1").gameObject != null);

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

            _anim.enabled = false;

            _pd.health = 1;
            _pd.maxHealth = 1;
            _pd.maxHealthBase = 1;
            _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

            On.GameManager.HazardRespawn += OnHazardRespawn;
            On.HeroController.EnterScene += OnEnterScene;
            On.HeroController.FinishedEnteringScene += OnFinishEnteringScene;
            On.HeroController.TakeDamage += OnTakeDamage;
        }

        private void OnDisable()
        {
            ClearProp();
            EnableInput(true);

            _anim.enabled = true;

            _pd.health = _origHealth;
            _pd.maxHealth = _origMaxHealth;
            _pd.maxHealthBase = _origMaxHealthBase;
            _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

            On.GameManager.HazardRespawn -= OnHazardRespawn;
            On.HeroController.EnterScene -= OnEnterScene;
            On.HeroController.FinishedEnteringScene -= OnFinishEnteringScene;
            On.HeroController.TakeDamage -= OnTakeDamage;
        }

        private void OnHazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self)
        {
            orig(self);

            if (PropSprite != null) _meshRend.enabled = false;
        }

        private IEnumerator OnEnterScene(On.HeroController.orig_EnterScene orig, HeroController self, TransitionPoint enterGate, float delayBeforeEnter)
        {
            yield return orig(self, enterGate, delayBeforeEnter);

            if (PropSprite != null) _meshRend.enabled = false;
        }

        private void OnFinishEnteringScene(On.HeroController.orig_FinishedEnteringScene orig, HeroController self, bool setHazardMarker, bool preventRunBob)
        {
            orig(self, setHazardMarker, preventRunBob);

            if (PropSprite != null) _meshRend.enabled = false;
        }

        private void OnTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            orig(self, go, damageSide, damageAmount, hazardType);

            SetPropStateFree();
        }

        /// <summary>
        /// Enable or disable inputs of a player on the prop team.
        /// </summary>
        /// <param name="enable">Whether to enable or disable the set of inputs.</param>
        private void EnableInput(bool enable)
        {
            if (enable)
            {
                On.HeroController.CanCast -= RemoveCast;
                On.HeroController.CanDreamNail -= RemoveDreamNail;
                On.HeroController.CanFocus -= RemoveFocus;
                On.HeroController.CanNailCharge -= RemoveNailCharge;
            }
            else
            {
                On.HeroController.CanCast += RemoveCast;
                On.HeroController.CanDreamNail += RemoveDreamNail;
                On.HeroController.CanFocus += RemoveFocus;
                On.HeroController.CanNailCharge += RemoveNailCharge;
            }
        }

        private bool RemoveCast(On.HeroController.orig_CanCast orig, HeroController self) => false;
        private bool RemoveDreamNail(On.HeroController.orig_CanDreamNail orig, HeroController self) => false;
        private bool RemoveFocus(On.HeroController.orig_CanFocus orig, HeroController self) => false;
        private bool RemoveNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self) => false;

        /// <summary>
        /// Change the prop's state based on input.
        /// </summary>
        private void ReadPropStateInputs()
        {
            var chatBoxOpen = _chatBoxType.GetField("_isOpen", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_chatBox) as bool?;

            if ((chatBoxOpen.HasValue && chatBoxOpen.Value) ||
                GameManager.instance.IsGamePaused()) return;

            if (PropSprite != null)
            {
                if (_propInput.TranslateXYWasPressed())
                {
                    if (_propState != PropState.TranslateXY)
                    {
                        PropHunt.Instance.Log("Translate XY");
                        _propState = PropState.TranslateXY;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = PropHunt.Instance.PropIcons["TranslateXY"];
                    }
                    else
                    {
                        SetPropStateFree();
                    }
                }

                if (_propInput.TranslateZWasPressed())
                {
                    if (_propState != PropState.TranslateZ)
                    {
                        PropHunt.Instance.Log("Translate Z");
                        _propState = PropState.TranslateZ;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = PropHunt.Instance.PropIcons["TranslateZ"];
                    }
                    else
                    {
                        SetPropStateFree();
                    }
                }

                if (_propInput.RotateWasPressed())
                {
                    if (_propState != PropState.Rotate)
                    {
                        PropHunt.Instance.Log("Rotate");
                        _propState = PropState.Rotate;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = PropHunt.Instance.PropIcons["Rotate"];
                    }
                    else
                    {
                        SetPropStateFree();
                    }
                }

                if (_propInput.ScaleWasPressed())
                {
                    if (_propState != PropState.Scale)
                    {
                        PropHunt.Instance.Log("Scale");
                        _propState = PropState.Scale;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = PropHunt.Instance.PropIcons["ScaleUp"];
                    }
                    else
                    {
                        SetPropStateFree();
                    }
                }
            }

            if (!_propInput.SelectWasPressed()) return;

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
                _pd.maxHealthBase = (int)maxHealth;
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

            _pipe.BroadcastInScene(new UpdatePropSpriteEvent { SpriteName = spriteName },
                GameManager.instance.sceneName, false);
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
                    _pipe.BroadcastInScene(
                        new UpdatePropPositionXYEvent
                            { X = Prop.transform.localPosition.x, Y = Prop.transform.localPosition.y },
                        GameManager.instance.sceneName, false);
                    break;
                case PropState.TranslateZ:
                    float inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0
                        ? _heroInput.moveVector.Value.y
                        : _heroInput.moveVector.Value.x;
                    var moveZ = Vector3.forward * inputValue * Time.deltaTime * TRANSLATE_Z_SPEED;
                    Prop.transform.localPosition += moveZ;
                    clampedPos = new Vector3(Prop.transform.localPosition.x, Prop.transform.localPosition.y,
                        Mathf.Clamp(Prop.transform.localPosition.z, MIN_Z, MAX_Z));
                    Prop.transform.localPosition = clampedPos;
                    _pipe.BroadcastInScene(new UpdatePropPositionZEvent { Z = Prop.transform.localPosition.z },
                        GameManager.instance.sceneName, false);
                    break;
                case PropState.Rotate:
                    inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0
                        ? _heroInput.moveVector.Value.y
                        : _heroInput.moveVector.Value.x;
                    float rotateZ = inputValue * Time.deltaTime * ROTATE_SPEED;
                    if (rotateZ > 0)
                    {
                        _iconRenderer.flipX = true;
                    }
                    else if (rotateZ < 0)
                    {
                        _iconRenderer.flipX = false;
                    }
                    Prop.transform.Rotate(0, 0, rotateZ);
                    _pipe.BroadcastInScene(
                        new UpdatePropRotationEvent { Rotation = Prop.transform.localRotation.eulerAngles.z },
                        GameManager.instance.sceneName, false);
                    break;
                case PropState.Scale:
                    inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0
                        ? _heroInput.moveVector.Value.y
                        : _heroInput.moveVector.Value.x;
                    _iconRenderer.flipX = false;
                    if (inputValue > 0)
                    {
                        _iconRenderer.sprite = PropHunt.Instance.PropIcons["ScaleUp"];
                    }
                    else if (inputValue < 0)
                    {
                        _iconRenderer.sprite = PropHunt.Instance.PropIcons["ScaleDown"];
                    }

                    var scaleFactor = Prop.transform.localScale.x + inputValue * Time.deltaTime * SCALE_SPEED;
                    scaleFactor = Mathf.Clamp(scaleFactor, MIN_SCALE, MAX_SCALE);
                    Prop.transform.localScale = Vector3.one * scaleFactor;
                    _pipe.BroadcastInScene(new UpdatePropScaleEvent { Scale = Prop.transform.localScale.x },
                        GameManager.instance.sceneName, false);
                    break;
            }
        }

        /// <summary>
        /// Set the prop's sprite to empty and make the player visible again.
        /// </summary>
        public void ClearProp()
        {
            _col.size = _origColSize;

            _pd.health = 1;
            _pd.maxHealth = 1;
            _pd.maxHealthBase = 1;
            _healthDisplays.ForEach(fsm => fsm.SetState("ReInit"));

            _propState = PropState.Free;
            _propSprite.sprite = null;
            _iconRenderer.sprite = null;
            _meshRend.enabled = true;
            _hc.AcceptInput();
            _hc.RegainControl();

            _username.SetActive(true);

            if (!_pipe.ClientApi.NetClient.IsConnected) return;

            _pipe.Broadcast(new UpdatePropSpriteEvent { SpriteName = string.Empty });
        }

        /// <summary>
        /// Set the prop state to free.
        /// </summary>
        private void SetPropStateFree()
        {
            _propState = PropState.Free;
            _hc.AcceptInput();
            _hc.RegainControl();
            _iconRenderer.sprite = null;
        }
    }
}