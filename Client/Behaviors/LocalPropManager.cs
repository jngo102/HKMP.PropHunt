using GlobalEnums;
using PropHunt.Input;
using PropHunt.Util;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PropHunt.Client.Behaviors
{
    /// <summary>
    /// Behavior for handling the local player's prop.
    /// </summary>
    [RequireComponent(typeof(HeroController))]
    [RequireComponent(typeof(MeshRenderer))]
    internal class LocalPropManager : MonoBehaviour
    {
        /// <summary>
        /// The maximum distance that the player can be from a background object to choose it as a prop.
        /// </summary>
        private const float SHORTEST_DISTANCE = 2;

        /// <summary>
        /// The rate at which the player may translate their prop along the x- and y-axes.
        /// </summary>
        private const float TRANSLATE_XY_SPEED = 2f;

        /// <summary>
        /// The rate at which the player may translate their prop along the z-axis.
        /// </summary>
        private const float TRANSLATE_Z_SPEED = 1f;

        /// <summary>
        /// The rate at which the player may rotate their prop.
        /// </summary>
        private const float ROTATE_SPEED = 60f;

        /// <summary>
        /// The rate at which the player may scale their prop.
        /// </summary>
        private const float SCALE_SPEED = 1f;

        /// <summary>
        /// The magnitude of the maximum distance that the player can translate their prop along the x- and y-axes.
        /// </summary>
        private const float XY_MAX_MAGNITUDE = 1f;

        /// <summary>
        /// The minimum distance that the player can translate their prop along the z-axis.
        /// </summary>
        private const float MIN_Z = -0.5f;

        /// <summary>
        /// The maximum distance that the player can translate their prop along the z-axis.
        /// </summary>
        private const float MAX_Z = 0.5f;

        /// <summary>
        /// The minimum scale at which the player may shrink their prop.
        /// </summary>
        private const float MIN_SCALE = 0.5f;

        /// <summary>
        /// The maximum scale at which the player may grow their prop.
        /// </summary>
        private const float MAX_SCALE = 2f;

        /// <summary>
        /// The original size of the player's collider.
        /// </summary>
        private Vector2 _origColSize;

        /// <summary>
        /// The player's box collider.
        /// </summary>
        private BoxCollider2D _col;

        /// <summary>
        /// The hero controller instance.
        /// </summary>
        private HeroController _hc;

        /// <summary>
        /// The main input actions for the player.
        /// </summary>
        private HeroActions _heroInput;

        /// <summary>
        /// An additional set of input actions for transforming a prop.
        /// </summary>
        private PropActions _propInput;

        /// <summary>
        /// The renderer for the player object.
        /// </summary>
        private MeshRenderer _meshRend;

        /// <summary>
        /// The renderer for the player's prop.
        /// </summary>
        private SpriteRenderer _propSprite;

        /// <summary>
        /// The renderer for the transform icon.
        /// </summary>
        private SpriteRenderer _iconRenderer;
        
        /// <summary>
        /// The current prop state.
        /// </summary>
        private PropState _propState = PropState.Free;

        public GameObject Prop { get; private set; }

        public Sprite PropSprite => _propSprite.sprite;

        /// <summary>
        /// The HKMP chat box object.
        /// </summary>
        private object _chatBox;
        
        /// <summary>
        /// The type of the HkMP chat box.  
        /// </summary>
        private Type _chatBoxType;
        
        private void Awake()
        {
            _col = GetComponent<BoxCollider2D>();
            _hc = GetComponent<HeroController>();
            _heroInput = GameManager.instance.inputHandler.inputActions;
            _propInput = PropHunt.Settings.Bindings;
            _meshRend = GetComponent<MeshRenderer>();

            Prop = new GameObject("Prop");
            Prop.transform.SetParent(transform);
            Prop.transform.localPosition = Vector3.zero;
            _propSprite = Prop.AddComponent<SpriteRenderer>();

            var icon = new GameObject("Prop Icon");
            icon.transform.SetParent(Prop.transform);
            icon.transform.localPosition = Vector3.zero;
            _iconRenderer = icon.AddComponent<SpriteRenderer>();
            _iconRenderer.sortingOrder = 100;

            _origColSize = _col.size;

            var hkmpAssembly = AppDomain.CurrentDomain.GetAssemblyByName("HKMP");
            _chatBoxType = hkmpAssembly.GetType("Hkmp.Ui.Chat.ChatBox");
            _chatBox = ClientGameManager.ChangeHkmpChatBoxType(_chatBoxType);
        }
        
        private void Update()
        {
            ReadPropStateInputs();
            ReadMovementInputs();
        }

        private void OnEnable()
        {
            LoadoutManager.SetPropLoadout();

            On.GameManager.HazardRespawn += OnHazardRespawn;
            On.HeroController.EnterScene += OnEnterScene;
            On.HeroController.FinishedEnteringScene += OnFinishEnteringScene;
            On.HeroController.TakeDamage += OnTakeDamage;
        }

        private void OnDisable() => Revert();

        /// <summary>
        /// Revert all changes made by this component to the game.
        /// </summary>
        private void Revert()
        {
            LoadoutManager.RevertPropLoadout();
            ClearProp();

            On.GameManager.HazardRespawn -= OnHazardRespawn;
            On.HeroController.EnterScene -= OnEnterScene;
            On.HeroController.FinishedEnteringScene -= OnFinishEnteringScene;
            On.HeroController.TakeDamage -= OnTakeDamage;
        }

        /// <summary>
        /// Hide the player during a hazard respawn if their prop has a sprite.
        /// </summary>
        private void OnHazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self)
        {
            orig(self);

            if (PropSprite != null) _meshRend.enabled = false;
        }

        /// <summary>
        /// Hide the player while entering a scene if their prop has a sprite.
        /// </summary>
        private IEnumerator OnEnterScene(On.HeroController.orig_EnterScene orig, HeroController self, TransitionPoint enterGate, float delayBeforeEnter)
        {
            yield return orig(self, enterGate, delayBeforeEnter);

            if (PropSprite != null) _meshRend.enabled = false;
        }

        /// <summary>
        /// Hide the player once they finish entering a scene if their prop has a sprite.
        /// </summary>
        private void OnFinishEnteringScene(On.HeroController.orig_FinishedEnteringScene orig, HeroController self, bool setHazardMarker, bool preventRunBob)
        {
            orig(self, setHazardMarker, preventRunBob);

            if (PropSprite != null) _meshRend.enabled = false;
        }

        /// <summary>
        /// Give back control to the player when they take damage.
        /// </summary>
        private void OnTakeDamage(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            orig(self, go, damageSide, damageAmount, hazardType);

            SetPropStateFree();
        }

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
                if (_propInput.TranslateXyWasPressed())
                {
                    if (_propState != PropState.TranslateXy)
                    {
                        _propState = PropState.TranslateXy;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = IconManager.GetIcon(IconType.TranslateXy);
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
                        _propState = PropState.TranslateZ;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = IconManager.GetIcon(IconType.TranslateZ);
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
                        _propState = PropState.Rotate;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = IconManager.GetIcon(IconType.Rotate);
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
                        _propState = PropState.Scale;
                        _hc.IgnoreInput();
                        _hc.RelinquishControl();
                        _iconRenderer.transform.rotation = Quaternion.identity;
                        _iconRenderer.flipX = false;
                        _iconRenderer.sprite = IconManager.GetIcon(IconType.ScaleUp);
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
            
            var breakSprite = closestBreakable?.GetComponentInChildren<SpriteRenderer>().sprite;
            var breakCol = closestBreakable?.GetComponentInChildren<BoxCollider2D>();
            _propSprite.sprite = breakSprite;
            if (breakSprite != null)
            {
                var moveUp = Vector3.up * ((breakCol.size.y - _col.size.y + _col.offset.y) / 2);
                transform.position += moveUp;
                    
                _col.size = breakCol.size;

                _propSprite.transform.localPosition = Vector3.zero;
                _propSprite.transform.rotation = Quaternion.identity;
                _propSprite.transform.localScale = Vector3.one;
                _meshRend.enabled = false;

                ComponentManager.BroadcastPropSprite(PropSprite, Prop.transform.localPosition, Prop.transform.localRotation.eulerAngles.z, Prop.transform.localScale.x);
            }
            else
            {
                ClearProp();
            }
        }

        /// <summary>
        /// While the prop state is not free, read inputs to determine how to transform the prop.
        /// </summary>
        private void ReadMovementInputs()
        {
            switch (_propState)
            {
                case PropState.TranslateXy:
                    var moveXy = _heroInput.moveVector.Value * Time.deltaTime * TRANSLATE_XY_SPEED;
                    moveXy = new Vector3(moveXy.x * transform.localScale.x, moveXy.y);
                    Prop.transform.localPosition += (Vector3)moveXy;
                    var clampedVector = Vector2.ClampMagnitude(Prop.transform.localPosition, XY_MAX_MAGNITUDE);
                    var clampedPos = new Vector3(clampedVector.x, clampedVector.y, Prop.transform.localPosition.z);
                    Prop.transform.localPosition = clampedPos;
                    ComponentManager.BroadcastPropPositionXy(Prop.transform.localPosition.x, Prop.transform.localPosition.y);
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
                    ComponentManager.BroadcastPropPositionZ(Prop.transform.localPosition.z);
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
                    ComponentManager.BroadcastPropRotation(Prop.transform.localRotation.eulerAngles.z);
                    break;
                case PropState.Scale:
                    inputValue = Mathf.Abs(_heroInput.moveVector.Value.y) > 0
                        ? _heroInput.moveVector.Value.y
                        : _heroInput.moveVector.Value.x;
                    _iconRenderer.flipX = false;
                    if (inputValue > 0)
                    {
                        _iconRenderer.sprite = IconManager.GetIcon(IconType.ScaleUp);
                    }
                    else if (inputValue < 0)
                    {
                        _iconRenderer.sprite = IconManager.GetIcon(IconType.ScaleDown);
                    }

                    var scaleFactor = Prop.transform.localScale.x + inputValue * Time.deltaTime * SCALE_SPEED;
                    scaleFactor = Mathf.Clamp(scaleFactor, MIN_SCALE, MAX_SCALE);
                    Prop.transform.localScale = Vector3.one * scaleFactor;

                    ComponentManager.BroadcastPropScale(Prop.transform.localScale.x);
                    break;
            }
        }

        /// <summary>
        /// Set the prop's sprite to empty and make the player visible again.
        /// </summary>
        public void ClearProp()
        {
            _col.size = _origColSize;

            LoadoutManager.SetHealth(1, true);

            _propState = PropState.Free;
            _propSprite.sprite = null;
            _iconRenderer.sprite = null;
            _meshRend.enabled = true;
            _hc.AcceptInput();
            _hc.RegainControl();

            try
            {
                ComponentManager.BroadcastPropSprite(null, Vector3.zero, 0, 1);
            }
            catch (InvalidOperationException)
            {
                
            }
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